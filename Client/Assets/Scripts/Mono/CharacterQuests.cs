using System;
using System.Linq;
using System.Text;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Mono
{
    public class CharacterQuests : NetworkBehaviour
    {
        private int _questId;

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
                UpdateLevelClientRpc(result.level, clientId);
            }
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(int level, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                UIManager.Instance.PlayerLevelText.text = $"Level: {level}";
            }
        }

        private async void Start()
        {
            if (!IsOwner)
            {
                return;
            }

            UIManager.Instance.QuestAcceptButton.onClick.AddListener(async () =>
            {
                UIManager.Instance.HideQuestCanvas();

                var questNpc = QuestManager.Instance.QuestNpcs[_questId];

                if (questNpc.CharacterQuest?.status == CharacterQuestStatusEnum.Finished)
                {
                    CompleteQuest(questNpc);
                }
                else
                {
                    await AddQuestAsync(questNpc);
                }

                await UpdateQuestLog();
            });

            UIManager.Instance.QuestCancelButton.onClick.AddListener(() => UIManager.Instance.HideQuestCanvas());

            QuestManager.Instance.AddedProgresEvent.AddListener(async (int questId, CharacterQuestStatusEnum status) =>
            {
                await UpdateQuestLog();

                if (status == CharacterQuestStatusEnum.Finished)
                {
                    var npc = QuestManager.Instance.QuestNpcs[questId];

                    npc.HideExclamationMark();
                    npc.ShowQuestionMark();
                }
            });

            await UpdateQuestLog();
        }

        private async UniTask AddQuestAsync(QuestNpc questNpc)
        {
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.QuestAccepted, 0.5f);

            questNpc.HideExclamationMark();

            var characterQuest = await QuestManager.Instance.AcceptCharacterQuestAsync(_questId);

            QuestManager.Instance.CharacterQuests.Add(characterQuest);

            questNpc.CharacterQuest = characterQuest;
        }

        private void CompleteQuest(QuestNpc questNpc)
        {
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.QuestCompleted, 0.5f);

            questNpc.HideQuestionMark();

            var characterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => x.id == questNpc.CharacterQuest.id)
                .Single();

            characterQuest.status = CharacterQuestStatusEnum.Completed;

            questNpc.CheckNextQuest();

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

        // FIXME: refactor and optimization
        private async UniTask UpdateQuestLog()
        {
            await UniTask.WaitUntil(() => QuestManager.Instance.CharacterQuests != null);

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
    }
}