using System;
using System.Linq;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Quest.Subscriptions;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
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

        private QuestNpc _questNpc;
        private StarterAssetsInputs _input;

        [ServerRpc]
        private void CompleteQuestServerRpc(QuestEnum questId, int characterQuestId)
        {
            // TODO: validation
            CompleteQuestAsync(questId, characterQuestId, UserManager.Instance.GetPlayerSessionId(OwnerClientId)).Forget();
        }

        [ServerRpc]
        private void AcceptQuestServerRpc(QuestEnum questId)
        {
            AcceptQuestAsync(questId, UserManager.Instance.GetPlayerSessionId(OwnerClientId)).Forget();
        }

        private async UniTask AcceptQuestAsync(QuestEnum questId, string playerSessionId)
        {
            var characterQuest = await QuestManager.Instance.AcceptCharacterQuestAsync(questId, playerSessionId);

            AcceptQuestClientRpc(
                characterQuest.Id,
                characterQuest.QuestId,
                characterQuest.Status,
                characterQuest.Progress,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { OwnerClientId }
                    }
                });
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

        private async UniTask CompleteQuestAsync(QuestEnum questId, int characterQuestId, string playerSessionId)
        {
            var quest = QuestManager.Instance.Quests
                .Where(x => x.Id == questId)
                .Single();

            var result = await QuestManager.Instance.CompleteAsync(characterQuestId, playerSessionId);

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

            AddExperienceSubscription.Instance.Invoke(OwnerClientId.ToString(), new AddExperienceSubscriptionEvent
            {
                Amount = result.Reward,
                Type = ExperienceTypeEnum.Main,
                PlayerSessionId = playerSessionId,
            });
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

            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) => await CheckProgressAsync(e.GameObjectName, e.Progress, OwnerClientId));
            }
        }

        private async UniTask CheckProgressAsync(string gameObjectName, int progress, ulong clientId)
        {
            // TODO: multiple quests with same gameObjectName
            var quest = QuestManager.Instance.Quests
                .Where(x => x.GameObjectName == gameObjectName)
                .FirstOrDefault();

            if (quest == null || quest.Id == QuestEnum.None)
            {
                Debug.Log($"Quest not found. GameObjectName: {gameObjectName}");

                return;
            }

            var result = await QuestManager.Instance.CheckProgressAsync(
                quest.Id,
                progress,
                UserManager.Instance.GetPlayerSessionId(clientId));

            if (result.Status != CharacterQuestStatusEnum.None)
            {
                UpdateQuestClientRpc(result.CharacterQuestId, result.Progress, result.Status, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                });
            }
        }

        [ClientRpc]
        private void UpdateQuestClientRpc(int characterQuestId, int progress, CharacterQuestStatusEnum status, ClientRpcParams rpcParams = default)
        {
            Debug.Log($"UpdateQuestLogClientRpc: {characterQuestId}");

            var characterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => x.Id == characterQuestId)
                .Single();

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
            var quest = QuestManager.Instance.Quests
                .Where(x => x.Id == characterQuest.QuestId)
                .Single();

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.QuestCompleted, 0.5f);

            characterQuest.Status = CharacterQuestStatusEnum.Completed;

            QuestUI.Instance.Complete(characterQuest);

            CompleteQuestSubscription.Instance.InvokeAndUnsubscribe(characterQuest.QuestId.ToString(), new CompleteQuestSubscriptionEvent());

            CompleteQuestServerRpc(quest.Id, characterQuest.Id);
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
            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnDestroy();
        }
    }
}
