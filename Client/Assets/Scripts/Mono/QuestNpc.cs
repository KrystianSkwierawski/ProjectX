using System;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Mono
{
    public class QuestNpc : MonoBehaviour
    {
        public QuestEnum[] QuestsIds { get; private set; } = new QuestEnum[] { QuestEnum.Kill2Beans, QuestEnum.Collect2Cans };

        public QuestDto Quest { get; set; }

        public CharacterQuestDto CharacterQuest { get; set; }

        private GameObject _exclamationMark;
        private GameObject _quesionMark;

        private async void Start()
        {
            await UniTask.WaitUntil(
                () => QuestManager.Instance.Quests != null && QuestManager.Instance.CharacterQuests != null,
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

            _exclamationMark = gameObject.transform.Find("ExclamationMark").gameObject;
            _quesionMark = gameObject.transform.Find("QuestionMark").gameObject;

            CharacterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => QuestsIds.Contains(x.questId))
                .Where(x => x.status != CharacterQuestStatusEnum.Completed)
                .FirstOrDefault();

            SetStatus();

            foreach (var questId in QuestsIds)
            {
                var characterQuest = QuestManager.Instance.CharacterQuests
                    .Where(x => x.questId == questId)
                    .SingleOrDefault();

                var key = questId.ToString();

                if (characterQuest == null)
                {
                    AcceptQuestSubscription.Instance.Subscribe(key, (e) =>
                    {
                        CharacterQuest = e.CharacterQuest;
                        MarkAsAccepted();
                    });
                }

                FinishCharacterQuestSubscription.Instance.Subscribe(key, (e) =>
                {
                    HideExclamationMark();
                    ShowQuestionMark();
                });

                CompleteQuestSubscription.Instance.Subscribe(key, (e) =>
                {
                    HideQuestionMark();
                    CheckNextQuest();
                });
            }
        }

        private void SetStatus()
        {
            Action action = CharacterQuest?.status switch
            {
                CharacterQuestStatusEnum.Accepted => LoadAccepted,
                CharacterQuestStatusEnum.Finished => LoadFinishedQuest,
                _ => LoadNextQuest,
            };

            action();
        }

        private void LoadNextQuest()
        {
            var completedQuests = QuestManager.Instance.CharacterQuests
                .Where(x => x.status == CharacterQuestStatusEnum.Completed);

            var filteredIds = QuestsIds.Where(x => !completedQuests.Any(cq => cq.questId == x));

            Quest = QuestManager.Instance.Quests
                .Where(x => filteredIds.Contains(x.id))
                .First();

            ShowExclamationMark();
        }

        private void LoadFinishedQuest()
        {
            Quest = QuestManager.Instance.Quests
                .Where(x => x.id == CharacterQuest.questId)
                .First();

            ShowQuestionMark();
        }

        public void CheckNextQuest()
        {
            Quest = QuestManager.Instance.Quests
                .Where(x => x.previousQuestId == Quest.id)
                .FirstOrDefault();

            if (Quest == null)
            {
                return;
            }

            CharacterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => x.questId == Quest.id)
                .FirstOrDefault();

            if (CharacterQuest == null)
            {
                ShowExclamationMark();
            }
        }

        public void ShowQuestionMark()
        {
            _quesionMark.SetActive(true);
        }

        public void HideQuestionMark()
        {
            _quesionMark.SetActive(false);
        }

        public void ShowExclamationMark()
        {
            _exclamationMark.GetComponent<MeshRenderer>().materials = new Material[] { UIManager.Instance.Material001, UIManager.Instance.Material002 };
            _exclamationMark.SetActive(true);
        }

        public void HideExclamationMark()
        {
            _exclamationMark.SetActive(false);
        }

        public void MarkAsAccepted()
        {
            _exclamationMark.GetComponent<MeshRenderer>().materials = new Material[] { UIManager.Instance.Material001 };
        }

        private void LoadAccepted()
        {
            _exclamationMark.SetActive(true);
            MarkAsAccepted();

            Quest = QuestManager.Instance.Quests
                .Where(x => x.id == CharacterQuest.questId)
                .First();
        }
    }
}