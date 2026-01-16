using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;
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
        public GameObject Target { get; private set; }
        [SerializeField] private GameObject _inventorySlotPrefab;

        public TextMeshProUGUI QuestAcceptButtonText { get; private set; }
        public TextMeshProUGUI TargetNameText { get; private set; }
        public TextMeshProUGUI TargetHealthPointsText { get; private set; }
        public TextMeshProUGUI QuestTitleText { get; private set; }
        public TextMeshProUGUI QuestDescriptionText { get; private set; }
        public TextMeshProUGUI QuestLogText { get; private set; }
        public TextMeshProUGUI PlayerLevelText { get; private set; }
        public TextMeshProUGUI PlayerNameText { get; private set; }
        public TextMeshProUGUI PlayerHealthPointsText { get; private set; }

        public Button QuestAcceptButton { get; private set; }
        public Button QuestCancelButton { get; private set; }

        public Image CastProgressBar { get; private set; }


        private InventorySlot[] _inventorySlots;

        public void Init()
        {
            InitGameObjects();
            InitMaterials();
            InitTextures();
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
            Target = TargetCanvas.transform.Find("Target").gameObject;

            QuestAcceptButtonText = QuestCanvas.transform.Find("Quest/AcceptButton/Text").GetComponent<TextMeshProUGUI>();
            TargetNameText = TargetCanvas.transform.Find("Target/Name").GetComponent<TextMeshProUGUI>();
            TargetHealthPointsText = TargetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>();
            QuestTitleText = QuestCanvas.transform.Find("Quest/Title").GetComponent<TextMeshProUGUI>();
            QuestDescriptionText = QuestCanvas.transform.Find("Quest/Description").GetComponent<TextMeshProUGUI>();
            QuestLogText = QuestCanvas.transform.Find("Log/Text").GetComponent<TextMeshProUGUI>();
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
                    slot.Text.gameObject.SetActive(false);
                    continue;
                }

                if (Textures.TryGetValue(item.type, out var texture))
                {
                    slot.Text.gameObject.SetActive(true);
                    slot.Text.text = item.count.ToString();
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

        public void SetQuestLog(string text)
        {
            QuestLog.SetActive(true);
            QuestLogText.text = text;
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
                    Text = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
                };
            }
        }

        private class InventorySlot
        {
            public GameObject GameObject { get; set; }

            public RawImage Image { get; set; }

            public TextMeshProUGUI Text { get; set; }
        }
    }
}