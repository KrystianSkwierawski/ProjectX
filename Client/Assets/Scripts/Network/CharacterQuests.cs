using System;
using System.Linq;
using System.Text;
using Assets.Scripts.Enums;
using Assets.Scripts.Extensions;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Mono
{
    public class CharacterQuests : NetworkBehaviour
    {
        private const float _npcMaxDistance = 5f;

        private QuestNpc _questNpc;
        private StarterAssetsInputs _input;

        [ServerRpc]
        private void CompleteQuestServerRpc(QuestEnum questId, int characterQuestId, string token)
        {
            // TODO: validation
            CompleteQuestAsync(questId, characterQuestId, token).Forget();
        }

        private async UniTask CompleteQuestAsync(QuestEnum questId, int characterQuestId, string clientToken)
        {
            // TODO: validate and get type from complete?
            var quest = QuestManager.Instance.Quests
                .Where(x => x.id == questId)
                .Single();

            if (quest.type == QuestTypeEnum.Collect)
            {
                RemoveInventoryItemSubscription.Instance.Invoke(OwnerClientId.ToString(), new RemoveInventoryItemSubscriptionEvent
                {
                    Item = new InventoryItemDto
                    {
                        type = Enum.Parse<InventoryItemEnum>(quest.gameObjectName),
                        count = quest.requirement,
                    },
                    ClientToken = UserManager.Instance.Token,
                });
            }

            var result = await QuestManager.Instance.CompleteAsync(characterQuestId, clientToken);

            AddExperienceSubscription.Instance.Invoke(OwnerClientId.ToString(), new AddExperienceSubscriptionEvent
            {
                Amount = result.reward,
                Type = ExperienceTypeEnum.Main,
                ClientToken = clientToken,
            });
        }

        private async void Start()
        {
            if (IsOwner)
            {
                _input = GetComponent<StarterAssetsInputs>();

                QuestUI.Instance.QuestCancelButton.onClick.AddListener(() => QuestUI.Instance.Hide());

                QuestUI.Instance.QuestAcceptButton.onClick.AddListener(async () =>
                {
                    QuestUI.Instance.Hide();

                    var characterQuest = QuestManager.Instance.CharacterQuests
                        .Where(x => x.questId == _questNpc.Quest.id)
                        .FirstOrDefault();

                    if (characterQuest?.status == CharacterQuestStatusEnum.Finished)
                    {
                        CompleteQuest(characterQuest);
                    }
                    else
                    {
                        await AcceptQuestAsync();
                    }
                });
            }

            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) => await CheckProgressAsync(e.GameObjectName, e.Progress, OwnerClientId, e.ClientToken));
            }
        }

        private async UniTask CheckProgressAsync(string gameObjectName, int progress, ulong clientId, string clientToken)
        {
            // TODO: multiple quests with same gameObjectName
            var quest = QuestManager.Instance.Quests
                .Where(x => x.gameObjectName == gameObjectName)
                .FirstOrDefault();

            if (quest == null || quest.id == QuestEnum.None)
            {
                Debug.Log($"Quest not found. GameObjectName: {gameObjectName}");

                return;
            }

            var result = await QuestManager.Instance.CheckProgressAsync(quest.id, progress, 1, clientToken);

            if (result.status != CharacterQuestStatusEnum.None)
            {
                UpdateQuestClientRpc(result.characterQuestId, progress, result.status, new ClientRpcParams
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
                .Where(x => x.id == characterQuestId)
                .Single();

            characterQuest.progress += progress;
            characterQuest.status = status;

            QuestUI.Instance.UpdateProgress(characterQuest);

            if (status == CharacterQuestStatusEnum.Finished)
            {
                FinishCharacterQuestSubscription.Instance.Invoke(characterQuest.questId.ToString(), new FinishCharacterQuestSubscriptionEvent());
            }
        }

        private async UniTask AcceptQuestAsync()
        {
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.QuestAccepted, 0.5f);

            var characterQuest = await QuestManager.Instance.AcceptCharacterQuestAsync(_questNpc.Quest.id);

            QuestManager.Instance.CharacterQuests.Add(characterQuest);

            QuestUI.Instance.Accept(characterQuest);

            // TODO: server rpc + validation
            AcceptQuestSubscription.Instance.InvokeAndUnsubscribe(_questNpc.Quest.id.ToString(), new AddQuestSubscriptionEvent
            {
                CharacterQuest = characterQuest
            });
        }

        private void CompleteQuest(CharacterQuestDto characterQuest)
        {
            var quest = QuestManager.Instance.Quests
                .Where(x => x.id == characterQuest.questId)
                .Single();

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.QuestCompleted, 0.5f);

            characterQuest.status = CharacterQuestStatusEnum.Completed;

            QuestUI.Instance.Complete(characterQuest);

            CompleteQuestSubscription.Instance.InvokeAndUnsubscribe(characterQuest.questId.ToString(), new CompleteQuestSubscriptionEvent());

            CompleteQuestServerRpc(quest.id, characterQuest.id, UserManager.Instance.Token);
        }

        private void Update()
        {
            if (!IsOwner || QuestManager.Instance.CharacterQuests == null)
            {
                return;
            }

            if (_questNpc != null && _input.Move != Vector2.zero && _questNpc.transform.IsFarToTarget(transform.gameObject, _npcMaxDistance))
            {
                _questNpc = null;
                QuestUI.Instance.Quest.SetActive(false);

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

            if (_questNpc.CharacterQuest?.status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Completed)
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