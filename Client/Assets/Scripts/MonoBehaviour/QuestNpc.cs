using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class QuestNpc : MonoBehaviour
{
    [SerializeReference] private int _id = 1;

    public QuestDto Quest { get; set; }

    public CharacterQuestDto CharacterQuest { get; set; }


    private async void Start()
    {
        var token = this.GetCancellationTokenOnDestroy();

        await UniTask.WaitUntil(
            () => QuestManager.Instance.Quests != null && QuestManager.Instance.CharacterQuests != null,
            cancellationToken: token
        );

        Quest = QuestManager.Instance.Quests
            .Where(x => x.id == _id)
            .Single();

        CharacterQuest = QuestManager.Instance.CharacterQuests
            .Where(x => x.questId == _id)
            .FirstOrDefault();

        if (CharacterQuest == null)
        {
            gameObject.transform.Find("ExclamationMark").gameObject.SetActive(true);
        }

        QuestManager.Instance.AcceptedQuestEvent.AddListener(() => gameObject.transform.Find("ExclamationMark").gameObject.SetActive(false));
    }
}
