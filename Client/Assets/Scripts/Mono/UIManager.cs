using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Assets.Scripts.Mono
{
    public class UIManager : MonoSingleton<UIManager>
    {
        public readonly IDictionary<CharacterInventoryTypeEnum, Texture> Textures = new Dictionary<CharacterInventoryTypeEnum, Texture>();

        public Material Material001 { get; private set; }
        public Material Material002 { get; private set; }

        public GameObject ProgressBarCanvas { get; private set; }
        public GameObject TargetCanvas { get; private set; }
        public GameObject QuestCanvas { get; private set; }
        public GameObject PlayerCanvas { get; private set; }
        public GameObject InventoryCanvas { get; private set; }
        public GameObject Inventory { get; private set; }
        public GameObject Quest { get; private set; }
        public GameObject QuestLog { get; private set; }
        public GameObject QuestLogContent { get; private set; }
        public GameObject Target { get; private set; }
        public GameObject Loot { get; private set; }
        public GameObject LootContent { get; private set; }

        [SerializeField] private GameObject _inventorySlotPrefab;
        [SerializeField] private GameObject _textPrefab;

        public TextMeshProUGUI QuestAcceptButtonText { get; private set; }
        public TextMeshProUGUI TargetNameText { get; private set; }
        public TextMeshProUGUI TargetHealthPointsText { get; private set; }
        public TextMeshProUGUI QuestTitleText { get; private set; }
        public TextMeshProUGUI QuestDescriptionText { get; private set; }
        public TextMeshProUGUI PlayerLevelText { get; private set; }
        public TextMeshProUGUI PlayerNameText { get; private set; }
        public TextMeshProUGUI PlayerHealthPointsText { get; private set; }

        public Button QuestAcceptButton { get; private set; }
        public Button QuestCancelButton { get; private set; }

        public Image CastProgressBar { get; private set; }

        private InventorySlot[] _inventorySlots;

        private ObjectPool<QuestLogObject> _questLogPool;
        private readonly IDictionary<QuestEnum, QuestLogObject> _questLogObjects = new Dictionary<QuestEnum, QuestLogObject>();

        private static readonly Color _greenColor = new Color(0.0039215686f, 0.7333333333f, 0.0078431373f, 1f);

        public void Init()
        {
            InitGameObjects();
            InitMaterials();
            InitTextures();
            InitQuestLog();
        }

        private void InitGameObjects()
        {
            ProgressBarCanvas = GameObject.Find("ProgressBarCanvas");
            TargetCanvas = GameObject.Find("TargetCanvas");
            QuestCanvas = GameObject.Find("QuestCanvas");
            PlayerCanvas = GameObject.Find("PlayerCanvas");
            InventoryCanvas = GameObject.Find("InventoryCanvas");
            Inventory = InventoryCanvas.transform.Find("Inventory").gameObject;
            Quest = QuestCanvas.transform.Find("Quest").gameObject;
            QuestLog = QuestCanvas.transform.Find("Log").gameObject;
            QuestLogContent = QuestLog.transform.Find("Viewport/Content").gameObject;
            Target = TargetCanvas.transform.Find("Target").gameObject;
            Loot = InventoryCanvas.transform.Find("Loot").gameObject;
            LootContent = Loot.transform.Find("Viewport/Content").gameObject;

            QuestAcceptButtonText = QuestCanvas.transform.Find("Quest/AcceptButton/Text").GetComponent<TextMeshProUGUI>();
            TargetNameText = TargetCanvas.transform.Find("Target/Name").GetComponent<TextMeshProUGUI>();
            TargetHealthPointsText = TargetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>();
            QuestTitleText = QuestCanvas.transform.Find("Quest/Title").GetComponent<TextMeshProUGUI>();
            QuestDescriptionText = QuestCanvas.transform.Find("Quest/Description").GetComponent<TextMeshProUGUI>();
            PlayerLevelText = PlayerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>();
            PlayerNameText = PlayerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>();
            PlayerHealthPointsText = PlayerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>();

            QuestAcceptButton = QuestCanvas.transform.Find("Quest/AcceptButton").GetComponent<Button>();
            QuestCancelButton = QuestCanvas.transform.Find("Quest/CancelButton").GetComponent<Button>();

            CastProgressBar = GameObject.Find("ProgressBar").GetComponent<Image>();
        }

        private void InitMaterials()
        {
            Material001 = Resources.Load<Material>("Materials/Material.001");
            Material002 = Resources.Load<Material>("Materials/Material.002");
        }

        public void UpdateInventory(CharacterInventoryDto value)
        {
            _inventorySlots ??= InstantiateInventorySlots(value.count).ToArray();

            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                var slot = _inventorySlots[i];
                var item = value.inventory.items.ElementAtOrDefault(i);

                if (item == null)
                {
                    slot.Mesh.gameObject.SetActive(false);
                    continue;
                }

                if (Textures.TryGetValue(item.type, out var texture))
                {
                    slot.Mesh.gameObject.SetActive(true);
                    slot.Mesh.text = item.count.ToString();
                    slot.Image.color = Color.white;
                    slot.Image.texture = texture;
                }
            }
        }

        private void InitTextures()
        {
            foreach (var type in Enum.GetValues(typeof(CharacterInventoryTypeEnum)).Cast<CharacterInventoryTypeEnum>())
            {
                var texture = Resources.Load<Texture>($"Textures/{type}");

                if (texture != null)
                {
                    Debug.Log($"UIManager -> Add texture. Type: {type}");

                    Textures.Add(type, texture);
                }
            }
        }

        public void SetTarget(string name, string health)
        {
            Target.SetActive(true);
            TargetNameText.text = name;
            TargetHealthPointsText.text = health;
        }

        public void ShowCastBar(float progress)
        {
            if (CastProgressBar != null)
            {
                ProgressBarCanvas.SetActive(true);
                CastProgressBar.fillAmount = Mathf.Clamp01(progress);
            }
        }

        public void HideCastBar()
        {
            if (CastProgressBar != null)
            {
                ProgressBarCanvas.SetActive(false);
            }
        }

        public void FailCastBar()
        {
            if (CastProgressBar != null)
            {
                CastProgressBar.color = Color.red;
                CastProgressBar.fillAmount = 1f;
                ProgressBarCanvas.SetActive(true);
            }
        }

        public void ShowQuest(QuestNpc questNpc)
        {
            if (questNpc.CharacterQuest == null || questNpc.CharacterQuest.status == CharacterQuestStatusEnum.Finished)
            {
                Quest.SetActive(true);
                QuestTitleText.text = questNpc.Quest.title;
                QuestDescriptionText.text = questNpc.CharacterQuest == null ? questNpc.Quest.description : questNpc.Quest.completeDescription;
                QuestAcceptButtonText.text = questNpc.CharacterQuest == null ? "Accept" : "Complete";
            }
        }

        public void HideQuestCanvas()
        {
            Quest.SetActive(false);
        }

        private void InitQuestLog()
        {
            _questLogPool = new ObjectPool<QuestLogObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_textPrefab, QuestLogContent.transform);

                    return new QuestLogObject
                    {
                        GameObject = obj,
                        Mesh = obj.GetComponent<TextMeshProUGUI>()
                    };
                },
                actionOnGet: (QuestLogObject questLogObject) => questLogObject.GameObject.SetActive(true),
                actionOnRelease: (QuestLogObject questLogObject) =>
                {
                    questLogObject.GameObject.SetActive(false);
                    questLogObject.Mesh.text = string.Empty;
                    questLogObject.Mesh.color = Color.white;
                }
            );

            foreach (var characterQuest in QuestManager.Instance.CharacterQuests
                .Where(x => x.status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished))
            {
                AcceptQuest(characterQuest);
            }
        }

        public void AcceptQuest(CharacterQuestDto characterQuest)
        {
            if (!QuestLog.activeSelf)
            {
                QuestLog.SetActive(true);
            }

            var quest = QuestManager.Instance.Quests
                .Where(y => y.id == characterQuest.questId)
                .Single();

            var questLogObject = _questLogPool.Get();

            questLogObject.Mesh.text = string.Format(quest.statusText, Math.Min(characterQuest.progress, quest.requirement), quest.requirement);

            if (characterQuest.status == CharacterQuestStatusEnum.Finished)
            {
                questLogObject.Mesh.color = _greenColor;
            }

            _questLogObjects.Add(quest.id, questLogObject);
        }

        public void UpdateQuestProgress(CharacterQuestDto characterQuest)
        {
            if (_questLogObjects.TryGetValue(characterQuest.questId, out var questLogObject))
            {
                var quest = QuestManager.Instance.Quests
                    .Where(y => y.id == characterQuest.questId)
                    .Single();

                questLogObject.Mesh.text = string.Format(quest.statusText, Math.Min(characterQuest.progress, quest.requirement), quest.requirement);

                if (characterQuest.status == CharacterQuestStatusEnum.Finished)
                {
                    questLogObject.Mesh.color = _greenColor;
                }
            }
        }

        public void CompleteQuest(CharacterQuestDto characterQuest)
        {
            if (_questLogObjects.TryGetValue(characterQuest.questId, out var questLogObject))
            {
                _questLogPool.Release(questLogObject);
                _questLogObjects.Remove(characterQuest.questId);
            }
        }

        public void SetPlayer(string name, string health, string level)
        {
            PlayerNameText.text = name;
            PlayerHealthPointsText.text = health;
            PlayerLevelText.text = $"Level: {level}";
        }

        private IEnumerable<InventorySlot> InstantiateInventorySlots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(_inventorySlotPrefab);
                slot.transform.SetParent(Inventory.transform);

                yield return new InventorySlot
                {
                    GameObject = slot,
                    Image = slot.transform.Find("Background").GetComponent<RawImage>(),
                    Mesh = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
                };
            }
        }

        public void ShowLoot(InventoryItem[] items)
        {
            Loot.SetActive(true);

            foreach (var item in items)
            {
                var slot = Instantiate(_inventorySlotPrefab);
                slot.transform.SetParent(LootContent.transform);

                var image = slot.transform.Find("Background").GetComponent<RawImage>();
                var text = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>();

                if (Textures.TryGetValue(item.type, out var texture))
                {
                    text.gameObject.SetActive(true);
                    text.text = item.count.ToString();
                    image.color = Color.white;
                    image.texture = texture;
                }

                slot.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // TODO: invoke add inventory
                    // TODO: pool
                    // TODO: hide
                    Destroy(slot);
                });
            }
        }

        private class InventorySlot
        {
            public GameObject GameObject { get; set; }

            public RawImage Image { get; set; }

            public TextMeshProUGUI Mesh { get; set; }
        }

        private class QuestLogObject
        {
            public GameObject GameObject { get; set; }

            public TextMeshProUGUI Mesh { get; set; }
        }
    }
}