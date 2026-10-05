using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Quest;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Subscriptions;
using Assets.Scripts.Areas.Quest.Mono;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Trade.Mono;
using Assets.Scripts.Areas.Trade.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Inventory.Mono
{
    public sealed class CharacterStash : NetworkBehaviour
    {
        public static CharacterStash Local { get; private set; }

        private CancellationTokenSource _lifetime;
        private bool _serverBusy;
        private bool _clientBusy;
        private bool _wantsOpen;
        private int _viewGeneration;

        public bool IsBusy => _clientBusy;

        public override void OnNetworkSpawn()
        {
            _lifetime?.Dispose();
            _lifetime = new CancellationTokenSource();
            _serverBusy = false;
            _clientBusy = false;
            _wantsOpen = false;

            if (IsOwner)
            {
                Local = this;
            }
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            var station = StashStation.Instance;
            var available = station != null && station.IsInRange(transform)
                && !GetComponent<DungeonTravel>().IsTransitioning;

            if (_wantsOpen && (!available || Keyboard.current?.escapeKey.wasPressedThisFrame == true
                || TradeUI.Instance?.HasSession == true))
            {
                Close();
            }

            var mouse = Mouse.current;
            var hover = available && mouse != null && Camera.main != null && !InputFocusUI.IsAnyInputFocused
                && !(EventSystem.current?.IsPointerOverGameObject() ?? false)
                && Physics.Raycast(Camera.main.ScreenPointToRay(mouse.position.ReadValue()), out var hit)
                && hit.collider.GetComponent<StashStation>() == station;

            if (!hover)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            CursorUI.Instance.ShowPointer();

            if (mouse.rightButton.wasPressedThisFrame && !_clientBusy)
            {
                if (TradeUI.Instance?.OpenAfterTrade(Open) != true)
                {
                    Open();
                }
            }
        }

        private void Open()
        {
            if (!IsSpawned || StashStation.Instance == null || !StashStation.Instance.IsInRange(transform))
            {
                return;
            }

            _wantsOpen = true;
            _viewGeneration++;
            Request(StashOperationEnum.Read, -1, null, null);
        }

        public void Close()
        {
            _wantsOpen = false;
            _viewGeneration++;
            StashUI.Instance?.Hide();
        }

        public void Request(StashOperationEnum operation, int source, int? target, InventoryItemDto item)
        {
            if (!IsSpawned || !_wantsOpen || _clientBusy)
            {
                return;
            }

            _clientBusy = true;
            AccessServerRpc(operation, StashUI.Instance?.Dto?.Revision ?? 0, source, target ?? -1,
                item?.Type ?? InventoryItemEnum.None, item?.Count ?? 0, _viewGeneration);
        }

        [ServerRpc]
        private void AccessServerRpc(StashOperationEnum operation, long revision, int source, int target,
            InventoryItemEnum expectedType, int expectedCount, int generation)
        {
            var station = StashStation.Instance;
            var allowed = !_serverBusy && Enum.IsDefined(typeof(StashOperationEnum), operation)
                && UserManager.Instance.TryGetPlayerSessionId(OwnerClientId, out _)
                && UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character) && character.Health > 0
                && DungeonTravel.CanInteract(OwnerClientId) && DungeonTravel.GetInstanceId(OwnerClientId) == 0
                && station != null && station.IsInRange(transform)
                && !TradeServerState.IsInventoryReserved(OwnerClientId);

            if (!allowed)
            {
                Debug.LogWarning($"Stash access rejected. ClientId: {OwnerClientId}, Operation: {operation}, Busy: {_serverBusy}.");
                ResultClientRpc(string.Empty, generation, OwnerClientId.ToClientRpcParams());

                return;
            }

            _serverBusy = true;
            var request = new AccessCharacterStashCommand
            {
                Operation = operation,
                Revision = revision,
                SourceIndex = source,
                TargetIndex = target < 0 ? null : target,
                ExpectedType = expectedType,
                ExpectedCount = expectedCount
            };

            AccessAsync(request, UserManager.Instance.GetPlayerSessionId(OwnerClientId), generation, _lifetime.Token).Forget();
        }

        private async UniTask AccessAsync(AccessCharacterStashCommand request, string session, int generation, CancellationToken lifetime)
        {
            using var mutation = TradeServerState.TrackInventoryMutation(OwnerClientId);
            var operationToken = TradeCommitCoordinator.GetServerLifetimeCancellationToken();
            var quests = GetComponent<CharacterQuests>();

            try
            {
                await quests.WaitForInventoryQuestMutationAsync(operationToken);

                CharacterStashDto result;

                try
                {
                    result = await UnityWebRequestHelper.ExecutePostAsync<CharacterStashDto>(
                        "CharacterStashes", request, session, cancellationToken: operationToken);
                }
                finally
                {
                    quests.ReleaseInventoryQuestMutation();
                }

                Debug.Log($"Stash operation completed. ClientId: {OwnerClientId}, Operation: {request.Operation}, Status: {result.Status}, Revision: {result.Revision}.");

                if (Alive(lifetime))
                {
                    ResultClientRpc(JsonSerializer.Serialize(result), generation, OwnerClientId.ToClientRpcParams());
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Stash request failed. ClientId: {OwnerClientId}, Operation: {request.Operation}, Error: {exception.GetType().Name}.");

                if (Alive(lifetime))
                {
                    ResultClientRpc(string.Empty, generation, OwnerClientId.ToClientRpcParams());
                }
            }
            finally
            {
                if (Alive(lifetime))
                {
                    _serverBusy = false;
                }
            }
        }

        [ClientRpc]
        private void ResultClientRpc(string json, int generation, ClientRpcParams rpcParams = default)
        {
            _clientBusy = false;
            var result = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<CharacterStashDto>(json);

            if (result?.CharacterInventory != null)
            {
                GetComponent<CharacterInventory>().ApplyAuthoritativeInventory(result.CharacterInventory);
            }

            GetComponent<CharacterInventory>().ReloadAuthoritativeInventory();
            RefreshQuestsAsync(_lifetime.Token).Forget();

            if (result == null || result.Status != StashStatusEnum.Applied)
            {
                StashUI.Instance.ShowFailure(result?.Status);
                if (result?.Status != StashStatusEnum.Full)
                {
                    Close();
                }

                return;
            }

            if (_wantsOpen && generation == _viewGeneration && StashStation.Instance != null
                && StashStation.Instance.IsInRange(transform) && !GetComponent<DungeonTravel>().IsTransitioning)
            {
                StashUI.Instance.Show(result);
            }
        }

        public UniTask RefreshQuestsAsync() => RefreshQuestsAsync(_lifetime.Token);

        private async UniTask RefreshQuestsAsync(CancellationToken lifetime)
        {
            var previousQuests = QuestManager.Instance.CharacterQuests;
            var applied = await QuestManager.Instance.LoadCharacterQuestsAsync(UserManager.Instance.SelectedCharacterId, lifetime);

            if (!Alive(lifetime) || !applied)
            {
                return;
            }

            var previousStatuses = previousQuests?.ToDictionary(x => x.Id, x => x.Status);

            foreach (var quest in QuestManager.Instance.CharacterQuests)
            {
                if (quest.Status == CharacterQuestStatusEnum.Completed)
                {
                    if (previousStatuses != null && previousStatuses.TryGetValue(quest.Id, out var previousCompletionStatus)
                        && previousCompletionStatus != CharacterQuestStatusEnum.Completed)
                    {
                        GetComponent<CharacterQuests>().ApplyCompletedQuest(quest.Id);
                    }

                    continue;
                }

                QuestUI.Instance.UpdateProgress(quest);

                if (previousStatuses != null && previousStatuses.TryGetValue(quest.Id, out var previousStatus)
                    && previousStatus != quest.Status
                    && (quest.Status == CharacterQuestStatusEnum.Accepted || quest.Status == CharacterQuestStatusEnum.Finished))
                {
                    FinishCharacterQuestSubscription.Instance.Invoke(quest.QuestId.ToString(), new FinishCharacterQuestSubscriptionEvent
                    {
                        IsFinished = quest.Status == CharacterQuestStatusEnum.Finished
                    });
                }
            }
        }

        private bool Alive(CancellationToken token) => IsSpawned && _lifetime != null
            && _lifetime.Token == token && !token.IsCancellationRequested;

        public override void OnNetworkDespawn()
        {
            _lifetime?.Cancel();

            if (Local == this)
            {
                Close();
                CursorUI.Instance?.ShowDefault();
                Local = null;
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;

            base.OnDestroy();
        }
    }
}
