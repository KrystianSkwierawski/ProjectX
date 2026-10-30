using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Mono;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Mono;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Quest.Subscriptions;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Trade.Mono;
using TradeController = Assets.Scripts.Areas.Trade.Mono.Trade;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Quest.Mono
{
    public class CharacterQuests : NetworkBehaviour
    {
        private const float _npcMaxDistance = 5f;

        private CancellationTokenSource _networkLifetimeCancellationTokenSource;
        private readonly SemaphoreSlim _questMutationSemaphore = new SemaphoreSlim(1, 1);
        private readonly HashSet<int> _presentedQuestCompletions = new HashSet<int>();
        private readonly HashSet<int> _presentedQuestAcceptances = new HashSet<int>();
        private StarterAssetsInputs _input;

        public static CharacterQuests Local { get; private set; }

        public bool IsAbandonPending { get; private set; }

        private int _pendingAbandonId;
        private bool _serverAbandonPending;

        public UniTask WaitForInventoryQuestMutationAsync(CancellationToken cancellationToken)
        {
            return _questMutationSemaphore.WaitAsync(cancellationToken).AsUniTask();
        }

        public void ReleaseInventoryQuestMutation()
        {
            _questMutationSemaphore.Release();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _presentedQuestCompletions.Clear();
            _presentedQuestAcceptances.Clear();
            _serverAbandonPending = false;

            if (IsOwner)
            {
                Local = this;
                IsAbandonPending = false;
                _pendingAbandonId = 0;
            }

            CancelNetworkLifetime();
            _networkLifetimeCancellationTokenSource?.Dispose();
            _networkLifetimeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            if (!IsServer)
            {
                return;
            }

            var cancellationToken = GetNetworkLifetimeCancellationToken();

            CheckCharacterQuestSubscription.Instance.Subscribe(
                OwnerClientId.ToString(),
                e =>
                {
                    // Mixed goals also synchronize inventory. Accepted progress retains the
                    // server lifetime and mutation barrier; RPC work uses the spawn lifetime.
                    var operationCancellationToken = TradeCommitCoordinator.GetServerLifetimeCancellationToken();

                    CheckProgressSequentiallyAsync(
                            e.GameObjectName,
                            e.QuestType,
                            e.Progress,
                            OwnerClientId,
                            e.PlayerSessionId,
                            operationCancellationToken,
                            cancellationToken)
                        .SuppressCancellationThrow()
                        .Forget();
                });
        }

        [ServerRpc]
        private void AbandonQuestServerRpc(int characterQuestId)
        {
            if (_serverAbandonPending || TradeServerState.IsInventoryReserved(OwnerClientId))
            {
                AbandonQuestClientRpc(characterQuestId, false, OwnerClientId.ToClientRpcParams());

                return;
            }

            _serverAbandonPending = true;

            AbandonQuestSequentiallyAsync(
                    characterQuestId,
                    UserManager.Instance.GetPlayerSessionId(OwnerClientId),
                    TradeCommitCoordinator.GetServerLifetimeCancellationToken(),
                    GetNetworkLifetimeCancellationToken())
                .SuppressCancellationThrow()
                .Forget();
        }

        public void RequestAbandon(int characterQuestId)
        {
            if (!IsOwner || !IsSpawned || IsAbandonPending)
            {
                return;
            }

            var quest = QuestManager.Instance.CharacterQuests?
                .Where(quest => quest.Id == characterQuestId)
                .SingleOrDefault();

            if (quest == null || quest.Status is not (CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished))
            {
                return;
            }

            if (TradeController.Local?.IsInventoryLocked == true)
            {
                TradeController.NotifyInventoryLocked(OwnerClientId);

                return;
            }

            _pendingAbandonId = characterQuestId;
            IsAbandonPending = true;
            QuestManager.Instance.NotifyCharacterQuestsChanged();

            AbandonQuestServerRpc(characterQuestId);
        }

        private async UniTask AbandonQuestSequentiallyAsync(
            int characterQuestId,
            string playerSessionId,
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            try
            {
                using var mutation = TradeServerState.TrackInventoryMutation(OwnerClientId);

                await _questMutationSemaphore.WaitAsync(operationCancellationToken);

                AbandonCharacterQuestDto result;

                try
                {
                    result = await QuestManager.Instance.AbandonAsync(characterQuestId, playerSessionId, operationCancellationToken);
                }
                finally
                {
                    _questMutationSemaphore.Release();
                }

                Debug.Log($"Quest abandonment settled. ClientId: {OwnerClientId}, CharacterQuestId: {characterQuestId}, Abandoned: {result.Abandoned}.");

                if (CanUseNetworkLifetime(networkLifetimeCancellationToken))
                {
                    AbandonQuestClientRpc(characterQuestId, result.Abandoned, OwnerClientId.ToClientRpcParams());
                }
            }
            catch (Exception exception) when (!(exception is OperationCanceledException && operationCancellationToken.IsCancellationRequested))
            {
                Debug.LogWarning($"Quest abandonment failed. ClientId: {OwnerClientId}, CharacterQuestId: {characterQuestId}, Error: {exception.GetType().Name}.");

                if (CanUseNetworkLifetime(networkLifetimeCancellationToken))
                {
                    AbandonQuestClientRpc(characterQuestId, false, OwnerClientId.ToClientRpcParams());
                }
            }
            finally
            {
                if (IsCurrentNetworkLifetime(networkLifetimeCancellationToken))
                {
                    _serverAbandonPending = false;
                }
            }
        }

        [ClientRpc]
        private void AbandonQuestClientRpc(int characterQuestId, bool abandoned, ClientRpcParams rpcParams = default)
        {
            if (_pendingAbandonId == characterQuestId)
            {
                IsAbandonPending = false;
                _pendingAbandonId = 0;
            }

            if (abandoned)
            {
                QuestManager.Instance.InvalidatePendingCharacterQuestLoads();

                var quest = QuestManager.Instance.CharacterQuests?
                    .Where(quest => quest.Id == characterQuestId)
                    .SingleOrDefault();

                if (quest != null)
                {
                    QuestManager.Instance.CharacterQuests.Remove(quest);
                    QuestUI.Instance.RemoveQuest(quest.QuestId);
                }

                QuestManager.Instance.NotifyCharacterQuestsChanged();

                return;
            }

            QuestManager.Instance.NotifyCharacterQuestsChanged();

            LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey(TranslateKeyEnum.QuestAbandonFailed), color: ColorUI.Error).Forget();
            RefreshQuestsAfterAbandonFailureAsync(GetNetworkLifetimeCancellationToken()).SuppressCancellationThrow().Forget();
        }

        private async UniTask RefreshQuestsAfterAbandonFailureAsync(CancellationToken cancellationToken)
        {
            try
            {
                var characterId = UserManager.Instance.SelectedCharacterId;
                var loaded = await QuestManager.Instance.LoadCharacterQuestsAsync(characterId, cancellationToken);

                if (!loaded || !CanUseNetworkLifetime(cancellationToken))
                {
                    return;
                }

                // Rebuild the tracker from the authoritative snapshot, including an
                // abandonment whose HTTP response was lost before reaching the server.
                foreach (var quest in QuestManager.Instance.Quests)
                {
                    QuestUI.Instance.RemoveQuest(quest.Id);
                }

                foreach (var quest in QuestManager.Instance.CharacterQuests
                    .Where(quest => quest.Status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished))
                {
                    QuestUI.Instance.Accept(quest);
                }
            }
            catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                Debug.LogWarning($"Quest reload after abandonment failed. Error: {exception.GetType().Name}.");
            }
        }

        [ServerRpc]
        private void CompleteQuestServerRpc(int characterQuestId)
        {
            if (TradeServerState.IsInventoryReserved(OwnerClientId))
            {
                TradeController.NotifyInventoryLocked(OwnerClientId);

                return;
            }

            var networkLifetimeCancellationToken = GetNetworkLifetimeCancellationToken();

            CompleteQuestSequentiallyAsync(
                    characterQuestId,
                    UserManager.Instance.GetPlayerSessionId(OwnerClientId),
                    TradeCommitCoordinator.GetServerLifetimeCancellationToken(),
                    networkLifetimeCancellationToken)
                .SuppressCancellationThrow()
                .Forget();
        }

        [ServerRpc]
        private void AcceptQuestServerRpc(QuestEnum questId)
        {
            var isCollectQuest = QuestManager.Instance.Quests
                .Any(x => x.Id == questId && x.Requirements.Any(requirement => requirement.Type == QuestTypeEnum.Collect));

            if (isCollectQuest && TradeServerState.IsInventoryReserved(OwnerClientId))
            {
                TradeController.NotifyInventoryLocked(OwnerClientId);

                return;
            }

            var networkLifetimeCancellationToken = GetNetworkLifetimeCancellationToken();
            var operationCancellationToken = isCollectQuest
                ? TradeCommitCoordinator.GetServerLifetimeCancellationToken()
                : networkLifetimeCancellationToken;

            AcceptQuestSequentiallyAsync(
                    questId,
                    UserManager.Instance.GetPlayerSessionId(OwnerClientId),
                    isCollectQuest,
                    operationCancellationToken,
                    networkLifetimeCancellationToken)
                .SuppressCancellationThrow()
                .Forget();
        }

        private async UniTask AcceptQuestSequentiallyAsync(
            QuestEnum questId,
            string playerSessionId,
            bool tracksInventory,
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            using var inventoryMutation = tracksInventory
                ? TradeServerState.TrackInventoryMutation(OwnerClientId)
                : null;

            await _questMutationSemaphore.WaitAsync(operationCancellationToken);

            try
            {
                await AcceptQuestAsync(
                    questId,
                    playerSessionId,
                    operationCancellationToken,
                    networkLifetimeCancellationToken);
            }
            finally
            {
                _questMutationSemaphore.Release();
            }
        }

        private async UniTask AcceptQuestAsync(
            QuestEnum questId,
            string playerSessionId,
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            if (!CanUseNetworkLifetime(networkLifetimeCancellationToken))
            {
                return;
            }

            var characterQuest = await QuestManager.Instance.AcceptCharacterQuestAsync(
                questId,
                playerSessionId,
                operationCancellationToken);

            if (!CanUseNetworkLifetime(networkLifetimeCancellationToken))
            {
                return;
            }

            AcceptQuestClientRpc(
                characterQuest.Id,
                characterQuest.QuestId,
                characterQuest.Status,
                JsonConvert.SerializeObject(characterQuest.ObjectiveProgress),
                OwnerClientId.ToClientRpcParams());
        }

        [ClientRpc]
        private void AcceptQuestClientRpc(
            int characterQuestId,
            QuestEnum questId,
            CharacterQuestStatusEnum status,
            string objectiveProgressJson,
            ClientRpcParams rpcParams = default)
        {
            ApplyAcceptedQuest(new CharacterQuestDto
            {
                Id = characterQuestId,
                QuestId = questId,
                Status = status,
                ObjectiveProgress = JsonConvert.DeserializeObject<Dictionary<string, int>>(objectiveProgressJson)
            });
        }

        private void ApplyAcceptedQuest(CharacterQuestDto acceptedQuest)
        {
            if (!_presentedQuestAcceptances.Add(acceptedQuest.Id))
            {
                return;
            }

            QuestManager.Instance.InvalidatePendingCharacterQuestLoads();
            var characterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => x.Id == acceptedQuest.Id)
                .FirstOrDefault();

            if (characterQuest == null)
            {
                characterQuest = acceptedQuest;
                QuestManager.Instance.CharacterQuests.Add(characterQuest);
            }

            if (characterQuest.Status == CharacterQuestStatusEnum.Completed)
            {
                return;
            }

            Debug.Log($"Presenting quest acceptance. CharacterQuestId: {characterQuest.Id}.");
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.QuestAccepted, 0.5f);

            QuestUI.Instance.Accept(characterQuest);

            AcceptQuestSubscription.Instance.InvokeAndUnsubscribe(characterQuest.QuestId.ToString(), new AddQuestSubscriptionEvent
            {
                CharacterQuest = characterQuest
            });

            if (characterQuest.Status == CharacterQuestStatusEnum.Finished)
            {
                FinishCharacterQuestSubscription.Instance.Invoke(characterQuest.QuestId.ToString(), new FinishCharacterQuestSubscriptionEvent
                {
                    IsFinished = true
                });
            }

            QuestManager.Instance.NotifyCharacterQuestsChanged();
        }

        private async UniTask CompleteQuestAsync(
            int characterQuestId,
            string playerSessionId,
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            var result = await QuestManager.Instance.CompleteAsync(
                characterQuestId,
                playerSessionId,
                operationCancellationToken);

            if (!CanUseNetworkLifetime(networkLifetimeCancellationToken))
            {
                return;
            }

            if (result.Status != CompleteCharacterQuestStatusEnum.Applied)
            {
                Debug.Log($"Quest completion rejected. ClientId: {OwnerClientId}, CharacterQuestId: {characterQuestId}, Status: {result.Status}.");
                QuestInventoryChangedClientRpc(result.Status, OwnerClientId.ToClientRpcParams());

                return;
            }

            var quest = QuestManager.Instance.Quests
                .Where(x => x.Id == result.QuestId)
                .Single();

            if (quest.Requirements.Any(x => x.Type == QuestTypeEnum.Collect) || quest.ItemRewards.Length > 0)
            {
                UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
                {
                    Request = new UpdateCharacterInventoryCommand
                    {
                        Add = quest.ItemRewards,
                    },
                    PlayerSessionId = playerSessionId,
                    PersistInApi = false,
                });
            }

            GetComponent<Player>()?.ApplyPersistedExperienceLevel(ExperienceTypeEnum.Main, result.Level);

            foreach (var progress in result.CollectProgress)
            {
                UpdateQuestClientRpc(progress.CharacterQuestId, JsonConvert.SerializeObject(progress.ObjectiveProgress), progress.Status, OwnerClientId.ToClientRpcParams());
            }

            Debug.Log($"Quest rewards applied. ClientId: {OwnerClientId}, QuestId: {result.QuestId}, Experience: {result.Reward}, ItemRewards: {quest.ItemRewards.Length}.");
            CompleteQuestClientRpc(characterQuestId, OwnerClientId.ToClientRpcParams());
        }

        [ClientRpc]
        private void QuestInventoryChangedClientRpc(CompleteCharacterQuestStatusEnum status, ClientRpcParams rpcParams = default)
        {
            GetComponent<CharacterInventory>()?.ReloadAuthoritativeInventory();
            LogUI.Instance.ShowAsync(
                    TranslateManager.Instance.GetByKey(status == CompleteCharacterQuestStatusEnum.InventoryFull
                        ? TranslateKeyEnum.InventoryFull : TranslateKeyEnum.QuestInventoryChanged),
                    color: ColorUI.Error)
                .Forget();
        }

        [ClientRpc]
        private void CompleteQuestClientRpc(int characterQuestId, ClientRpcParams rpcParams = default)
        {
            ApplyCompletedQuest(characterQuestId);
        }

        public void ApplyCompletedQuest(int characterQuestId)
        {
            var characterQuest = QuestManager.Instance.CharacterQuests?
                .Where(x => x.Id == characterQuestId)
                .FirstOrDefault();

            if (characterQuest == null || !_presentedQuestCompletions.Add(characterQuestId))
            {
                return;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.QuestCompleted, 0.5f);
            QuestManager.Instance.InvalidatePendingCharacterQuestLoads();
            characterQuest.Status = CharacterQuestStatusEnum.Completed;

            Debug.Log($"Presenting quest completion. CharacterQuestId: {characterQuestId}.");

            QuestUI.Instance.Complete(characterQuest);
            CompleteQuestSubscription.Instance.InvokeAndUnsubscribe(
                characterQuest.QuestId.ToString(),
                new CompleteQuestSubscriptionEvent());

            QuestManager.Instance.NotifyCharacterQuestsChanged();
        }

        private async UniTask CompleteQuestSequentiallyAsync(
            int characterQuestId,
            string playerSessionId,
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            using var inventoryMutation = TradeServerState.TrackInventoryMutation(OwnerClientId);

            await _questMutationSemaphore.WaitAsync(operationCancellationToken);

            try
            {
                await CompleteQuestAsync(
                    characterQuestId,
                    playerSessionId,
                    operationCancellationToken,
                    networkLifetimeCancellationToken);
            }
            finally
            {
                _questMutationSemaphore.Release();
            }
        }

        private void Start()
        {
            if (IsOwner)
            {
                _input = GetComponent<StarterAssetsInputs>();

                QuestUI.Instance.QuestAcceptButton.onClick.AddListener(() =>
                {
                    var questNpc = QuestUI.Instance.DisplayedNpc;

                    if (questNpc == null || questNpc.Quest == null)
                    {
                        return;
                    }

                    var questId = questNpc.Quest.Id;

                    QuestUI.Instance.Hide();

                    var characterQuest = QuestManager.Instance.CharacterQuests
                        .Where(x => x.QuestId == questId)
                        .FirstOrDefault();

                    if (characterQuest?.Status == CharacterQuestStatusEnum.Finished)
                    {
                        CompleteQuest(characterQuest);
                    }
                    else if (characterQuest == null)
                    {
                        AcceptQuestServerRpc(questId);
                    }
                });
            }

        }

        private async UniTask CheckProgressSequentiallyAsync(
            string gameObjectName,
            QuestTypeEnum questType,
            int progress,
            ulong clientId,
            string playerSessionId,
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            using var inventoryMutation = TradeServerState.DeferInventoryMutation(clientId);

            if (!inventoryMutation.TryBegin())
            {
                Debug.Log($"Quest progress deferred until trade releases inventory. ClientId: {clientId}, QuestType: {questType}, Target: {gameObjectName}.");

                await UniTask.WaitUntil(inventoryMutation.TryBegin, cancellationToken: operationCancellationToken);

                Debug.Log($"Deferred quest progress admitted. ClientId: {clientId}, QuestType: {questType}, Target: {gameObjectName}.");
            }

            await _questMutationSemaphore.WaitAsync(operationCancellationToken);

            try
            {
                await CheckProgressAsync(
                    gameObjectName,
                    questType,
                    progress,
                    clientId,
                    playerSessionId,
                    operationCancellationToken,
                    networkLifetimeCancellationToken);
            }
            finally
            {
                _questMutationSemaphore.Release();
            }
        }

        private async UniTask CheckProgressAsync(
            string gameObjectName,
            QuestTypeEnum questType,
            int progress,
            ulong clientId,
            string playerSessionId,
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            var quests = QuestManager.Instance.Quests
                .Where(x => x.Requirements.Any(requirement => requirement.Target == gameObjectName && requirement.Type == questType))
                .ToArray();

            if (quests.Length == 0)
            {
                Debug.Log($"Quest not found. GameObjectName: {gameObjectName}, QuestType: {questType}");

                return;
            }

            foreach (var quest in quests)
            {
                operationCancellationToken.ThrowIfCancellationRequested();

                var requirement = quest.Requirements
                    .Where(x => x.Target == gameObjectName && x.Type == questType)
                    .Single();

                var result = await QuestManager.Instance.CheckProgressAsync(
                    quest.Id,
                    requirement.Id,
                    progress,
                    playerSessionId,
                    operationCancellationToken);

                Debug.Log($"Quest progress persisted. ClientId: {clientId}, QuestId: {quest.Id}, Status: {result.Status}, Progress: {result.Progress}.");

                if (CanUseNetworkLifetime(networkLifetimeCancellationToken) && result.Status != CharacterQuestStatusEnum.None)
                {
                    UpdateQuestClientRpc(result.CharacterQuestId, JsonConvert.SerializeObject(result.ObjectiveProgress), result.Status, clientId.ToClientRpcParams());
                }
            }
        }

        [ClientRpc]
        private void UpdateQuestClientRpc(int characterQuestId, string objectiveProgressJson, CharacterQuestStatusEnum status, ClientRpcParams rpcParams = default)
        {
            Debug.Log($"UpdateQuestLogClientRpc: {characterQuestId}");

            var characterQuest = QuestManager.Instance.CharacterQuests?
                .Where(x => x.Id == characterQuestId)
                .FirstOrDefault();

            if (characterQuest == null)
            {
                Debug.LogWarning($"Ignoring quest progress for unknown character quest: {characterQuestId}");

                return;
            }

            if (characterQuest.Status == CharacterQuestStatusEnum.Completed
                && status != CharacterQuestStatusEnum.Completed)
            {
                Debug.Log($"Ignoring stale quest progress for completed character quest: {characterQuestId}");

                return;
            }

            var previousStatus = characterQuest.Status;

            QuestManager.Instance.InvalidatePendingCharacterQuestLoads();
            characterQuest.ObjectiveProgress = JsonConvert.DeserializeObject<Dictionary<string, int>>(objectiveProgressJson);
            characterQuest.Progress = characterQuest.ObjectiveProgress.Values.Sum();
            characterQuest.Status = status;

            QuestUI.Instance.UpdateProgress(characterQuest);

            QuestManager.Instance.NotifyCharacterQuestsChanged();

            if (status != previousStatus)
            {
                FinishCharacterQuestSubscription.Instance.Invoke(characterQuest.QuestId.ToString(), new FinishCharacterQuestSubscriptionEvent
                {
                    IsFinished = status == CharacterQuestStatusEnum.Finished
                });
            }
        }

        private void CompleteQuest(CharacterQuestDto characterQuest)
        {
            if (TradeController.Local?.IsInventoryLocked == true)
            {
                TradeController.NotifyInventoryLocked(OwnerClientId);

                return;
            }

            CompleteQuestServerRpc(characterQuest.Id);
        }

        private void Update()
        {
            if (!IsOwner || QuestManager.Instance.CharacterQuests == null)
            {
                return;
            }

            var displayedNpc = QuestUI.Instance.DisplayedNpc;

            if ((displayedNpc != null && _input.Move != Vector2.zero && displayedNpc.transform.IsFarToTarget(transform.gameObject, _npcMaxDistance))
                || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                QuestUI.Instance.Hide();

                return;
            }

            CheckQuestNpcClicked();
        }

        private void CheckQuestNpcClicked()
        {
            var mouse = Mouse.current;

            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

            var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "QuestNpc";

            if (!hover)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            if (hit.transform.IsFarToTarget(transform.gameObject, _npcMaxDistance))
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            var questNpc = hit.transform.GetComponent<QuestNpc>();

            if (questNpc == null || questNpc.Quest == null)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            if (questNpc.CharacterQuest?.Status == CharacterQuestStatusEnum.Completed)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            CursorUI.Instance.ShowPointer();

            if (mouse.rightButton.wasPressedThisFrame)
            {
                QuestUI.Instance.Show(questNpc);
            }
        }

        public override void OnNetworkDespawn()
        {
            CancelNetworkLifetime();

            ClearLocal();

            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            CancelNetworkLifetime();
            ClearLocal();
            _networkLifetimeCancellationTokenSource?.Dispose();
            _networkLifetimeCancellationTokenSource = null;

            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnDestroy();
        }

        private void ClearLocal()
        {
            if (Local == this)
            {
                IsAbandonPending = false;
                Local = null;
            }
        }

        private CancellationToken GetNetworkLifetimeCancellationToken()
        {
            return _networkLifetimeCancellationTokenSource?.Token ?? new CancellationToken(canceled: true);
        }

        private bool IsCurrentNetworkLifetime(CancellationToken cancellationToken)
        {
            return _networkLifetimeCancellationTokenSource != null
                && _networkLifetimeCancellationTokenSource.Token == cancellationToken;
        }

        private bool CanUseNetworkLifetime(CancellationToken cancellationToken)
        {
            return IsCurrentNetworkLifetime(cancellationToken)
                && !cancellationToken.IsCancellationRequested
                && IsSpawned;
        }

        private void CancelNetworkLifetime()
        {
            if (_networkLifetimeCancellationTokenSource?.IsCancellationRequested == false)
            {
                _networkLifetimeCancellationTokenSource.Cancel();
            }
        }
    }
}
