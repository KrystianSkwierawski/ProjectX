using System;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterQuests : NetworkBehaviour
{
    private int _questId;

    [SerializeField] private AudioClip _questAcceptedSfx;
    [SerializeField] private UIManager _uiManager;

    private async void Start()
    {
        if (!IsOwner)
        {
            return;
        }

        UIManager.Instance.QuestAcceptButton.onClick.AddListener(async () =>
        {
            var characterQuest = await QuestManager.Instance.AcceptCharacterQuestAsync(_questId);

            QuestManager.Instance.CharacterQuests.Add(characterQuest);

            var npc = QuestManager.Instance.QuestNpcs[_questId];
            npc.HideExclamationMark();

            npc.CharacterQuest = characterQuest;

            UIManager.Instance.HideQuestCanvas();
            await UpdateQuestLog();
            GetComponent<AudioSource>().PlayOneShot(_questAcceptedSfx, 0.5f);
        });

        UIManager.Instance.QuestDeclineButton.onClick.AddListener(() => UIManager.Instance.HideQuestCanvas());

        QuestManager.Instance.AddedProgresEvent.AddListener(async (int questId, CharacterQuestStatusEnum status) =>
        {
            await UpdateQuestLog();

            if (status == CharacterQuestStatusEnum.Finished)
            {
                var npc = QuestManager.Instance.QuestNpcs[questId];

                npc.ShowExclamationMark();
            }
        });

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

                if (questNpc.CharacterQuest == null)
                {
                    UIManager.Instance.ShowQuest(questNpc.Quest.title, questNpc.Quest.description);
                }
            }
        }
    }

    private async UniTask UpdateQuestLog()
    {
        await UniTask.WaitUntil(() => QuestManager.Instance.CharacterQuests != null);

        if (QuestManager.Instance.CharacterQuests.Any())
        {
            var sb = new StringBuilder();

            foreach (var characterQuest in QuestManager.Instance.CharacterQuests.Where(x => x.status == CharacterQuestStatusEnum.Accepted))
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
