using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Mono
{
    public class QuestNpc : MonoBehaviour
    {
        public int[] Ids { get; private set; } = new int[] { 1, 2 };

        public QuestDto Quest { get; set; }

        public CharacterQuestDto CharacterQuest { get; set; }

        private GameObject _exclamationMark;
        private GameObject _quesionMark;

        private async void Start()
        {
            var token = this.GetCancellationTokenOnDestroy();

            await UniTask.WaitUntil(
                () => QuestManager.Instance.Quests != null && QuestManager.Instance.CharacterQuests != null,
                cancellationToken: token
            );

            _exclamationMark = gameObject.transform.Find("ExclamationMark").gameObject;
            _quesionMark = gameObject.transform.Find("QuestionMark").gameObject;

            CharacterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => Ids.Contains(x.questId))
                .Where(x => x.status != CharacterQuestStatusEnum.Completed)
                .FirstOrDefault();

            if (CharacterQuest == null)
            {
                LoadNextQuest();
            }
            else if (CharacterQuest.status == CharacterQuestStatusEnum.Finished)
            {
                LoadFinishedQuest();
            }

            // TODO: load accepted?

            QuestManager.Instance.QuestNpcs.Add(Quest.id, this);
        }

        private void LoadNextQuest()
        {
            var completedQuests = QuestManager.Instance.CharacterQuests
                .Where(x => x.status == CharacterQuestStatusEnum.Completed);

            var filteredIds = Ids.Where(x => !completedQuests.Any(cq => cq.questId == x));

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

            QuestManager.Instance.QuestNpcs.Remove(Quest.previousQuestId);
            QuestManager.Instance.QuestNpcs.Add(Quest.id, this);
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
    }
}