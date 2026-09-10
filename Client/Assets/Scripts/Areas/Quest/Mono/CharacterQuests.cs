using System;
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
        private QuestNpc _questNpc;
        private StarterAssetsInputs _input;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

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
                    // Collect persistence must keep the trade barrier until the request settles,
                    // even if the participant despawns. RPC/UI work still uses the spawn token.
                    var operationCancellationToken = e.QuestType == QuestTypeEnum.Collect
                        ? TradeCommitCoordinator.GetServerLifetimeCancellationToken()
                        : cancellationToken;

                    CheckProgressSequentiallyAsync(
                            e.GameObjectName,
                            e.QuestType,
                            e.Progress,
                            OwnerClientId,
                            operationCancellationToken,
                            cancellationToken)
                        .SuppressCancellationThrow()
                        .Forget();
                });
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
                .Any(x => x.Id == questId && x.Type == QuestTypeEnum.Collect);

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
                characterQuest.Progress,
                OwnerClientId.ToClientRpcParams());
        }

        [ClientRpc]
        private void AcceptQuestClientRpc(
            int characterQuestId,
            QuestEnum questId,
            CharacterQuestStatusEnum status,
            int progress,
            ClientRpcParams rpcParams = default)
        {
            var characterQuest = new CharacterQuestDto
            {
                Id = characterQuestId,
                QuestId = questId,
                Status = status,
                Progress = progress
            };

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.QuestAccepted, 0.5f);

            QuestManager.Instance.CharacterQuests.Add(characterQuest);

            QuestUI.Instance.Accept(characterQuest);

            AcceptQuestSubscription.Instance.InvokeAndUnsubscribe(questId.ToString(), new AddQuestSubscriptionEvent
            {
                CharacterQuest = characterQuest
            });

            if (status == CharacterQuestStatusEnum.Finished)
            {
                FinishCharacterQuestSubscription.Instance.Invoke(questId.ToString(), new FinishCharacterQuestSubscriptionEvent
                {
                    IsFinished = true
                });
            }
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
                QuestInventoryChangedClientRpc(OwnerClientId.ToClientRpcParams());

                return;
            }

            var quest = QuestManager.Instance.Quests
                .Where(x => x.Id == result.QuestId)
                .Single();

            if (quest.Type == QuestTypeEnum.Collect)
            {
                UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
                {
                    Request = new UpdateCharacterInventoryCommand
                    {
                        Remove = new InventoryItemDto[]
                        {
                            new InventoryItemDto
                            {
                                Type = Enum.Parse<InventoryItemEnum>(quest.GameObjectName),
                                Count = quest.Requirement,
                            }
                        }
                    },
                    PlayerSessionId = playerSessionId,
                    PersistInApi = false,
                });
            }

            GetComponent<Player>()?.ApplyPersistedExperienceLevel(ExperienceTypeEnum.Main, result.Level);

            CompleteQuestClientRpc(characterQuestId, OwnerClientId.ToClientRpcParams());
        }

        [ClientRpc]
        private void QuestInventoryChangedClientRpc(ClientRpcParams rpcParams = default)
        {
            GetComponent<CharacterInventory>()?.ReloadAuthoritativeInventory();
            LogUI.Instance.ShowAsync(
                    TranslateManager.Instance.GetByKey(TranslateKeyEnum.QuestInventoryChanged),
                    color: ColorUI.Error)
                .Forget();
        }

        [ClientRpc]
        private void CompleteQuestClientRpc(int characterQuestId, ClientRpcParams rpcParams = default)
        {
            var characterQuest = QuestManager.Instance.CharacterQuests?
                .FirstOrDefault(x => x.Id == characterQuestId);

            if (characterQuest == null || characterQuest.Status == CharacterQuestStatusEnum.Completed)
            {
                return;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.QuestCompleted, 0.5f);
            characterQuest.Status = CharacterQuestStatusEnum.Completed;

            QuestUI.Instance.Complete(characterQuest);
            CompleteQuestSubscription.Instance.InvokeAndUnsubscribe(
                characterQuest.QuestId.ToString(),
                new CompleteQuestSubscriptionEvent());
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

                QuestUI.Instance.QuestCancelButton.onClick.AddListener(() => QuestUI.Instance.Hide());

                QuestUI.Instance.QuestAcceptButton.onClick.AddListener(() =>
                {
                    QuestUI.Instance.Hide();

                    var characterQuest = QuestManager.Instance.CharacterQuests
                        .Where(x => x.QuestId == _questNpc.Quest.Id)
                        .FirstOrDefault();

                    if (characterQuest?.Status == CharacterQuestStatusEnum.Finished)
                    {
                        CompleteQuest(characterQuest);
                    }
                    else
                    {
                        AcceptQuestServerRpc(_questNpc.Quest.Id);
                    }
                });
            }

        }

        private async UniTask CheckProgressSequentiallyAsync(
            string gameObjectName,
            QuestTypeEnum questType,
            int progress,
            ulong clientId,
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            using var inventoryMutation = questType == QuestTypeEnum.Collect
                ? TradeServerState.TrackInventoryMutation(clientId)
                : null;

            await _questMutationSemaphore.WaitAsync(operationCancellationToken);

            try
            {
                await CheckProgressAsync(
                    gameObjectName,
                    questType,
                    progress,
                    clientId,
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
            CancellationToken operationCancellationToken,
            CancellationToken networkLifetimeCancellationToken)
        {
            if (!CanUseNetworkLifetime(networkLifetimeCancellationToken))
            {
                return;
            }

            var quests = QuestManager.Instance.Quests
                .Where(x => x.GameObjectName == gameObjectName)
                .Where(x => x.Type == questType)
                .ToArray();

            if (quests.Length == 0)
            {
                Debug.Log($"Quest not found. GameObjectName: {gameObjectName}, QuestType: {questType}");

                return;
            }

            var playerSessionId = UserManager.Instance.GetPlayerSessionId(clientId);

            foreach (var quest in quests)
            {
                if (!CanUseNetworkLifetime(networkLifetimeCancellationToken))
                {
                    return;
                }

                var result = await QuestManager.Instance.CheckProgressAsync(
                    quest.Id,
                    progress,
                    playerSessionId,
                    operationCancellationToken);

                if (!CanUseNetworkLifetime(networkLifetimeCancellationToken))
                {
                    return;
                }

                if (result.Status != CharacterQuestStatusEnum.None)
                {
                    UpdateQuestClientRpc(result.CharacterQuestId, result.Progress, result.Status, clientId.ToClientRpcParams());
                }
            }
        }

        [ClientRpc]
        private void UpdateQuestClientRpc(int characterQuestId, int progress, CharacterQuestStatusEnum status, ClientRpcParams rpcParams = default)
        {
            Debug.Log($"UpdateQuestLogClientRpc: {characterQuestId}");

            var characterQuest = QuestManager.Instance.CharacterQuests?
                .FirstOrDefault(x => x.Id == characterQuestId);

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

            characterQuest.Progress = progress;
            characterQuest.Status = status;

            QuestUI.Instance.UpdateProgress(characterQuest);

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

            if (_questNpc != null && _input.Move != Vector2.zero && _questNpc.transform.IsFarToTarget(transform.gameObject, _npcMaxDistance) || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _questNpc = null;

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

            _questNpc = hit.transform.GetComponent<QuestNpc>();

            if (_questNpc.Quest == null)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            if (_questNpc.CharacterQuest?.Status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Completed)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            CursorUI.Instance.ShowPointer();

            if (mouse.rightButton.wasPressedThisFrame)
            {
                QuestUI.Instance.Show(_questNpc);
            }
        }

        public override void OnNetworkDespawn()
        {
            CancelNetworkLifetime();

            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            CancelNetworkLifetime();
            _networkLifetimeCancellationTokenSource?.Dispose();
            _networkLifetimeCancellationTokenSource = null;

            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnDestroy();
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
