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
            if (IsOwner)
            {
                UIManager.Instance.QuestCancelButton.onClick.AddListener(() => UIManager.Instance.HideQuestCanvas());

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

                    await UpdateQuestLogAsync();
                });


                await UpdateQuestLogAsync();
            }

            if (IsServer)
            {
                CombatManager.Instance.OnKillEvent.AddListener(async (KillEventModel killEvent) =>
                {
                    await UniTask.WhenAll
                    (
                        CheckProgressAsync(killEvent.GameObject.name, 1, killEvent.ClientId, killEvent.ClientToken),
                        CheckProgressAsync(nameof(CharacterInventoryTypeEnum.Can), 1, killEvent.ClientId, killEvent.ClientToken)
                    );
                });
            }
        }

        private async UniTask CheckProgressAsync(string objectName, int progress, ulong clientId, string clientToken)
        {
            var progres = await QuestManager.Instance.CheckProgressAsync(1, objectName, progress, clientToken);

            if (progres.status != CharacterQuestStatusEnum.None)
            {
                UpdateQuestLogClientRpc(progres.characterQuestId, 1, progres.status, clientId);
            }
        }

        [ClientRpc]
        private void UpdateQuestLogClientRpc(int characterQuestId, int progress, CharacterQuestStatusEnum status, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                Debug.Log($"UpdateQuestLogClientRpc: {clientId}");

                var characterQuest = QuestManager.Instance.CharacterQuests
                    .Where(x => x.id == characterQuestId)
                    .Single();

                characterQuest.progress += progress;
                characterQuest.status = status;

                _ = UpdateQuestLogAsync();

                if (status == CharacterQuestStatusEnum.Finished)
                {
                    var npc = QuestManager.Instance.QuestNpcs[characterQuest.questId];

                    npc.HideExclamationMark();
                    npc.ShowQuestionMark();
                }
            }
        }

        private async UniTask AddQuestAsync(QuestNpc questNpc)
        {
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.QuestAccepted, 0.5f);

            questNpc.MarkAsAccepted();

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

        // TODO: refactor and optimization
        private async UniTask UpdateQuestLogAsync()
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