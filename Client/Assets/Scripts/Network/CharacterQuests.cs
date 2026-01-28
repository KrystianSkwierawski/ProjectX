using System;
using System.Linq;
using System.Text;
using Assets.Scripts.Enums;
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
        private QuestNpc _questNpc;
        private float _npcMaxDistance = 5f;
        private StarterAssetsInputs _input;

        [ServerRpc]
        private void CompleteQuestServerRpc(int characterQuestId, string token, ulong clientId)
        {
            // TODO: validation
            CompleteQuestAsync(characterQuestId, token, clientId).Forget();
        }

        private async UniTask CompleteQuestAsync(int characterQuestId, string clientToken, ulong clientId)
        {
            var result = await UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
            {
                characterId = 1,
                characterQuestId = characterQuestId,
                type = ExperienceTypeEnum.Questing
            }, clientToken);

            if (result.leveledUp)
            {
                UpdateLevelClientRpc(result.level, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                });
            }
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(int level, ClientRpcParams rpcParams = default)
        {
            PlayerUI.Instance.PlayerLevelText.text = $"Level: {level}";
        }

        private async void Start()
        {
            if (IsOwner)
            {
                _input = GetComponent<StarterAssetsInputs>();

                QuestUI.Instance.QuestCancelButton.onClick.AddListener(() => QuestUI.Instance.HideQuestCanvas());

                QuestUI.Instance.QuestAcceptButton.onClick.AddListener(async () =>
                {
                    QuestUI.Instance.HideQuestCanvas();

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

            if (quest == null)
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

            QuestUI.Instance.UpdateQuestProgress(characterQuest);

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

            QuestUI.Instance.AcceptQuest(characterQuest);

            // TODO: server rpc + validation
            AcceptQuestSubscription.Instance.InvokeAndUnsubscribe(_questNpc.Quest.id.ToString(), new AddQuestSubscriptionEvent
            {
                CharacterQuest = characterQuest
            });
        }

        private void CompleteQuest(CharacterQuestDto characterQuest)
        {
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.QuestCompleted, 0.5f);

            characterQuest.status = CharacterQuestStatusEnum.Completed;

            QuestUI.Instance.CompleteQuest(characterQuest);

            CompleteQuestSubscription.Instance.InvokeAndUnsubscribe(characterQuest.questId.ToString(), new CompleteQuestSubscriptionEvent());

            CompleteQuestServerRpc(characterQuest.id, TokenManager.Instance.Token, NetworkManager.Singleton.LocalClientId);
        }

        private void Update()
        {
            if (!IsOwner || QuestManager.Instance.CharacterQuests == null)
            {
                return;
            }

            if (_questNpc != null && _input.Move != Vector2.zero && Vector3.Distance(_questNpc.transform.position, transform.position) >= _npcMaxDistance)
            {
                _questNpc = null;
                QuestUI.Instance.Quest.SetActive(false);

                return;
            }

            if (CheckQuestNpcClicked())
            {
                QuestUI.Instance.ShowQuest(_questNpc);
            }
        }

        private bool CheckQuestNpcClicked()
        {
            var mouse = Mouse.current;

            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

            var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "QuestNpc";

            if (!hover)
            {
                CursorUI.Instance.ShowDefault(this);

                return false;
            }

            CursorUI.Instance.ShowPointer(this);

            if (mouse.rightButton.wasPressedThisFrame)
            {
                var dist = Vector3.Distance(hit.transform.position, transform.position);

                if (dist > _npcMaxDistance)
                {
                    return false;
                }

                _questNpc = hit.transform.GetComponent<QuestNpc>();

                return true;
            }

            return false;
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