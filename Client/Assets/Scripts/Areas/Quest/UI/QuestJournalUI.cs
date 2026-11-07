using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Mono;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Quest.Mono;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Trade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Quest.UI
{
    public class QuestJournalUI : MonoSingleton<QuestJournalUI>
    {
        [SerializeField] private GameObject _textButtonPrefab;
        [SerializeField] private GameObject _itemRowPrefab;

        private GameObject _panel;
        private Transform _list;
        private ScrollRect _details;
        private TMP_Text _title;
        private TMP_Text _status;
        private TMP_Text _empty;
        private Button _abandon;
        private TMP_Text _abandonText;
        private int? _selectedId;
        private bool _confirming;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly List<GameObject> _buttons = new List<GameObject>();
        private Vector2 _canvasSize;
        private Vector2 _questOrigin;
        private Vector2 _questSize;

        private void Start()
        {
            _panel = transform.Find("Journal").gameObject;
            _questOrigin = ((RectTransform)transform.Find("Quest")).anchoredPosition;
            _questSize = ((RectTransform)transform.Find("Quest")).sizeDelta;
            _list = _panel.transform.Find("List/Viewport/Content");
            _details = _panel.transform.Find("Description").GetComponent<ScrollRect>();
            var listLayout = _list.GetComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(12, 12, 8, 8);
            listLayout.spacing = 6;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            _title = _panel.transform.Find("Title").GetComponent<TMP_Text>();
            _title.gameObject.SetActive(false);
            _status = _panel.transform.Find("Status").GetComponent<TMP_Text>();
            _empty = _panel.transform.Find("Empty").GetComponent<TMP_Text>();
            _abandon = _panel.transform.Find("AcceptButton").GetComponent<Button>();
            _abandonText = _abandon.GetComponentInChildren<TMP_Text>();
            _panel.transform.Find("Close").GetComponent<Button>().onClick.AddListener(Hide);
            _abandon.onClick.AddListener(Abandon);
            QuestManager.Instance.CharacterQuestsChanged += Refresh;
            _panel.SetActive(false);
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 12;
            _title.fontSizeMax = 22;
            _title.alignment = TextAlignmentOptions.Left;
            _title.margin = Vector4.zero;
            _title.textWrappingMode = TextWrappingModes.NoWrap;
            _title.overflowMode = TextOverflowModes.Ellipsis;
            _status.enableAutoSizing = true;
            _status.fontSizeMin = 11;
            _status.fontSizeMax = 18;
            _status.alignment = TextAlignmentOptions.Left;
            _status.margin = Vector4.zero;
            _status.textWrappingMode = TextWrappingModes.NoWrap;
            _status.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void Update()
        {
            var size = ((RectTransform)transform).rect.size;

            if (_panel != null && size != _canvasSize)
            {
                _canvasSize = size;
                UpdateLayout(size);
            }

            var keyboard = Keyboard.current;

            if (keyboard == null || InputFocusUI.IsAnyInputFocused)
            {
                return;
            }

            if (keyboard.jKey.wasPressedThisFrame)
            {
                Toggle();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame && _panel.activeSelf)
            {
                Hide();
            }
        }

        public void Toggle()
        {
            if (_panel.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            if (_panel.activeSelf || TradeUI.Instance?.OpenAfterTrade(Show) == true)
            {
                return;
            }

            CharacterStash.Local?.Close();
            CharacterUI.Instance.Hide();
            CraftingUI.Instance.Hide();
            MerchantUI.Instance.Hide();
            GearUI.Instance.Hide();
            QuestUI.Instance.Hide();
            _panel.SetActive(true);
            UpdateLayout(((RectTransform)transform).rect.size);
            Refresh();
            Debug.Log("[QuestJournalUI] Journal opened.", this);
        }

        public void Hide()
        {
            _confirming = false;

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        public void Refresh()
        {
            if (_panel == null || !_panel.activeSelf)
            {
                return;
            }

            foreach (var button in _buttons)
            {
                button.SetActive(false);
                DestroyObject(button);
            }

            _buttons.Clear();

            var quests = QuestManager.Instance.CharacterQuests
                .Where(x => x.Status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished or CharacterQuestStatusEnum.Completed)
                .ToArray();

            if (!quests.Any(x => x.Id == _selectedId))
            {
                _selectedId = quests.Length == 0 ? null : quests[0].Id;
                _confirming = false;
            }

            foreach (var characterQuest in quests)
            {
                var definition = QuestManager.Instance.Quests
                    .Where(x => x.Id == characterQuest.QuestId)
                    .FirstOrDefault();

                if (definition == null)
                {
                    continue;
                }

                var obj = Instantiate(_textButtonPrefab, _list);
                var text = obj.GetComponent<TMP_Text>();
                text.enableAutoSizing = false;
                text.fontSize = 16;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Overflow;
                text.margin = new Vector4(4, 2, 4, 2);
                text.text = definition.Title;
                text.color = characterQuest.Id == _selectedId ? ColorUI.Green : ColorUI.White;
                obj.GetComponent<Button>().onClick.AddListener(() => Select(characterQuest.Id));
                _buttons.Add(obj);
            }

            UpdateRowHeights();

            _empty.text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.JournalEmpty);
            _empty.gameObject.SetActive(quests.Length == 0);
            var selected = quests
                .Where(x => x.Id == _selectedId)
                .FirstOrDefault();

            ShowDetails(selected);
        }

        private void UpdateLayout(Vector2 canvasSize)
        {
            var panel = (RectTransform)_panel.transform;
            var questSize = Vector2.Min(_questSize, canvasSize - new Vector2(32, 32));
            var left = Mathf.Min(_questOrigin.x, canvasSize.x - questSize.x - 16);
            var top = Mathf.Min(-_questOrigin.y, canvasSize.y - questSize.y - 16);
            var width = Mathf.Min(880, Mathf.Max(0, canvasSize.x - left - 16));
            var height = Mathf.Min(560, Mathf.Max(0, canvasSize.y - top - 16));
            panel.sizeDelta = new Vector2(width, height);
            panel.anchorMin = panel.anchorMax = new Vector2(0, 1);
            panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(left, -top);
            var narrow = width < 580;
            var listHeight = narrow ? Mathf.Min(110, height * 0.25f) : height - 76;
            var listWidth = narrow ? width - 24 : Mathf.Min(220, width * 0.3f);
            Place((RectTransform)_panel.transform.Find("List"), 12, 54, listWidth, listHeight);
            var detailsLeft = narrow ? 12 : listWidth + 24;
            var detailsTop = narrow ? 54 + listHeight + 34 : 88;
            var detailsWidth = width - detailsLeft - 12;
            Place((RectTransform)_details.transform, detailsLeft, detailsTop, detailsWidth, Mathf.Max(60, height - detailsTop - 58));
            Place((RectTransform)_status.transform, detailsLeft, detailsTop - 30, detailsWidth, 26);
            Place((RectTransform)_title.transform, 52, 8, width - 104, 36);
            Place((RectTransform)_empty.transform, 12, narrow ? 54 + listHeight + 45 : 95, width - 24, 42);
            Place((RectTransform)_abandon.transform, detailsLeft + Mathf.Max(0, (detailsWidth - 200) / 2), height - 48, Mathf.Min(200, detailsWidth), 36);
            UpdateRowHeights();
        }

        private void UpdateRowHeights()
        {
            Canvas.ForceUpdateCanvases();
            var width = ((RectTransform)_list.parent).rect.width - 24;

            foreach (var button in _buttons)
            {
                var text = button.GetComponent<TMP_Text>();
                var layout = button.GetComponent<LayoutElement>() ?? button.AddComponent<LayoutElement>();

                layout.preferredHeight = Mathf.Max(28, text.GetPreferredValues(text.text, Mathf.Max(1, width - 8), 0).y + 8);
            }
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void Select(int id)
        {
            _selectedId = id;
            _confirming = false;
            Debug.Log($"[QuestJournalUI] Quest selected. CharacterQuestId: {id}.", this);
            Refresh();
            _details.StopMovement();
            _details.verticalNormalizedPosition = 1f;
        }

        private void ShowDetails(CharacterQuestDto characterQuest)
        {
            ClearRows();
            var content = _details.content;
            var hasSelection = characterQuest != null;
            _details.gameObject.SetActive(hasSelection);
            _status.gameObject.SetActive(hasSelection);
            _abandon.gameObject.SetActive(hasSelection && characterQuest.Status != CharacterQuestStatusEnum.Completed);
            _title.text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.JournalTitle);

            if (!hasSelection)
            {
                return;
            }

            var quest = QuestManager.Instance.Quests
                .Where(x => x.Id == characterQuest.QuestId)
                .FirstOrDefault();

            if (quest == null)
            {
                return;
            }

            _title.text = quest.Title;
            var statusKey = characterQuest.Status == CharacterQuestStatusEnum.Completed ? TranslateKeyEnum.JournalStatusCompleted
                : characterQuest.Status == CharacterQuestStatusEnum.Finished ? TranslateKeyEnum.JournalStatusReady : TranslateKeyEnum.JournalStatusActive;
            _status.text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.JournalStatus) + " " + TranslateManager.Instance.GetByKey(statusKey);
            content.Find("Text").GetComponent<TMP_Text>().text = characterQuest.Status == CharacterQuestStatusEnum.Finished
                ? quest.CompleteDescription : quest.Description;
            content.Find("RequirementsTitle").GetComponent<TMP_Text>().text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.Requirements);
            content.Find("RewardsTitle").GetComponent<TMP_Text>().text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.Reward);

            foreach (var requirement in quest.Requirements)
            {
                var progress = characterQuest.ObjectiveProgress != null && characterQuest.ObjectiveProgress.TryGetValue(requirement.Id, out var value) ? value : 0;
                var row = Instantiate(_itemRowPrefab, content.Find("Requirements"));
                var slot = row.transform.Find("Slot");
                var label = row.transform.Find("Label").GetComponent<TMP_Text>();
                var image = slot.Find("Background").GetComponent<RawImage>();
                var itemType = requirement.ItemType;

                image.texture = requirement.Type == QuestTypeEnum.Collect ? InventoryUI.Instance.Textures[itemType] : Resources.Load<Texture>("Icons/Kill");
                image.color = ColorUI.White;
                slot.Find("Text").gameObject.SetActive(false);
                if (requirement.Type == QuestTypeEnum.Collect)
                {
                    ConfigureItemTooltip(slot, new InventoryItemDto { Type = itemType, Count = requirement.Count });
                }
                else
                {
                    slot.Find("Preview").gameObject.SetActive(false);
                    slot.GetComponent<HoverUI>().enabled = false;
                }

                label.text = QuestUI.FormatObjective(requirement, progress);
                label.color = progress >= requirement.Count ? ColorUI.Green : ColorUI.White;
                _rows.Add(row);
            }

            if (quest.Reward > 0)
            {
                AddReward(new InventoryItemDto { Type = InventoryItemEnum.Xp, Count = quest.Reward });
            }

            foreach (var item in quest.ItemRewards)
            {
                AddReward(item);
            }

            _abandon.interactable = CharacterQuests.Local != null && !CharacterQuests.Local.IsAbandonPending;
            _abandonText.text = TranslateManager.Instance.GetByKey(_confirming ? TranslateKeyEnum.JournalConfirmAbandon : TranslateKeyEnum.JournalAbandon);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private void AddReward(InventoryItemDto item)
        {
            var row = Instantiate(_itemRowPrefab, _details.content.Find("Rewards"));
            var slot = row.transform.Find("Slot");
            var title = TranslateManager.Instance.GetByKey(item.Type + "Title");
            var image = slot.Find("Background").GetComponent<RawImage>();

            image.texture = InventoryUI.Instance.Textures[item.Type];
            image.color = ColorUI.White;
            var count = slot.Find("Text").GetComponent<TMP_Text>();
            count.gameObject.SetActive(true);
            count.text = item.Count.ToString();
            row.transform.Find("Label").GetComponent<TMP_Text>().text = title;
            ConfigureItemTooltip(slot, item);
            _rows.Add(row);
        }

        private static void ConfigureItemTooltip(Transform slot, InventoryItemDto item)
        {
            var preview = slot.Find("Preview").gameObject;
            var title = TranslateManager.Instance.GetByKey(item.Type + "Title");

            ItemRarityUI.Apply(slot, item.Type);
            preview.transform.Find("Title").GetComponent<TMP_Text>().text = title;
            preview.transform.Find("Description").GetComponent<TMP_Text>().text = InventoryUI.Instance.PrepareDescription(item);
            preview.SetActive(false);
            slot.GetComponent<HoverUI>().enabled = true;
            var key = slot.gameObject.GetInstanceID().ToString();
            OnPointerEnterSubscription.Instance.Subscribe(key, _ => preview.SetActive(true));
            OnPointerExitSubscription.Instance.Subscribe(key, _ => preview.SetActive(false));
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                var key = row.transform.Find("Slot").gameObject.GetInstanceID().ToString();
                OnPointerEnterSubscription.Instance.Unsubscribe(key);
                OnPointerExitSubscription.Instance.Unsubscribe(key);
                row.SetActive(false);
                DestroyObject(row);
            }

            _rows.Clear();
        }

        private static void DestroyObject(GameObject obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(obj);

                return;
            }
#endif
            Destroy(obj);
        }

        private void Abandon()
        {
            if (!_selectedId.HasValue || CharacterQuests.Local == null || CharacterQuests.Local.IsAbandonPending)
            {
                return;
            }

            if (!_confirming)
            {
                _confirming = true;
                Refresh();

                return;
            }

            _confirming = false;
            Debug.Log($"[QuestJournalUI] Abandon requested. CharacterQuestId: {_selectedId.Value}.", this);
            CharacterQuests.Local.RequestAbandon(_selectedId.Value);
            Refresh();
        }

        protected override void OnDestroy()
        {
            QuestManager.Instance.CharacterQuestsChanged -= Refresh;
            ClearRows();
            base.OnDestroy();
        }
    }
}
