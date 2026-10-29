using Assets.Scripts.Areas.Inventory;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Inventory.Mono;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Quest.Mono;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Trade.UI;

namespace Assets.Scripts.Areas.Quest.UI
{
    public class QuestUI : MonoSingleton<QuestUI>
    {
        #region Prefab

        [SerializeField] private GameObject _textPrefab;
        [SerializeField] private GameObject _itemRowPrefab;

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


        #endregion

        #region Material

        public Material Material001 { get; private set; }

        public Material Material002 { get; private set; }

        #endregion

        private ObjectPool<QuestLogPoolObject> _questLogObjectPool;
        private ScrollRect _descriptionScroll;
        private float _nextRequirementRefresh;
        private Transform _requirements;
        private Transform _rewards;
        private Vector2 _panelSize;
        private Vector2 _panelPosition;
        private readonly Vector3[] _lootCorners = new Vector3[4];
        private float _logWidth;
        private readonly List<GameObject> _detailRows = new List<GameObject>();
        private readonly List<Action> _requirementRefresh = new List<Action>();
        private readonly IDictionary<QuestEnum, QuestLogPoolObject> _questLogObjects = new Dictionary<QuestEnum, QuestLogPoolObject>();

        public QuestNpc DisplayedNpc { get; private set; }

        public void Start()
        {
            QuestCanvas = GameObject.Find("QuestCanvas");
            Quest = QuestCanvas.transform.Find("Quest").gameObject;
            Quest.transform.Find("Close").GetComponent<Button>().onClick.AddListener(Hide);
            QuestLog = QuestCanvas.transform.Find("Log").gameObject;
            QuestLogContent = QuestLog.transform.Find("Viewport/Content").gameObject;
            QuestAcceptButtonText = QuestCanvas.transform.Find("Quest/AcceptButton/Text").GetComponent<TextMeshProUGUI>();
            QuestTitleText = QuestCanvas.transform.Find("Quest/Title").GetComponent<TextMeshProUGUI>();
            QuestDescriptionText = QuestCanvas.transform.Find("Quest/Description/Viewport/Content/Text").GetComponent<TextMeshProUGUI>();
            _descriptionScroll = QuestCanvas.transform.Find("Quest/Description").GetComponent<ScrollRect>();
            _requirements = _descriptionScroll.content.Find("Requirements");
            _rewards = _descriptionScroll.content.Find("Rewards");
            _panelSize = ((RectTransform)Quest.transform).sizeDelta;
            _panelPosition = ((RectTransform)Quest.transform).anchoredPosition;
            _logWidth = ((RectTransform)QuestLog.transform).sizeDelta.x;

            QuestAcceptButton = QuestCanvas.transform.Find("Quest/AcceptButton").GetComponent<Button>();
            Material001 = Resources.Load<Material>("Materials/Material.001");
            Material002 = Resources.Load<Material>("Materials/Material.002");

            _questLogObjectPool = new ObjectPool<QuestLogPoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_textPrefab, QuestLogContent.transform);
                    var mesh = obj.GetComponent<TextMeshProUGUI>();

                    mesh.alignment = TextAlignmentOptions.TopLeft;
                    mesh.margin = new Vector4(0, 2, 0, 2);

                    return new QuestLogPoolObject
                    {
                        GameObject = obj,
                        Mesh = mesh
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
            if (questNpc == null || questNpc.Quest == null)
            {
                return;
            }

            if (TradeUI.Instance?.OpenAfterTrade(() => Show(questNpc)) == true)
            {
                return;
            }

            CharacterStash.Local?.Close();

            // FIXME: array
            CharacterUI.Instance.Hide();
            CraftingUI.Instance.Hide();
            MerchantUI.Instance.Hide();
            GearUI.Instance.Hide();

            DisplayedNpc = questNpc;
            Quest.SetActive(true);

            QuestTitleText.text = questNpc.Quest.Title;

            RefreshPanelBounds();
            ShowDetails(questNpc.Quest);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_descriptionScroll.content);
            _descriptionScroll.StopMovement();
            _descriptionScroll.verticalNormalizedPosition = 1f;

            Debug.Log($"[QuestUI] Displayed quest {questNpc.Quest.Id}, status={questNpc.CharacterQuest?.Status.ToString() ?? "Available"}.", this);
        }

        private void ShowDetails(QuestDto quest)
        {
            ClearDetails();

            _descriptionScroll.content.Find("RequirementsTitle").GetComponent<TextMeshProUGUI>().text =
                TranslateManager.Instance.GetByKey(TranslateKeyEnum.Requirements);
            _descriptionScroll.content.Find("RewardsTitle").GetComponent<TextMeshProUGUI>().text =
                TranslateManager.Instance.GetByKey(TranslateKeyEnum.Reward);

            foreach (var requirement in quest.Requirements)
            {
                if (requirement.Type == QuestTypeEnum.Collect)
                {
                    AddDetailItem(new InventoryItemDto { Type = requirement.ItemType, Count = requirement.Count }, _requirements, requirement);
                }
                else
                {
                    var row = Instantiate(_itemRowPrefab, _requirements);
                    var label = row.transform.Find("Label").GetComponent<TextMeshProUGUI>();

                    var slot = row.transform.Find("Slot");
                    var image = slot.Find("Background").GetComponent<RawImage>();

                    image.texture = Resources.Load<Texture>("Icons/Kill");
                    image.color = ColorUI.White;
                    slot.Find("Text").gameObject.SetActive(false);
                    slot.Find("Preview").gameObject.SetActive(false);
                    slot.GetComponent<HoverUI>().enabled = false;
                    _detailRows.Add(row);
                    _requirementRefresh.Add(() =>
                    {
                        var progress = GetProgress(DisplayedNpc.CharacterQuest, requirement.Id);

                        label.text = FormatObjective(requirement, progress);
                        label.color = progress >= requirement.Count ? ColorUI.Green : ColorUI.White;
                    });
                }
            }

            if (quest.Reward > 0)
            {
                AddDetailItem(new InventoryItemDto { Type = InventoryItemEnum.Xp, Count = quest.Reward }, _rewards, null);
            }

            foreach (var item in quest.ItemRewards)
            {
                AddDetailItem(item, _rewards, null);
            }

            RefreshRequirements();
        }

        private void AddDetailItem(InventoryItemDto item, Transform parent, QuestRequirementDto requirement)
        {
            var row = Instantiate(_itemRowPrefab, parent);
            var slot = row.transform.Find("Slot");
            var preview = slot.Find("Preview").gameObject;
            var title = TranslateManager.Instance.GetByKey(item.Type + "Title");
            var count = slot.Find("Text").GetComponent<TextMeshProUGUI>();
            var label = row.transform.Find("Label").GetComponent<TextMeshProUGUI>();

            var image = slot.Find("Background").GetComponent<RawImage>();

            image.color = ColorUI.White;
            image.texture = InventoryUI.Instance.Textures[item.Type];
            ItemRarityUI.Apply(slot, item.Type);
            preview.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = title;
            preview.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = InventoryUI.Instance.PrepareDescription(item);
            preview.SetActive(false);
            slot.GetComponent<HoverUI>().enabled = true;

            var key = slot.gameObject.GetInstanceID().ToString();

            OnPointerEnterSubscription.Instance.Subscribe(key, _ => preview.SetActive(true));
            OnPointerExitSubscription.Instance.Subscribe(key, _ => preview.SetActive(false));
            count.gameObject.SetActive(true);
            count.text = item.Count.ToString();
            label.text = title;
            _detailRows.Add(row);

            if (requirement != null)
            {
                count.gameObject.SetActive(false);
                _requirementRefresh.Add(() =>
                {
                    var available = InventoryManager.Instance.Dto.Inventory.Items
                        .Where(x => x.Type == item.Type)
                        .Sum(x => x.Count);

                    label.text = FormatObjective(requirement, available);
                    label.color = available >= item.Count ? ColorUI.Green : ColorUI.White;
                });
            }
        }

        private void LateUpdate()
        {
            RefreshPanelBounds();

            if (DisplayedNpc != null && Quest.activeInHierarchy && Time.unscaledTime >= _nextRequirementRefresh)
            {
                _nextRequirementRefresh = Time.unscaledTime + 0.2f;
                RefreshRequirements();
            }
        }

        private void RefreshPanelBounds()
        {
            if (QuestCanvas == null)
            {
                return;
            }

            var canvasSize = ((RectTransform)QuestCanvas.transform).rect.size;

            var log = (RectTransform)QuestLog.transform;

            log.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(_logWidth, Mathf.Max(0, canvasSize.x - 32)));

            var panel = (RectTransform)Quest.transform;
            var size = Vector2.Min(_panelSize, canvasSize - new Vector2(32, 32));

            var position = new Vector2(
                Mathf.Min(_panelPosition.x, canvasSize.x - size.x - 16),
                -Mathf.Min(-_panelPosition.y, canvasSize.y - size.y - 16));
            var loot = InventoryUI.Instance?.Loot;

            if (loot != null && loot.activeInHierarchy)
            {
                var canvasRect = (RectTransform)QuestCanvas.transform;
                var canvas = QuestCanvas.GetComponent<Canvas>();
                var lootCanvas = loot.GetComponentInParent<Canvas>();
                var lootCamera = lootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : lootCanvas.worldCamera;
                var questCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

                ((RectTransform)loot.transform).GetWorldCorners(_lootCorners);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                    RectTransformUtility.WorldToScreenPoint(lootCamera, _lootCorners[0]), questCamera, out var lootMin);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                    RectTransformUtility.WorldToScreenPoint(lootCamera, _lootCorners[2]), questCamera, out var lootMax);

                var panelLeft = canvasRect.rect.xMin + position.x;
                var panelTop = canvasRect.rect.yMax + position.y;

                if (panelLeft < lootMax.x + 8 && panelLeft + size.x > lootMin.x - 8
                    && panelTop > lootMin.y && panelTop - size.y < lootMax.y + 8)
                {
                    var leftOfLoot = lootMin.x - canvasRect.rect.xMin - size.x - 8;
                    var rightOfLoot = lootMax.x - canvasRect.rect.xMin + 8;

                    if (leftOfLoot >= 16)
                    {
                        position.x = leftOfLoot;
                    }
                    else if (rightOfLoot + size.x <= canvasSize.x - 16)
                    {
                        position.x = rightOfLoot;
                    }
                    else
                    {
                        position.y = -16;
                        panelTop = canvasRect.rect.yMax + position.y;
                        size.y = Mathf.Min(size.y, Mathf.Max(180, panelTop - lootMax.y - 8));
                    }
                }
            }

            if (panel.sizeDelta != size || panel.anchoredPosition != position)
            {
                panel.sizeDelta = size;
                panel.anchoredPosition = position;
            }
        }

        private static int GetProgress(CharacterQuestDto characterQuest, string requirementId)
        {
            return characterQuest?.ObjectiveProgress != null
                && characterQuest.ObjectiveProgress.TryGetValue(requirementId, out var progress) ? progress : 0;
        }

        private static string FormatObjective(QuestRequirementDto requirement, int progress)
        {
            var action = TranslateManager.Instance.GetByKey(requirement.Type == QuestTypeEnum.Kill
                ? TranslateKeyEnum.QuestObjectiveKill : TranslateKeyEnum.QuestObjectiveCollect);
            var target = requirement.Type == QuestTypeEnum.Kill
                ? TranslateManager.Instance.GetByKey((requirement.Target ?? string.Empty).Replace("(Clone)", string.Empty) + "ObjectiveTarget")
                : TranslateManager.Instance.GetByKey(requirement.ItemType + "ObjectiveTarget");

            if (string.IsNullOrEmpty(target))
            {
                target = requirement.Type == QuestTypeEnum.Collect
                    ? TranslateManager.Instance.GetByKey(requirement.ItemType + "Title")
                    : requirement.Target?.Replace("(Clone)", string.Empty) ?? string.Empty;
            }

            return $"{action} {target} {Mathf.Clamp(progress, 0, requirement.Count)}/{requirement.Count}";
        }

        private static string FormatProgress(QuestDto quest, CharacterQuestDto characterQuest)
        {
            var green = ColorUtility.ToHtmlStringRGB(ColorUI.Green);
            var lines = quest.Requirements
                .Select(requirement =>
                {
                    var progress = GetProgress(characterQuest, requirement.Id);
                    var text = $"- {FormatObjective(requirement, progress)}";

                    return progress >= requirement.Count ? $"<color=#{green}>{text}</color>" : text;
                })
                .ToArray();

            return $"{quest.Title}\n{string.Join("\n", lines)}";
        }

        private void RefreshRequirements()
        {
            var quest = DisplayedNpc.Quest;
            var status = DisplayedNpc.CharacterQuest?.Status;

            QuestDescriptionText.text = status == CharacterQuestStatusEnum.Finished
                ? quest.CompleteDescription : quest.Description;

            QuestAcceptButton.interactable = status != CharacterQuestStatusEnum.Accepted && status != CharacterQuestStatusEnum.Completed;
            QuestAcceptButtonText.text = TranslateManager.Instance.GetByKey(status == CharacterQuestStatusEnum.Finished
                ? TranslateKeyEnum.Complete : status == CharacterQuestStatusEnum.Accepted ? TranslateKeyEnum.QuestInProgress : TranslateKeyEnum.Accept);

            foreach (var refresh in _requirementRefresh)
            {
                refresh();
            }
        }

        private void ClearDetails()
        {
            foreach (var row in _detailRows)
            {
                if (row == null)
                {
                    continue;
                }

                var key = row.transform.Find("Slot").gameObject.GetInstanceID().ToString();

                row.SetActive(false);
                OnPointerEnterSubscription.Instance.Unsubscribe(key);
                OnPointerExitSubscription.Instance.Unsubscribe(key);
                Destroy(row);
            }

            _detailRows.Clear();
            _requirementRefresh.Clear();
        }

        protected override void OnDestroy()
        {
            ClearDetails();

            base.OnDestroy();
        }

        public void Hide()
        {
            DisplayedNpc = null;

            if (Quest.activeSelf)
            {
                Quest.SetActive(false);
            }
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

            questLogObject.Mesh.text = FormatProgress(quest, characterQuest);

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

                questLogObject.Mesh.text = FormatProgress(quest, characterQuest);

                if (characterQuest.Status == CharacterQuestStatusEnum.Finished)
                {
                    questLogObject.Mesh.color = ColorUI.Green;
                }
                else
                {
                    questLogObject.Mesh.color = ColorUI.White;
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
