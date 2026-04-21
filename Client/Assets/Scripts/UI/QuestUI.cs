using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class QuestUI : MonoSingleton<QuestUI>
    {
        #region Prefab

        [SerializeField] private GameObject _textPrefab;

        #endregion

        #region GameObject

        public GameObject QuestCanvas { get; private set; }

        public GameObject Quest { get; private set; }

        public GameObject QuestLog { get; private set; }

        public GameObject QuestLogContent { get; private set; }

        #endregion

        #region TextMesh

        public TextMeshProUGUI QuestAcceptButtonText { get; private set; }

        public TextMeshProUGUI QuestTitleText { get; private set; }

        public TextMeshProUGUI QuestDescriptionText { get; private set; }

        #endregion

        #region Button

        public Button QuestAcceptButton { get; private set; }

        public Button QuestCancelButton { get; private set; }

        #endregion

        #region Material

        public Material Material001 { get; private set; }

        public Material Material002 { get; private set; }

        #endregion

        private ObjectPool<QuestLogPoolObject> _questLogObjectPool;
        private readonly IDictionary<QuestEnum, QuestLogPoolObject> _questLogObjects = new Dictionary<QuestEnum, QuestLogPoolObject>();

        public void Start()
        {
            QuestCanvas = GameObject.Find("QuestCanvas");
            Quest = QuestCanvas.transform.Find("Quest").gameObject;
            QuestLog = QuestCanvas.transform.Find("Log").gameObject;
            QuestLogContent = QuestLog.transform.Find("Viewport/Content").gameObject;
            QuestAcceptButtonText = QuestCanvas.transform.Find("Quest/AcceptButton/Text").GetComponent<TextMeshProUGUI>();
            QuestTitleText = QuestCanvas.transform.Find("Quest/Title").GetComponent<TextMeshProUGUI>();
            QuestDescriptionText = QuestCanvas.transform.Find("Quest/Description/Viewport/Content/Text").GetComponent<TextMeshProUGUI>();
            QuestAcceptButton = QuestCanvas.transform.Find("Quest/AcceptButton").GetComponent<Button>();
            QuestCancelButton = QuestCanvas.transform.Find("Quest/CancelButton").GetComponent<Button>();
            Material001 = Resources.Load<Material>("Materials/Material.001");
            Material002 = Resources.Load<Material>("Materials/Material.002");

            _questLogObjectPool = new ObjectPool<QuestLogPoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_textPrefab, QuestLogContent.transform);

                    return new QuestLogPoolObject
                    {
                        GameObject = obj,
                        Mesh = obj.GetComponent<TextMeshProUGUI>()
                    };
                },
                actionOnGet: (QuestLogPoolObject obj) => obj.GameObject.SetActive(true),
                actionOnRelease: (QuestLogPoolObject obj) =>
                {
                    obj.GameObject.SetActive(false);
                    obj.Mesh.text = string.Empty;
                    obj.Mesh.color = ColorUI.White;
                }
            );

            foreach (var characterQuest in QuestManager.Instance.CharacterQuests
                .Where(x => x.Status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished))
            {
                Accept(characterQuest);
            }
        }

        public void Show(QuestNpc questNpc)
        {
            if (Quest.activeSelf)
            {
                return;
            }

            CraftingUI.Instance.Hide();
            Quest.SetActive(true);
            QuestTitleText.text = questNpc.Quest.Title;

            if (questNpc.CharacterQuest?.Status == CharacterQuestStatusEnum.Finished)
            {
                QuestDescriptionText.text = questNpc.Quest.CompleteDescription;
                QuestAcceptButtonText.text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.Complete);

                return;
            }

            QuestDescriptionText.text = questNpc.Quest.Description;
            QuestAcceptButtonText.text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.Accept);
        }

        public void Hide()
        {
            Quest.SetActive(false);
        }

        public void Accept(CharacterQuestDto characterQuest)
        {
            if (!QuestLog.activeSelf)
            {
                QuestLog.SetActive(true);
            }

            var quest = QuestManager.Instance.Quests
                .Where(y => y.Id == characterQuest.QuestId)
                .Single();

            var questLogObject = _questLogObjectPool.Get();

            questLogObject.Mesh.text = string.Format(quest.StatusText, Math.Min(characterQuest.Progress, quest.Requirement), quest.Requirement);

            if (characterQuest.Status == CharacterQuestStatusEnum.Finished)
            {
                questLogObject.Mesh.color = ColorUI.Green;
            }

            _questLogObjects.Add(quest.Id, questLogObject);
        }

        public void UpdateProgress(CharacterQuestDto characterQuest)
        {
            if (_questLogObjects.TryGetValue(characterQuest.QuestId, out var questLogObject))
            {
                var quest = QuestManager.Instance.Quests
                    .Where(y => y.Id == characterQuest.QuestId)
                    .Single();

                questLogObject.Mesh.text = string.Format(quest.StatusText, Math.Min(characterQuest.Progress, quest.Requirement), quest.Requirement);

                if (characterQuest.Status == CharacterQuestStatusEnum.Finished)
                {
                    questLogObject.Mesh.color = ColorUI.Green;
                }
            }
        }

        public void Complete(CharacterQuestDto characterQuest)
        {
            if (_questLogObjects.TryGetValue(characterQuest.QuestId, out var questLogObject))
            {
                _questLogObjectPool.Release(questLogObject);
                _questLogObjects.Remove(characterQuest.QuestId);

                if (_questLogObjects.Count == 0)
                {
                    QuestLog.SetActive(false);
                }
            }
        }


        private class QuestLogPoolObject
        {
            public GameObject GameObject { get; set; }

            public TextMeshProUGUI Mesh { get; set; }
        }
    }
}
