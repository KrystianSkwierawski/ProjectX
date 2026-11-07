using System;
using System.Linq;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Quest.UI;
using UnityEngine;

namespace Assets.Scripts.Areas.Quest.Mono
{
    public class QuestNpc : MonoBehaviour
    {
        [SerializeField]
        private QuestEnum[] _questsIds;

        public QuestDto Quest { get; set; }

        public CharacterQuestDto CharacterQuest { get; set; }

        private GameObject _exclamationMark;
        private GameObject _quesionMark;
        private bool _subscribed;

        private void Start()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            return;
#else
            _exclamationMark = gameObject.transform.Find("ExclamationMark").gameObject;
            _quesionMark = gameObject.transform.Find("QuestionMark").gameObject;

            QuestManager.Instance.CharacterQuestsChanged += RefreshQuest;
            _subscribed = true;

            RefreshQuest();
#endif
        }

        private void OnDestroy()
        {
            if (_subscribed)
            {
                QuestManager.Instance.CharacterQuestsChanged -= RefreshQuest;
            }
        }

        private void RefreshQuest()
        {
            HideQuestionMark();
            HideExclamationMark();

            CharacterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => _questsIds.Contains(x.QuestId))
                .Where(x => x.Status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished)
                .FirstOrDefault();

            SetStatus();
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

            var filteredIds = _questsIds
                .Where(x => !completedQuests.Any(cq => cq.QuestId == x));

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
