using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class QuestNpc : MonoBehaviour
{
    public int Id { get; private set; } = 1;

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
            .Where(x => x.id == Id)
            .Single();

        CharacterQuest = QuestManager.Instance.CharacterQuests
            .Where(x => x.questId == Id)
            .FirstOrDefault();

        if (CharacterQuest == null)
        {
            gameObject.transform.Find("ExclamationMark").gameObject.SetActive(true);
        }

        QuestManager.Instance.QuestNpcs.Add(Id, this);
    }

    public void ShowExclamationMark()
    {
        gameObject.transform.Find("ExclamationMark").gameObject.SetActive(true);
    }

    public void HideExclamationMark()
    {
        gameObject.transform.Find("ExclamationMark").gameObject.SetActive(false);
    }
}
