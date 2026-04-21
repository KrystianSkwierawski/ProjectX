using System;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.Mono
{
    public class QuestNpc : MonoBehaviour
    {
        [SerializeField]
        private QuestEnum[] _questsIds;

        public QuestDto Quest { get; set; }

        public CharacterQuestDto CharacterQuest { get; set; }

        private GameObject _exclamationMark;
        private GameObject _quesionMark;

        private async void Start()
        {
            _exclamationMark = gameObject.transform.Find("ExclamationMark").gameObject;
            _quesionMark = gameObject.transform.Find("QuestionMark").gameObject;

            CharacterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => _questsIds.Contains(x.QuestId))
                .Where(x => x.Status != CharacterQuestStatusEnum.Completed)
                .FirstOrDefault();

            SetStatus();

            foreach (var questId in _questsIds)
            {
                var characterQuest = QuestManager.Instance.CharacterQuests
                    .Where(x => x.QuestId == questId)
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
            Action action = CharacterQuest?.Status switch
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
                .Where(x => x.Status == CharacterQuestStatusEnum.Completed);

            var filteredIds = _questsIds.Where(x => !completedQuests.Any(cq => cq.QuestId == x));

            Quest = QuestManager.Instance.Quests
                .Where(x => filteredIds.Contains(x.Id))
                .FirstOrDefault();

            if (Quest != null)
            {
                ShowExclamationMark();
            }
        }

        private void LoadFinishedQuest()
        {
            Quest = QuestManager.Instance.Quests
                .Where(x => x.Id == CharacterQuest.QuestId)
                .First();

            ShowQuestionMark();
        }

        public void CheckNextQuest()
        {
            Quest = QuestManager.Instance.Quests
                .Where(x => x.PreviousQuestId == Quest.Id)
                .FirstOrDefault();

            if (Quest == null)
            {
                return;
            }

            CharacterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => x.QuestId == Quest.Id)
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
            _exclamationMark.GetComponent<MeshRenderer>().materials = new Material[] { QuestUI.Instance.Material001, QuestUI.Instance.Material002 };
            _exclamationMark.SetActive(true);
        }

        public void HideExclamationMark()
        {
            _exclamationMark.SetActive(false);
        }

        public void MarkAsAccepted()
        {
            _exclamationMark.GetComponent<MeshRenderer>().materials = new Material[] { QuestUI.Instance.Material001 };
        }

        private void LoadAccepted()
        {
            _exclamationMark.SetActive(true);
            MarkAsAccepted();

            Quest = QuestManager.Instance.Quests
                .Where(x => x.Id == CharacterQuest.QuestId)
                .First();
        }
    }
}