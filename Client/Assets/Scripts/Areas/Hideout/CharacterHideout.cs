using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Inventory.Mono;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.Mono;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Trade.Mono;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Areas.Hideout
{
    public sealed class CharacterHideout : NetworkBehaviour
    {
        private CancellationTokenSource _lifetime;
        private HideoutBuilding _clientBuilding;
        private HideoutBuilding[] _clientBuildings = Array.Empty<HideoutBuilding>();
        private bool _busy;
        private float _nextRequest;

        public override void OnNetworkSpawn()
        {
            _lifetime = new CancellationTokenSource();
        }

        public async UniTask LoadRoomAsync(LocationEnvironment room, CancellationToken travelToken)
        {
            var lifetime = _lifetime.Token;
            var quests = GetComponent<CharacterQuests>();
            await quests.WaitForInventoryQuestMutationAsync(travelToken);

            HideoutDto result;

            try
            {
                if (!Alive(lifetime) || travelToken.IsCancellationRequested)
                {
                    return;
                }

                result = await UnityWebRequestHelper.ExecutePostAsync<HideoutDto>("Hideouts",
                    new { Building = HideoutBuildingEnum.None }, UserManager.Instance.GetPlayerSessionId(OwnerClientId), cancellationToken: travelToken);
            }
            finally
            {
                quests.ReleaseInventoryQuestMutation();
            }

            if (!Alive(lifetime) || travelToken.IsCancellationRequested || room == null)
            {
                return;
            }

            foreach (var building in room.GetComponentsInChildren<HideoutBuilding>(true))
            {
                building.Apply(result.Buildings.Where(x => x.Id == building.BuildingId).Single(), result.CurrentTime, true);
            }

            Debug.Log($"Hideout restored. CharacterId: {result.CharacterInventory.CharacterId}, Buildings: {result.Buildings.Length}.");
        }

        public void RestoreRoom(string json, double elapsedSeconds)
        {
            var result = JsonSerializer.Deserialize<HideoutDto>(json);
            var scene = SceneManager.GetSceneByName(nameof(LocationEnum.HideoutScene));
            _clientBuildings = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<HideoutBuilding>(true)).ToArray();

            foreach (var building in _clientBuildings)
            {
                building.Apply(result.Buildings.Where(x => x.Id == building.BuildingId).Single(), result.CurrentTime.AddSeconds(elapsedSeconds), false);
            }

            Debug.Log($"Hideout client projection restored before arrival. Buildings: {_clientBuildings.Length}.");
        }

        private void Update()
        {
            if (!IsOwner || !IsSpawned)
            {
                return;
            }

            var travel = GetComponent<DungeonTravel>();
            var available = _clientBuilding != null && _clientBuilding.CanBuild
                && _clientBuilding.IsInRange(transform) && !travel.IsTransitioning;

            if (CraftingUI.Instance.BuildHideout != null && (!available || Keyboard.current?.escapeKey.wasPressedThisFrame == true))
            {
                CraftingUI.Instance.Hide();
            }

            var mouse = Mouse.current;

            HideoutBuilding hoveredBuilding = null;

            if (!travel.IsTransitioning && mouse != null && Camera.main != null && !InputFocusUI.IsAnyInputFocused
                && !(EventSystem.current?.IsPointerOverGameObject() ?? false)
                && Physics.Raycast(Camera.main.ScreenPointToRay(mouse.position.ReadValue()), out var hit))
            {
                hoveredBuilding = hit.collider.GetComponentInParent<HideoutBuilding>();
            }

            var hover = hoveredBuilding != null && _clientBuildings.Contains(hoveredBuilding)
                && hoveredBuilding.CanBuild && hoveredBuilding.IsInRange(transform);

            if (!hover)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            CursorUI.Instance.ShowPointer();

            if (mouse.rightButton.wasPressedThisFrame)
            {
                _clientBuilding = hoveredBuilding;
                var buildingId = hoveredBuilding.BuildingId;
                GetComponent<Assets.Scripts.Areas.Professions.Mono.Crafting>().CancelForTravel();

                CraftingUI.Instance.ShowHideout(_clientBuilding.Definition, () =>
                {
                    CraftingUI.Instance.Hide();
                    AccessServerRpc(buildingId);
                });
            }
        }

        [ServerRpc]
        private void AccessServerRpc(HideoutBuildingEnum buildingId)
        {
            Access(buildingId);
        }

        private void Access(HideoutBuildingEnum buildingId)
        {
            var travel = GetComponent<DungeonTravel>();

            var building = travel.CurrentRoom?.GetComponentsInChildren<HideoutBuilding>(true)
                .Where(x => x.BuildingId == buildingId)
                .SingleOrDefault();

            var allowed = !_busy && Time.unscaledTime >= _nextRequest && building != null
                && travel.CurrentSceneName == nameof(LocationEnum.HideoutScene)
                && DungeonTravel.CanInteract(OwnerClientId)
                && UserManager.Instance.TryGetPlayerSessionId(OwnerClientId, out _)
                && UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character) && character.Health > 0
                && !TradeServerState.IsInventoryReserved(OwnerClientId)
                && building.CanBuild && building.IsInRange(transform);

            if (!allowed)
            {
                Debug.LogWarning($"Hideout request rejected. ClientId: {OwnerClientId}, Building: {buildingId}, Busy: {_busy}.");
                ResultClientRpc(string.Empty, DungeonTravel.GetInstanceId(OwnerClientId), OwnerClientId.ToClientRpcParams());

                return;
            }

            _busy = true;
            _nextRequest = Time.unscaledTime + 0.5f;
            AccessAsync(buildingId, building, DungeonTravel.GetInstanceId(OwnerClientId),
                UserManager.Instance.GetPlayerSessionId(OwnerClientId), _lifetime.Token).Forget();
        }

        private async UniTask AccessAsync(HideoutBuildingEnum buildingId, HideoutBuilding building, int instanceId, string session, CancellationToken lifetime)
        {
            using var mutation = TradeServerState.TrackInventoryMutation(OwnerClientId);
            var operationToken = TradeCommitCoordinator.GetServerLifetimeCancellationToken();
            var quests = GetComponent<CharacterQuests>();

            try
            {
                await quests.WaitForInventoryQuestMutationAsync(operationToken);

                HideoutDto result;

                try
                {
                    result = await UnityWebRequestHelper.ExecutePostAsync<HideoutDto>("Hideouts",
                        new { Building = buildingId }, session,
                        cancellationToken: operationToken);
                }
                finally
                {
                    quests.ReleaseInventoryQuestMutation();
                }

                Debug.Log($"Hideout request completed. ClientId: {OwnerClientId}, Building: {buildingId}, Outcome: {result.Outcome}.");

                // The accepted write belongs to the server, even if its owner has left this room.
                if (!operationToken.IsCancellationRequested && building != null)
                {
                    building.Apply(result.Buildings.Where(x => x.Id == buildingId).Single(), result.CurrentTime, true);
                }

                if (Alive(lifetime))
                {
                    ResultClientRpc(JsonSerializer.Serialize(result), instanceId, OwnerClientId.ToClientRpcParams());
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hideout request failed; reload before retry. ClientId: {OwnerClientId}, Error: {exception.GetType().Name}.");

                if (Alive(lifetime))
                {
                    ResultClientRpc(string.Empty, instanceId, OwnerClientId.ToClientRpcParams());
                }
            }
            finally
            {
                if (Alive(lifetime))
                {
                    _busy = false;
                }
            }
        }

        [ClientRpc]
        private void ResultClientRpc(string json, int instanceId, ClientRpcParams rpcParams = default)
        {
            if (string.IsNullOrEmpty(json))
            {
                GetComponent<CharacterInventory>().ReloadAuthoritativeInventory();
                GetComponent<CharacterStash>().RefreshQuestsAsync().Forget();
                LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey("HideoutUnavailable"), color: ColorUI.Red).Forget();

                return;
            }

            var travel = GetComponent<DungeonTravel>();

            var result = JsonSerializer.Deserialize<HideoutDto>(json);

            if (travel.IsViewingHideout(instanceId))
            {
                RestoreRoom(json, 0);
            }

            GetComponent<CharacterInventory>().ApplyAuthoritativeInventory(result.CharacterInventory);
            GetComponent<CharacterInventory>().ReloadAuthoritativeInventory();
            GetComponent<CharacterStash>().RefreshQuestsAsync().Forget();

            if (result.Outcome != "Applied" && result.Outcome != "AlreadyBuilt")
            {
                LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey("Hideout" + result.Outcome), color: ColorUI.Red).Forget();
            }
        }

        private bool Alive(CancellationToken token) => IsSpawned && _lifetime != null
            && _lifetime.Token == token && !token.IsCancellationRequested;

        public override void OnNetworkDespawn()
        {
            _lifetime?.Cancel();

            if (IsOwner)
            {
                CursorUI.Instance?.ShowDefault();
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();

            base.OnDestroy();
        }
    }
}
