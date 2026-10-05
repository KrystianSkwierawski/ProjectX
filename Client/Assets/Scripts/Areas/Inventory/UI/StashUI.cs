using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Mono;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Inventory.UI
{
    public sealed class StashUI : MonoSingleton<StashUI>
    {
        [SerializeField] private GameObject _inventorySlotPrefab;

        private GameObject _panel;
        private RectTransform _content;
        private GridLayoutGroup _grid;
        private RectTransform _viewport;
        private Vector2 _defaultPosition;
        private Vector2 _defaultSize;
        private readonly List<GameObject> _slots = new List<GameObject>();

        public CharacterStashDto Dto { get; private set; }

        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void Start()
        {
            _panel = transform.Find("Stash").gameObject;
            _content = (RectTransform)_panel.transform.Find("Viewport/Content");
            _grid = _content.GetComponent<GridLayoutGroup>();
            _viewport = _panel.GetComponent<ScrollRect>().viewport;
            _defaultPosition = ((RectTransform)_panel.transform).anchoredPosition;
            _defaultSize = ((RectTransform)_panel.transform).sizeDelta;
            transform.Find("Stash/Header/Close").GetComponent<Button>().onClick.AddListener(() => CharacterStash.Local?.Close());
            _panel.SetActive(false);
        }

        public void Show(CharacterStashDto dto)
        {
            Dto = dto;

            if (!IsOpen)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

                CraftingUI.Instance.Hide();
                MerchantUI.Instance.Hide();
                QuestUI.Instance.Hide();
                GearUI.Instance.Hide();
                CharacterUI.Instance.Hide();
                InventoryUI.Instance.Inventory.SetActive(true);
            }

            _panel.SetActive(true);

            while (_slots.Count < dto.Count)
            {
                CreateSlot(_slots.Count);
            }

            InventoryUI.Instance.CancelDrag();

            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var item = Item(i);
                var empty = item == null || item.Type == InventoryItemEnum.None || item.Count <= 0;
                var image = slot.transform.Find("Background").GetComponent<RawImage>();
                var text = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>();

                image.texture = empty ? null : InventoryUI.Instance.Textures[item.Type];
                image.color = empty ? ColorUI.Black : ColorUI.White;
                text.gameObject.SetActive(!empty);
                text.text = empty ? string.Empty : item.Count.ToString();
                slot.transform.Find("Preview").gameObject.SetActive(false);
                slot.GetComponent<HoverUI>().enabled = !empty;

                if (!empty)
                {
                    slot.transform.Find("Preview/Title").GetComponent<TextMeshProUGUI>().text = TranslateManager.Instance.GetByKey($"{item.Type}Title");
                    slot.transform.Find("Preview/Description").GetComponent<TextMeshProUGUI>().text = InventoryUI.Instance.PrepareDescription(item);
                }
            }

            UpdateLayout();
        }

        private void CreateSlot(int index)
        {
            var slot = Instantiate(_inventorySlotPrefab, _content);
            _slots.Add(slot);
            var image = slot.transform.Find("Background").GetComponent<RawImage>();
            var text = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            var preview = slot.transform.Find("Preview").gameObject;
            var key = slot.GetInstanceID().ToString();

            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = image.rectTransform.sizeDelta - new Vector2(4, 4);
            text.alignment = TextAlignmentOptions.BottomRight;
            text.raycastTarget = false;

            slot.GetComponent<ButtonUI>().OnRightClick.AddListener(() =>
            {
                if (Item(index) == null || Item(index).Count <= 0)
                {
                    return;
                }

                var operation = Keyboard.current?.altKey.isPressed == true ? StashOperationEnum.Split : StashOperationEnum.Withdraw;

                if (operation == StashOperationEnum.Split && Item(index)?.Count < 2)
                {
                    return;
                }

                CharacterStash.Local?.Request(operation, index, null, Item(index));
            });

            OnPointerEnterSubscription.Instance.Subscribe(key, _ =>
            {
                if (!InventoryUI.Instance.IsDragging && Item(index)?.Count > 0)
                {
                    preview.SetActive(true);
                }
            });
            OnPointerExitSubscription.Instance.Subscribe(key, _ => preview.SetActive(false));

            InventoryUI.Instance.ConfigureItemDrag(slot, image, text,
                () => CharacterStash.Local?.IsBusy == true ? InventoryItemEnum.None : Item(index)?.Type ?? InventoryItemEnum.None,
                data =>
                {
                    data.eligibleForClick = false;
                    var target = data.pointerCurrentRaycast.gameObject;
                    var stashIndex = GetSlotIndex(target);
                    var inventoryIndex = InventoryUI.Instance.GetSlotIndex(target);

                    if (stashIndex >= 0 && stashIndex != index)
                    {
                        CharacterStash.Local.Request(StashOperationEnum.Move, index, stashIndex, Item(index));
                    }
                    else if (inventoryIndex >= 0)
                    {
                        CharacterStash.Local.Request(StashOperationEnum.Withdraw, index, inventoryIndex, Item(index));
                    }
                });

            var trigger = slot.GetComponent<EventTrigger>();
            var scroll = new EventTrigger.Entry { eventID = EventTriggerType.Scroll };
            scroll.callback.AddListener(data => _panel.GetComponent<ScrollRect>().OnScroll((PointerEventData)data));
            trigger.triggers.Add(scroll);
        }

        private InventoryItemDto Item(int index) => Dto?.Inventory?.Items?.ElementAtOrDefault(index);

        public int GetSlotIndex(GameObject target)
        {
            return IsOpen && target != null ? _slots.FindIndex(x => target.transform.IsChildOf(x.transform)) : -1;
        }

        public void Deposit(int source, int? target = null)
        {
            var item = InventoryManager.Instance.Dto?.Inventory?.Items?.ElementAtOrDefault(source);
            CharacterStash.Local?.Request(StashOperationEnum.Deposit, source, target, item);
        }

        public void Hide()
        {
            if (IsOpen)
            {
                AudioManager.Instance?.TryPlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);
            }

            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            InventoryUI.Instance?.CancelDrag();
        }

        public void ShowFailure(StashStatusEnum? status)
        {
            var key = status == StashStatusEnum.Full ? TranslateKeyEnum.StashFull : TranslateKeyEnum.StashChanged;
            LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey(key), color: ColorUI.Red).Forget();
        }

        private void LateUpdate()
        {
            if (IsOpen)
            {
                if (!InventoryUI.Instance.Inventory.activeSelf || CraftingUI.Instance.Crafting.activeSelf
                    || MerchantUI.Instance.Merchant.activeSelf || QuestUI.Instance.Quest.activeSelf
                    || GearUI.Instance.Gear.activeSelf || CharacterUI.Instance.Character.activeSelf)
                {
                    CharacterStash.Local?.Close();

                    return;
                }

                UpdateLayout();
            }
        }

        private void UpdateLayout()
        {
            var canvas = (RectTransform)transform;
            var panel = (RectTransform)_panel.transform;
            var maximumWidth = Mathf.Min(_defaultSize.x, canvas.rect.width - 24);
            var horizontalInsets = -_viewport.sizeDelta.x + _grid.padding.horizontal;
            var columns = Mathf.Max(1, Mathf.FloorToInt(
                (maximumWidth - horizontalInsets + _grid.spacing.x) / (_grid.cellSize.x + _grid.spacing.x)));
            var width = horizontalInsets + columns * (_grid.cellSize.x + _grid.spacing.x) - _grid.spacing.x;
            var height = Mathf.Min(_defaultSize.y, canvas.rect.height - 160);
            var horizontalLimit = Mathf.Max(0, (canvas.rect.width - width) / 2 - 12);
            var verticalLimit = Mathf.Max(0, (canvas.rect.height - height) / 2 - 12);

            panel.sizeDelta = new Vector2(width, height);
            panel.anchoredPosition = new Vector2(
                Mathf.Clamp(_defaultPosition.x - (_defaultSize.x - width) / 2, -horizontalLimit, horizontalLimit),
                Mathf.Clamp(_defaultPosition.y, -verticalLimit, verticalLimit));

            _grid.constraintCount = columns;
        }

        protected override void OnDestroy()
        {
            foreach (var slot in _slots.Where(x => x != null))
            {
                var key = slot.GetInstanceID().ToString();
                OnPointerEnterSubscription.Instance.Unsubscribe(key);
                OnPointerExitSubscription.Instance.Unsubscribe(key);
            }

            base.OnDestroy();
        }
    }
}
