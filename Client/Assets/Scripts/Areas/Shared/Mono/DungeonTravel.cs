using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Mono;
using Assets.Scripts.Areas.Party.Mono;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Trade.Mono;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public sealed class DungeonTravel : NetworkBehaviour
    {
        public const string WorldScene = "EnvironmentScene";
        public const string DungeonScene = "DungeonScene";

        private static readonly Dictionary<ulong, DungeonTravel> _players = new Dictionary<ulong, DungeonTravel>();
        private static readonly List<Instance> _instances = new List<Instance>();

        private readonly NetworkVariable<int> _instanceId = new NetworkVariable<int>();
        private readonly NetworkVariable<LocationEnum> _location = new NetworkVariable<LocationEnum>();

        private CancellationTokenSource _lifetime;
        private Instance _instance;
        private Instance _pendingInstance;
        private TravelStageEnum _stage;
        private int _transitionId;
        private float _transitionStarted;
        private float _nextRequest;
        private float _nextPartyRetry;
        private Pose _destination;
        private Pose _returnPose;
        private IDisposable _loading;
        private Scene _clientScene;
        private bool _clientLoading;
        private int _clientInstanceId;
        private Vector3 _clientDeparturePosition;

        public bool IsTransitioning => _stage != TravelStageEnum.Idle || _clientLoading;
        public string CurrentSceneName => _location.Value.ToString();
        public LocationEnvironment CurrentRoom => _instance?.Room;
        public int CurrentInstanceId => _instanceId.Value;

        public Pose GetRespawnPose() => _instance != null
            ? _instance.Room.GetEntryPose(LocationEnum.EnvironmentScene)
            : new Pose(new Vector3(3.562874f, 1.41359f, 4.244279f), Quaternion.identity);

        public bool IsViewingHideout(int instanceId) => !_clientLoading && _clientScene.IsValid()
            && _clientScene.isLoaded && _clientScene.name == nameof(LocationEnum.HideoutScene)
            && _clientInstanceId == instanceId;
        public Pose PersistenceTransform => _instance != null || _pendingInstance != null
            ? _returnPose
            : new Pose(transform.position, transform.rotation);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _players.Clear();
            _instances.Clear();
        }

        public static int GetInstanceId(ulong clientId)
        {
            if (!_players.TryGetValue(clientId, out var player) || player._instance == null)
            {
                return 0;
            }

            // A former member immediately loses visibility, even while their return scene is loading.
            return player._instance.Location == LocationEnum.HideoutScene || player._instance.PartyId == PartyServerState.GetPartyId(clientId)
                ? player._instance.Id
                : -1 - (int)clientId;
        }

        public static bool CanInteract(ulong clientId)
        {
            return _players.TryGetValue(clientId, out var player) && player._stage == TravelStageEnum.Idle
                && (player._instance == null || player._instance.Location == LocationEnum.HideoutScene
                    || player._instance.PartyId == PartyServerState.GetPartyId(clientId));
        }

        public override void OnNetworkSpawn()
        {
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            if (IsServer)
            {
                _players[OwnerClientId] = this;
            }

            if (IsOwner && IsClient)
            {
                _clientScene = SceneManager.GetSceneByName(WorldScene);
            }
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                if (_stage != TravelStageEnum.Idle && Time.unscaledTime - _transitionStarted > 45f)
                {
                    Debug.LogWarning($"Dungeon transition timed out. ClientId: {OwnerClientId}, TransitionId: {_transitionId}.");
                    NetworkManager.DisconnectClient(OwnerClientId);

                    return;
                }

                if (_nextPartyRetry > 0f && Time.unscaledTime >= _nextPartyRetry)
                {
                    _nextPartyRetry = 0f;
                    ReconcileParty();
                }
            }

            if (IsOwner && !IsTransitioning)
            {
                CheckPortalInput();
            }
        }

        public static void NotifyPartyChanged()
        {
            foreach (var player in _players.Values.ToArray())
            {
                player.ReconcileParty();
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                RefreshVisibility();
            }
        }

        private void ReconcileParty()
        {
            if (!IsSpawned || !IsServer || _instance?.Location == LocationEnum.HideoutScene)
            {
                return;
            }

            if (_stage == TravelStageEnum.Idle && Time.unscaledTime >= _nextPartyRetry
                && _instance != null && _instance.PartyId != PartyServerState.GetPartyId(OwnerClientId))
            {
                // Forming a party while solo keeps that run; leaving an existing party ejects only this member.
                if (_instance.PartyId == Guid.Empty && PartyServerState.GetPartyId(OwnerClientId) != Guid.Empty
                    && !_instances.Any(x => x != _instance && x.Location == _instance.Location && x.PartyId == PartyServerState.GetPartyId(OwnerClientId)))
                {
                    _instance.PartyId = PartyServerState.GetPartyId(OwnerClientId);
                    RefreshVisibility();
                }
                else
                {
                    BeginTransition(null, _returnPose);
                }
            }
        }

        private void CheckPortalInput()
        {
            DungeonPortal portal = null;
            var mouse = Mouse.current;

            if (mouse != null && Camera.main != null && !InputFocusUI.IsAnyInputFocused
                && !(EventSystem.current?.IsPointerOverGameObject() ?? false))
            {
                var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

                if (Physics.Raycast(ray, out var hit) && hit.collider.TryGetComponent(out DungeonPortal candidate)
                    && Vector3.Distance(transform.position, candidate.transform.position) <= DungeonPortal.InteractionDistance)
                {
                    portal = candidate;
                }
            }

            if (portal == null)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            CursorUI.Instance.ShowPointer();

            if (mouse.rightButton.wasPressedThisFrame)
            {
                UsePortalServerRpc(portal.Destination);
            }
        }

        [ServerRpc]
        private void UsePortalServerRpc(LocationEnum destination)
        {
            if (_stage != TravelStageEnum.Idle || Time.unscaledTime < _nextRequest)
            {
                return;
            }

            _nextRequest = Time.unscaledTime + 0.5f;
            var portal = FindPortal(destination);

            var hasSession = UserManager.Instance.TryGetPlayerSessionId(OwnerClientId, out _);
            var hasCharacter = UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character);
            var isDead = hasCharacter && character.Health <= 0;
            var distance = portal != null ? Vector3.Distance(transform.position, portal.transform.position) : float.PositiveInfinity;
            var hasTrade = TradeServerState.TryGetSession(OwnerClientId, out _);
            var reason = !hasSession ? "MissingSession"
                : !hasCharacter ? "MissingCharacter"
                : isDead ? "DeadCharacter"
                : portal == null ? "MissingPortal"
                : !(distance <= DungeonPortal.InteractionDistance) ? "OutOfRange"
                : hasTrade ? "ActiveTrade"
                : null;

            if (reason != null)
            {
                Debug.LogWarning($"Dungeon portal rejected. ClientId: {OwnerClientId}, Destination: {destination}, Reason: {reason}, Health: {character?.Health}, Distance: {distance}, ActiveTrade: {hasTrade}.");
                RejectedClientRpc(isDead, OwnerClientId.ToClientRpcParams());

                return;
            }

            if (portal.Destination == LocationEnum.EnvironmentScene)
            {
                BeginTransition(null, _returnPose);

                return;
            }

            if (!Enum.IsDefined(typeof(LocationEnum), portal.Destination))
            {
                Debug.LogWarning($"Unknown portal destination. ClientId: {OwnerClientId}, Destination: {destination}.");
                RejectedClientRpc(false, OwnerClientId.ToClientRpcParams());

                return;
            }

            var partyId = portal.Destination == LocationEnum.HideoutScene ? Guid.Empty : PartyServerState.GetPartyId(OwnerClientId);
            var instance = partyId == Guid.Empty ? null : _instances
                .Where(x => x.PartyId == partyId && x.Location == portal.Destination)
                .FirstOrDefault();

            if (portal.Destination == LocationEnum.HideoutScene)
            {
                instance = _instances.Where(x => x.Location == LocationEnum.HideoutScene && x.CharacterId == character.Id).FirstOrDefault();
            }

            try
            {
                Physics.SyncTransforms();

                if (_instance == null)
                {
                    _returnPose = portal.GetArrivalPose();
                }

                instance ??= CreateInstance(partyId, portal.Destination);
                instance.CharacterId = portal.Destination == LocationEnum.HideoutScene ? character.Id : 0;

                Physics.SyncTransforms();
                BeginTransition(instance, instance.Room.GetEntryPose(_location.Value));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                RejectedClientRpc(false, OwnerClientId.ToClientRpcParams());
                CleanupInstances();
            }
        }

        [ClientRpc]
        private void RejectedClientRpc(bool isDead, ClientRpcParams rpcParams = default)
        {
            LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey(
                isDead ? Assets.Scripts.Areas.Shared.Enums.TranslateKeyEnum.DungeonPortalDead
                    : Assets.Scripts.Areas.Shared.Enums.TranslateKeyEnum.DungeonPortalUnavailable), color: ColorUI.Red).Forget();
        }

        private DungeonPortal FindPortal(LocationEnum destination)
        {
            var roots = _instance != null
                ? new[] { _instance.Room.gameObject }
                : SceneManager.GetSceneByName(WorldScene).GetRootGameObjects();

            return roots.SelectMany(x => x.GetComponentsInChildren<DungeonPortal>())
                .Where(x => x.Destination == destination)
                .FirstOrDefault();
        }

        private static Instance CreateInstance(Guid partyId, LocationEnum location)
        {
            var id = 1;

            while (_instances.Any(x => x.Id == id))
            {
                id++;
            }

            var prefabName = location.ToString().Replace("Scene", "EnvironmentPrefab");
            var prefab = Resources.Load<LocationEnvironment>(prefabName);

            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing environment prefab: {prefabName}.");
            }

            prefab.ValidateNavigation();

            var scene = SceneManager.CreateScene($"{location}-Instance-{id}");
            var room = Instantiate(prefab, new Vector3(10000 + id * 100, 0, 0), Quaternion.identity);
            SceneManager.MoveGameObjectToScene(room.gameObject, scene);
            var instance = new Instance { Id = id, PartyId = partyId, Location = location, Scene = scene, Room = room };

            _instances.Add(instance);

            foreach (var spawner in room.GetComponentsInChildren<Spawner>(true))
            {
                spawner.ConfigureInstance(id);
            }

            Debug.Log($"Dungeon created. InstanceId: {id}, PartyId: {partyId}.");

            return instance;
        }

        private void BeginTransition(Instance destination, Pose pose)
        {
            _pendingInstance = destination;
            _destination = pose;
            _stage = TravelStageEnum.Saving;
            _transitionStarted = Time.unscaledTime;
            _transitionId++;

            GetComponent<TargetSelector>().ResetForTravel();
            GetComponentInParent<Assets.Scripts.Areas.Trade.Mono.Trade>().CancelForTravel();
            SaveAndPrepareAsync(_lifetime.Token).Forget();

            Debug.Log($"Dungeon transition started. ClientId: {OwnerClientId}, TransitionId: {_transitionId}, Destination: {destination?.Id ?? 0}.");
        }

        private async UniTask SaveAndPrepareAsync(CancellationToken token)
        {
            try
            {
                if (_pendingInstance?.Location == LocationEnum.HideoutScene)
                {
                    await GetComponent<Assets.Scripts.Areas.Hideout.CharacterHideout>().LoadRoomAsync(_pendingInstance.Room, token);

                    if (!IsCurrentLifetime(token))
                    {
                        return;
                    }
                }

                await GetComponent<CharacterTransform>().SaveTransformAsync(_returnPose);

                if (!IsCurrentLifetime(token))
                {
                    return;
                }

                RefreshVisibility();
                _stage = TravelStageEnum.Loading;

                var hideout = _pendingInstance?.Room.GetComponentInChildren<Assets.Scripts.Areas.Hideout.HideoutBuilding>(true);
                var hideoutJson = hideout == null ? string.Empty : JsonSerializer.Serialize(new Assets.Scripts.Areas.Hideout.HideoutDto
                {
                    Buildings = new[] { hideout.Definition },
                    CurrentTime = hideout.CurrentTime
                });

                PrepareClientRpc(_transitionId, _pendingInstance?.Location ?? LocationEnum.EnvironmentScene,
                    _pendingInstance?.Room.transform.position ?? Vector3.zero, _pendingInstance?.Id ?? 0,
                    hideoutJson, OwnerClientId.ToClientRpcParams());
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (IsCurrentLifetime(token))
                {
                    _stage = TravelStageEnum.Idle;
                    _pendingInstance = null;
                    _nextPartyRetry = Time.unscaledTime + 5f;
                    RejectedClientRpc(false, OwnerClientId.ToClientRpcParams());
                    CleanupInstances();
                }
            }
        }

        [ClientRpc]
        private void PrepareClientRpc(int transitionId, LocationEnum location, Vector3 offset, int instanceId,
            string hideoutJson, ClientRpcParams rpcParams = default)
        {
            if (IsOwner && !_clientLoading)
            {
                LoadDestinationAsync(transitionId, location, offset, instanceId, hideoutJson, _lifetime.Token).Forget();
            }
        }

        private async UniTask LoadDestinationAsync(int transitionId, LocationEnum location, Vector3 offset,
            int instanceId, string hideoutJson, CancellationToken token)
        {
            var receivedAt = Time.realtimeSinceStartupAsDouble;
            _clientLoading = true;
            _clientDeparturePosition = transform.position;
            _loading = LoadingScreenUI.Instance.Show(TranslateManager.Instance.GetByKey(Assets.Scripts.Areas.Shared.Enums.TranslateKeyEnum.DungeonLoading));
            GetComponent<ThirdPersonController>().enabled = false;
            GetComponent<TargetSelector>().HandleUnselect();
            GetComponent<Assets.Scripts.Areas.Professions.Mono.Crafting>().CancelForTravel();
            GetComponent<Assets.Scripts.Areas.Professions.Mono.Mining>().CancelForTravel();
            GetComponent<Assets.Scripts.Areas.Professions.Mono.Lumberjack>().CancelForTravel();
            GetComponent<Assets.Scripts.Areas.Professions.Mono.Herbalism>().CancelForTravel();
            GetComponent<Assets.Scripts.Areas.Professions.Mono.Fishing>().CancelForTravel();
            MerchantUI.Instance.Hide();
            CursorUI.Instance.ShowDefault();

            try
            {
                // LoadingScene stays loaded throughout the session. Render its view before changing rooms.
                await UniTask.NextFrame(cancellationToken: token);

                if (!IsCurrentLifetime(token))
                {
                    return;
                }

                if (_clientScene.IsValid() && _clientScene.isLoaded)
                {
                    await SceneManager.UnloadSceneAsync(_clientScene);

                    if (!IsCurrentLifetime(token))
                    {
                        return;
                    }
                }

                var sceneName = location.ToString();
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                if (!IsCurrentLifetime(token))
                {
                    var abandonedScene = SceneManager.GetSceneByName(sceneName);

                    if (abandonedScene.IsValid() && abandonedScene.isLoaded)
                    {
                        await SceneManager.UnloadSceneAsync(abandonedScene);
                    }

                    return;
                }

                _clientScene = SceneManager.GetSceneByName(sceneName);

                foreach (var root in _clientScene.GetRootGameObjects())
                {
                    root.transform.position += offset;
                }

                Physics.SyncTransforms();
                _clientInstanceId = instanceId;

                if (location == LocationEnum.HideoutScene)
                {
                    GetComponent<Assets.Scripts.Areas.Hideout.CharacterHideout>()
                        .RestoreRoom(hideoutJson, Time.realtimeSinceStartupAsDouble - receivedAt);
                }

                Debug.Log($"Location loaded. Location: {location}, TransitionId: {transitionId}, EnvironmentSceneLoaded: {SceneManager.GetSceneByName(WorldScene).isLoaded}.");

                ReadyServerRpc(transitionId);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (IsCurrentLifetime(token))
                {
                    NetworkManager.Shutdown();
                }
            }
        }

        [ServerRpc]
        private void ReadyServerRpc(int transitionId)
        {
            if (_stage != TravelStageEnum.Loading || transitionId != _transitionId)
            {
                return;
            }

            _stage = TravelStageEnum.Arriving;
            _instance = _pendingInstance;
            _instanceId.Value = _instance?.Id ?? 0;
            _location.Value = _instance?.Location ?? LocationEnum.EnvironmentScene;
            _pendingInstance = null;
            transform.SetPositionAndRotation(_destination.position, _destination.rotation);

            RefreshVisibility();
            ArriveClientRpc(transitionId, _destination.position, _destination.rotation, OwnerClientId.ToClientRpcParams());
            CleanupInstances();
        }

        [ClientRpc]
        private void ArriveClientRpc(int transitionId, Vector3 position, Quaternion rotation, ClientRpcParams rpcParams = default)
        {
            FinishArrivalAsync(transitionId, position, rotation, _lifetime.Token).Forget();
        }

        private async UniTask FinishArrivalAsync(int transitionId, Vector3 position, Quaternion rotation, CancellationToken token)
        {
            var controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            GetComponent<ClientNetworkTransform>().Teleport(position, rotation, transform.localScale);
            Physics.SyncTransforms();
            controller.enabled = true;

            var movement = GetComponent<ThirdPersonController>();
            movement.ResetAfterTeleport(position - _clientDeparturePosition);

            // Keep LoadingScene visible until Cinemachine has evaluated the teleported target.
            try
            {
                await UniTask.NextFrame(cancellationToken: token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }

            if (!IsCurrentLifetime(token))
            {
                return;
            }

            movement.enabled = true;
            _clientLoading = false;
            _loading?.Dispose();
            _loading = null;

            ArrivedServerRpc(transitionId);
        }

        [ServerRpc]
        private void ArrivedServerRpc(int transitionId)
        {
            if (_stage != TravelStageEnum.Arriving || transitionId != _transitionId)
            {
                return;
            }

            _stage = TravelStageEnum.Idle;
            Debug.Log($"Dungeon transition completed. ClientId: {OwnerClientId}, TransitionId: {transitionId}, InstanceId: {_instance?.Id ?? 0}.");

            ReconcileParty();
        }

        private static void RefreshVisibility()
        {
            var manager = NetworkManager.Singleton;

            foreach (var obj in manager.SpawnManager.SpawnedObjectsList.ToArray())
            {
                if (obj.GetComponent<DungeonWorldObject>() == null)
                {
                    continue;
                }

                foreach (var clientId in manager.ConnectedClientsIds)
                {
                    var visible = obj.CheckObjectVisibility(clientId);

                    if (visible && !obj.IsNetworkVisibleTo(clientId))
                    {
                        obj.NetworkShow(clientId);
                    }
                    else if (!visible && obj.IsNetworkVisibleTo(clientId))
                    {
                        obj.NetworkHide(clientId);
                    }
                }
            }
        }

        private static void CleanupInstances()
        {
            foreach (var instance in _instances.ToArray())
            {
                if (_players.Values.Any(x => x._instance == instance || x._pendingInstance == instance))
                {
                    continue;
                }

                // Keep production running while the owner plays anywhere on this server.
                if (instance.Location == LocationEnum.HideoutScene && _players.Keys.Any(clientId =>
                    UserManager.Instance.Characters.TryGetValue(clientId, out var character) && character.Id == instance.CharacterId))
                {
                    continue;
                }

                foreach (var spawner in instance.Room.GetComponentsInChildren<Spawner>(true))
                {
                    spawner.StopSpawning();
                }

                SceneManager.UnloadSceneAsync(instance.Scene);
                _instances.Remove(instance);
                Debug.Log($"Dungeon released. InstanceId: {instance.Id}.");
            }
        }

        private bool IsCurrentLifetime(CancellationToken token)
        {
            return !token.IsCancellationRequested && IsSpawned && _lifetime?.Token == token;
        }

        public override void OnNetworkDespawn()
        {
            _lifetime?.Cancel();
            _loading?.Dispose();
            _loading = null;

            if (IsOwner && CursorUI.Instance != null)
            {
                CursorUI.Instance.ShowDefault();
            }

            if (IsServer)
            {
                _players.Remove(OwnerClientId);
                CleanupInstances();
            }

            _lifetime?.Dispose();
            _lifetime = null;
            base.OnNetworkDespawn();
        }

        private enum TravelStageEnum
        {
            Idle,
            Saving,
            Loading,
            Arriving
        }

        private sealed class Instance
        {
            public int Id;
            public int CharacterId;
            public Guid PartyId;
            public LocationEnvironment Room;
            public LocationEnum Location;
            public Scene Scene;
        }
    }
}
