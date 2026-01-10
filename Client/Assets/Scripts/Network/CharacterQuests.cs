using System;
using System.Linq;
using System.Text;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Mono
{
    public class CharacterQuests : NetworkBehaviour
    {
        private QuestEnum _questId;

        [ServerRpc]
        private void CompleteQuestServerRpc(int characterQuestId, string token, ulong clientId)
        {
            // TODO: validate transform.location
            _ = CompleteQuestAsync(characterQuestId, token, clientId);
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
            UIManager.Instance.PlayerLevelText.text = $"Level: {level}";
        }

        private async void Start()
        {
            if (IsOwner)
            {
                UIManager.Instance.QuestCancelButton.onClick.AddListener(() => UIManager.Instance.HideQuestCanvas());

                UIManager.Instance.QuestAcceptButton.onClick.AddListener(async () =>
                {
                    UIManager.Instance.HideQuestCanvas();

                    var characterQuest = QuestManager.Instance.CharacterQuests
                        .Where(x => x.questId == _questId)
                        .FirstOrDefault();

                    if (characterQuest?.status == CharacterQuestStatusEnum.Finished)
                    {
                        CompleteQuest(characterQuest);
                    }
                    else
                    {
                        await AcceptQuestAsync();
                    }

                    await UpdateQuestLogAsync();
                });

                await UpdateQuestLogAsync();
            }

            if (IsServer)
            {
                CheckCharacterQuestSubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) => await CheckProgressAsync(e.GameObjectName, e.Progress, OwnerClientId, e.ClientToken));
            }
        }

        private async UniTask CheckProgressAsync(string gameObjectName, int progress, ulong clientId, string clientToken)
        {
            var result = await QuestManager.Instance.CheckProgressAsync(1, gameObjectName, progress, clientToken);

            if (result.status != CharacterQuestStatusEnum.None)
            {
                UpdateQuestLogClientRpc(result.characterQuestId, progress, result.status, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                });
            }
        }

        [ClientRpc]
        private void UpdateQuestLogClientRpc(int characterQuestId, int progress, CharacterQuestStatusEnum status, ClientRpcParams rpcParams = default)
        {
            Debug.Log($"UpdateQuestLogClientRpc: {characterQuestId}");

            var characterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => x.id == characterQuestId)
                .Single();

            characterQuest.progress += progress;
            characterQuest.status = status;

            _ = UpdateQuestLogAsync();

            if (status == CharacterQuestStatusEnum.Finished)
            {
                FinishCharacterQuestSubscription.Instance.Invoke(characterQuest.questId.ToString(), new FinishCharacterQuestSubscriptionEvent());
            }
        }

        private async UniTask AcceptQuestAsync()
        {
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.QuestAccepted, 0.5f);

            var characterQuest = await QuestManager.Instance.AcceptCharacterQuestAsync(_questId);

            QuestManager.Instance.CharacterQuests.Add(characterQuest);

            AcceptQuestSubscription.Instance.InvokeAndUnsubscribe(_questId.ToString(), new AddQuestSubscriptionEvent
            {
                CharacterQuest = characterQuest
            });
        }

        private void CompleteQuest(CharacterQuestDto characterQuest)
        {
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.QuestCompleted, 0.5f);

            characterQuest.status = CharacterQuestStatusEnum.Completed;

            CompleteQuestSubscription.Instance.InvokeAndUnsubscribe(characterQuest.questId.ToString(), new CompleteQuestSubscriptionEvent());

            CompleteQuestServerRpc(characterQuest.id, TokenManager.Instance.Token, NetworkManager.Singleton.LocalClientId);
        }

        private void Update()
        {
            if (!IsOwner || QuestManager.Instance.CharacterQuests == null)
            {
                return;
            }

            var mouse = Mouse.current;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "QuestNpc")
                {
                    var questNpc = hit.transform.GetComponent<QuestNpc>();

                    if (questNpc?.Quest != null)
                    {
                        _questId = questNpc.Quest.id;

                        UIManager.Instance.ShowQuest(questNpc);
                    }
                }
            }
        }

        // TODO: refactor and optimization
        private async UniTask UpdateQuestLogAsync()
        {
            await UniTask.WaitUntil(
                () => QuestManager.Instance.CharacterQuests != null, 
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

            if (QuestManager.Instance.CharacterQuests.Any())
            {
                var sb = new StringBuilder();

                foreach (var characterQuest in QuestManager.Instance.CharacterQuests.Where(x => x.status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished))
                {
                    var quest = QuestManager.Instance.Quests
                        .Where(x => x.id == characterQuest.questId)
                        .Single();

                    var log = string.Format(quest.statusText, Math.Min(characterQuest.progress, quest.requirement), quest.requirement);

                    sb.AppendLine(log);
                }

                UIManager.Instance.SetQuestLog(sb.ToString());
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