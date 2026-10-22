using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Inventory.Mono;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
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

        public static CharacterHideout Local { get; private set; }

        public bool IsBusy => _busy;

        public HideoutBuilding SelectedBuilding => _clientBuilding;

        public override void OnNetworkSpawn()
        {
            _lifetime = new CancellationTokenSource();

            if (IsOwner)
            {
                Local = this;
            }
        }

        public async UniTask LoadRoomAsync(LocationEnvironment room, int instanceId, CancellationToken travelToken)
        {
            var lifetime = _lifetime.Token;
            var quests = GetComponent<CharacterQuests>();
            using var mutation = TradeServerState.TrackInventoryMutation(OwnerClientId);
            var operationToken = TradeCommitCoordinator.GetServerLifetimeCancellationToken();

            await quests.WaitForInventoryQuestMutationAsync(operationToken);

            HideoutDto result;

            try
            {
                if (!Alive(lifetime) || travelToken.IsCancellationRequested)
                {
                    return;
                }

                result = await UnityWebRequestHelper.ExecutePostAsync<HideoutDto>("Hideouts",
                    new { Building = HideoutBuildingEnum.None }, UserManager.Instance.GetPlayerSessionId(OwnerClientId), cancellationToken: operationToken);
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
                building.Bind(this, instanceId);
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

            Debug.Log($"Hideout client projection updated. Buildings: {_clientBuildings.Length}.");
        }

        private void Update()
        {
            if (!IsOwner || !IsSpawned)
            {
                return;
            }

            var travel = GetComponent<DungeonTravel>();
            var available = _clientBuilding != null && _clientBuilding.CanManage
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
                && hoveredBuilding.CanManage && hoveredBuilding.IsInRange(transform);

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

                ShowPanel();
                Request(HideoutOperationEnum.Read);
            }
        }

        private void ShowPanel()
        {
            CraftingUI.Instance.ShowHideout(_clientBuilding.Definition, () =>
                Request(_clientBuilding.CanBuild ? HideoutOperationEnum.Build : HideoutOperationEnum.Upgrade));
        }

        public void Request(HideoutOperationEnum operation, int slot = -1, int inventorySlot = -1, InventoryItemDto item = null)
        {
            if (_clientBuilding == null || _busy)
            {
                return;
            }

            AccessServerRpc(_clientBuilding.BuildingId, operation, slot, inventorySlot,
                item?.Type ?? InventoryItemEnum.None, item?.Count ?? 0, _clientBuilding.Definition.Farm?.Revision ?? 0);
        }

        public void Refresh(HideoutBuilding building)
        {
            if (_busy || !IsSpawned || TradeServerState.IsInventoryReserved(OwnerClientId) || !UserManager.Instance.TryGetPlayerSessionId(OwnerClientId, out var session))
            {
                return;
            }

            _busy = true;
            AccessAsync(building.BuildingId, building, building.InstanceId, session,
                _lifetime.Token, HideoutOperationEnum.Read).Forget();
        }

        public bool TryHarvest(GameObject crop)
        {
            var travel = GetComponent<DungeonTravel>();
            var building = travel.CurrentRoom?.GetComponentsInChildren<HideoutBuilding>(true)
                .Where(x => x.FindCrop(crop) >= 0).SingleOrDefault();

            if (building == null)
            {
                return false;
            }

            if (Vector3.Distance(transform.position, crop.transform.position) <= 2f)
            {
                Access(building.BuildingId, HideoutOperationEnum.Harvest, building.FindCrop(crop), -1,
                    InventoryItemEnum.None, 0, building.Definition.Farm.Revision);
            }

            return true;
        }

        [ServerRpc]
        private void AccessServerRpc(HideoutBuildingEnum buildingId, HideoutOperationEnum operation, int slot,
            int inventorySlot, InventoryItemEnum item, int count, long revision)
        {
            if (operation == HideoutOperationEnum.Harvest || !Enum.IsDefined(typeof(HideoutOperationEnum), operation))
            {
                Debug.LogWarning($"Hideout RPC operation rejected. ClientId: {OwnerClientId}, Operation: {operation}.");
                return;
            }

            Access(buildingId, operation, slot, inventorySlot, item, count, revision);
        }

        private void Access(HideoutBuildingEnum buildingId, HideoutOperationEnum operation, int slot,
            int inventorySlot, InventoryItemEnum item, int count, long revision)
        {
            var travel = GetComponent<DungeonTravel>();
            var building = travel.CurrentRoom?.GetComponentsInChildren<HideoutBuilding>(true)
                .Where(x => x.BuildingId == buildingId).SingleOrDefault();
            var allowed = !_busy && Time.unscaledTime >= _nextRequest && building != null
                && travel.CurrentSceneName == nameof(LocationEnum.HideoutScene)
                && DungeonTravel.CanInteract(OwnerClientId)
                && UserManager.Instance.TryGetPlayerSessionId(OwnerClientId, out _)
                && UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character) && character.Health > 0
                && !TradeServerState.IsInventoryReserved(OwnerClientId)
                && building.CanManage && building.IsInRange(transform);

            if (!allowed)
            {
                Debug.LogWarning($"Hideout request rejected. ClientId: {OwnerClientId}, Building: {buildingId}, Busy: {_busy}.");
                ResultClientRpc(string.Empty, DungeonTravel.GetInstanceId(OwnerClientId), OwnerClientId.ToClientRpcParams());

                return;
            }

            _busy = true;
            _nextRequest = Time.unscaledTime + 0.25f;
            AccessAsync(buildingId, building, DungeonTravel.GetInstanceId(OwnerClientId),
                UserManager.Instance.GetPlayerSessionId(OwnerClientId), _lifetime.Token,
                operation, slot, inventorySlot, item, count, revision).Forget();
        }

        private async UniTask AccessAsync(HideoutBuildingEnum buildingId, HideoutBuilding building, int instanceId, string session, CancellationToken lifetime,
            HideoutOperationEnum operation, int slot = -1, int inventorySlot = -1,
            InventoryItemEnum item = InventoryItemEnum.None, int count = 0, long revision = 0)
        {
            using var mutation = TradeServerState.TrackInventoryMutation(OwnerClientId);
            var operationToken = TradeCommitCoordinator.GetServerLifetimeCancellationToken();
            var quests = GetComponent<CharacterQuests>();
            var respawnInterval = building.Interval(slot, item);

            try
            {
                await quests.WaitForInventoryQuestMutationAsync(operationToken);

                HideoutDto result;

                try
                {
                    result = await UnityWebRequestHelper.ExecutePostAsync<HideoutDto>("Hideouts",
                        new
                        {
                            Building = buildingId, Operation = operation, Slot = slot,
                            InventorySlot = inventorySlot, Item = item, Count = count, Revision = revision,
                            RespawnInterval = respawnInterval,
                            HarvestExperience = operation == HideoutOperationEnum.Harvest ? 50 : 0
                        }, session,
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
                    GetComponent<Assets.Scripts.Areas.Character.Mono.Player>()
                        .ApplyPersistedExperienceLevel(ExperienceTypeEnum.Herbalism, result.HerbalismLevel);

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

                if (CraftingUI.Instance.BuildHideout != null && _clientBuilding != null)
                {
                    ShowPanel();
                }
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

            if (Local == this)
            {
                Local = null;
                CraftingUI.Instance?.Hide();
            }

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
