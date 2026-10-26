using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Inventory.UI
{
    // Uses the slot's authored preview and typography. The companion belongs to it,
    // so pointer exit, drag, pooling and window closure hide both together.
    public sealed class ItemComparisonUI : MonoBehaviour
    {
        private Func<InventoryItemDto> _item;
        private RectTransform _rect;
        private RectTransform _equippedPanel;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _description;
        private TextMeshProUGUI _equippedTitle;
        private TextMeshProUGUI _equippedDescription;
        private HoverUI _hover;
        private float _originalWidth;
        private InventoryItemEnum _lastItem;
        private InventoryItemEnum _lastEquipped;
        private int _lastCount;
        private Vector2 _lastScreen;
        private Vector2 _lastCanvasSize;
        private string _lastLabel;
        private string _lastDescription;
        private bool _dirty = true;

        public static void Bind(GameObject preview, Func<InventoryItemDto> item)
        {
            var comparison = preview.AddComponent<ItemComparisonUI>();
            comparison._item = item;
            comparison._rect = (RectTransform)preview.transform;
            comparison._originalWidth = comparison._rect.rect.width;
            comparison._title = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            comparison._description = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>();
            comparison._hover = preview.GetComponentInParent<HoverUI>();

            foreach (var graphic in preview.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        public static InventoryItemEnum EquippedFor(InventoryItemEnum candidate, CharacterDto character)
        {
            if (character == null)
            {
                return InventoryItemEnum.None;
            }

            var equipped = candidate.GetGearSlot() switch
            {
                InventoryItemEnum.HelmetTemplate => character.HelmetType,
                InventoryItemEnum.ChestTemplate => character.ChestType,
                InventoryItemEnum.BootsTemplate => character.BootsType,
                InventoryItemEnum.WeaponTemplate => character.WeaponType,
                InventoryItemEnum.AmmoTemplate when character.AmmoCount > 0 => character.AmmoType,
                _ => InventoryItemEnum.None,
            };

            return equipped.IsGear() ? equipped : InventoryItemEnum.None;
        }

        private void OnEnable()
        {
            _dirty = true;

            if (_item != null)
            {
                Refresh();
            }
        }

        private void LateUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            var item = _item?.Invoke();

            if (item == null || item.Count <= 0 || item.Type == InventoryItemEnum.None)
            {
                gameObject.SetActive(false);

                return;
            }

            CharacterDto character = null;

            if (NetworkManager.Singleton != null)
            {
                UserManager.Instance.Characters.TryGetValue(NetworkManager.Singleton.LocalClientId, out character);
            }

            var equipped = EquippedFor(item.Type, character);
            var screen = new Vector2(Screen.width, Screen.height);
            var canvas = _rect.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
            var canvasSize = canvas.rect.size;
            var label = TranslateManager.Instance.GetByKey(TranslateKeyEnum.CurrentlyEquipped);

            if (!_dirty && item.Type == _lastItem && item.Count == _lastCount
                && equipped == _lastEquipped && screen == _lastScreen && label == _lastLabel
                && canvasSize == _lastCanvasSize
                && _description.text == _lastDescription)
            {
                return;
            }

            _dirty = false;
            _lastItem = item.Type;
            _lastEquipped = equipped;
            _lastCount = item.Count;
            _lastScreen = screen;
            _lastCanvasSize = canvasSize;
            _lastLabel = label;

            _title.text = TranslateManager.Instance.GetByKey($"{item.Type}Title");
            _description.text = InventoryUI.Instance.PrepareDescription(item, equipped);
            _lastDescription = _description.text;

            if (equipped == InventoryItemEnum.None)
            {
                _equippedPanel?.gameObject.SetActive(false);
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _originalWidth);
                _hover?.ClampPreviewToCanvas();

                return;
            }

            EnsureEquippedPanel();
            _equippedPanel.gameObject.SetActive(true);
            _equippedTitle.text = $"<color=#E8C779>{label}</color>\n{TranslateManager.Instance.GetByKey($"{equipped}Title")}";
            _equippedDescription.text = InventoryUI.Instance.PrepareDescription(new InventoryItemDto { Type = equipped, Count = 1 });

            var available = canvas.rect.width - 24f;
            var width = Mathf.Min(260f, available);
            var stacked = available < width * 2f + 12f;

            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _equippedPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _equippedPanel.anchorMin = _equippedPanel.anchorMax = stacked ? Vector2.zero : Vector2.one;
            _equippedPanel.pivot = new Vector2(0, 1);
            _equippedPanel.anchoredPosition = stacked ? new Vector2(0, -12) : new Vector2(12, 0);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_equippedPanel);
            _hover?.ClampPreviewToCanvas();
        }

        private void EnsureEquippedPanel()
        {
            if (_equippedPanel != null)
            {
                return;
            }

            var panel = new GameObject("EquippedComparison", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            panel.layer = gameObject.layer;
            _equippedPanel = (RectTransform)panel.transform;
            _equippedPanel.SetParent(transform, false);
            panel.GetComponent<LayoutElement>().ignoreLayout = true;

            var background = panel.GetComponent<Image>();
            var source = GetComponent<Image>();
            background.sprite = source.sprite;
            background.type = source.type;
            background.color = source.color;
            background.raycastTarget = false;

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _equippedTitle = Instantiate(_title, _equippedPanel);
            _equippedDescription = Instantiate(_description, _equippedPanel);
            _equippedTitle.raycastTarget = false;
            _equippedDescription.raycastTarget = false;
        }
    }
}
