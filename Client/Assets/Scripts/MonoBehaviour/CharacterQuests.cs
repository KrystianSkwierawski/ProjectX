using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterQuests : NetworkBehaviour
{
    private GameObject _questCanvas;
    private int _questId;

    [SerializeField] private AudioClip _questAcceptedSfx;

    private async void Start()
    {
        if (!IsOwner)
        {
            return;
        }

        _questCanvas = GameObject.Find("QuestCanvas");

        _questCanvas.transform.Find("Quest/DeclineButton").GetComponent<Button>().onClick.AddListener(() => HideQuestCanvas());

        _questCanvas.transform.Find("Quest/AcceptButton").GetComponent<Button>().onClick.AddListener(async () =>
        {
            await QuestManager.Instance.AcceptCharacterQuestAsync(_questId);
            HideQuestCanvas();
            await UpdateQuestLog();
            GetComponent<AudioSource>().PlayOneShot(_questAcceptedSfx, 0.5f);
        });

        QuestManager.Instance.AddedProgresEvent.AddListener(async () => await UpdateQuestLog());

        await UpdateQuestLog();
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
                _questId = questNpc.Quest.id;

                if(questNpc.CharacterQuest == null)
                {
                    _questCanvas.transform.Find("Quest").gameObject.SetActive(true);
                    _questCanvas.transform.Find("Quest/Title").GetComponent<TextMeshProUGUI>().text = questNpc.Quest.title;
                    _questCanvas.transform.Find("Quest/Description").GetComponent<TextMeshProUGUI>().text = questNpc.Quest.description;
                }
            }
        }
    }

    private async UniTask UpdateQuestLog()
    {
        await UniTask.WaitUntil(() => QuestManager.Instance.CharacterQuests != null);

        Debug.Log(QuestManager.Instance.CharacterQuests.Count);

        if (QuestManager.Instance.CharacterQuests.Any())
        {
            _questCanvas.transform.Find("Log").gameObject.SetActive(true);

            var sb = new StringBuilder();

            foreach (var characterQuest in QuestManager.Instance.CharacterQuests.Where(x => x.status == CharacterQuestStatusEnum.Accepted))
            {
                var quest = QuestManager.Instance.Quests
                    .Where(x => x.id == characterQuest.questId)
                    .Single();

                var log = string.Format(quest.statusText, characterQuest.progress, quest.requirement);

                sb.AppendLine(log);
            }

            _questCanvas.transform.Find("Log/Text").GetComponent<TextMeshProUGUI>().text = sb.ToString();
        }
    }

    private void HideQuestCanvas()
    {
        _questCanvas.transform.Find("Quest").gameObject.SetActive(false);
    }
}
