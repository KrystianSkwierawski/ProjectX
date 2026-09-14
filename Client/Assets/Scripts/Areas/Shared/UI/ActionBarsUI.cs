using System;
using System.Linq;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Inventory;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Shared.UI
{
    public sealed class ActionBarsUI : MonoSingleton<ActionBarsUI>
    {
        private static readonly Key[] ShortcutKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
        };

        private readonly Slot[] _slots = new Slot[CharacterSettingsManager.SlotCount];
        private RectTransform _bar;
        private GridLayoutGroup _layout;
        private Vector2 _lastCanvasSize;
        private InventoryItemEnum[] _bindings;
        private int _editRevision;

        protected override bool PersistBetweenScenes => false;

        private void Start()
        {
            _bar = (RectTransform)transform.Find("ActionBars");
            _layout = _bar.GetComponent<GridLayoutGroup>();
            _bindings = CharacterSettingsManager.Instance.Dto?.ActionBars?.ToArray()
                ?? new InventoryItemEnum[CharacterSettingsManager.SlotCount];

            for (var i = 0; i < _slots.Length; i++)
            {
                var index = i;
                var root = _bar.GetChild(i);
                var preview = root.Find("Preview").gameObject;
                var slot = new Slot
                {
                    Root = root,
                    Image = root.Find("Background").GetComponent<RawImage>(),
                    Count = root.Find("Text").GetComponent<TextMeshProUGUI>(),
                    Preview = preview,
                    Title = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                    Description = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                    Key = root.gameObject.GetInstanceID().ToString()
                };
                _slots[i] = slot;

                root.Find("Shortcut").GetComponent<TextMeshProUGUI>().text = ((i + 1) % 10).ToString();
                var button = root.GetComponent<ButtonUI>();
                button.onClick.RemoveAllListeners();

                button.OnRightClick.AddListener(() => InventoryUI.Instance.UseActionBarItem(GetBinding(index)));

                InventoryUI.Instance.ConfigureActionBarDrag(root.gameObject, slot.Image, slot.Count,
                    () => GetBinding(index), eventData =>
                    {
                        var target = Array.FindIndex(_slots, x => RectTransformUtility.RectangleContainsScreenPoint(
                            (RectTransform)x.Root, eventData.position, eventData.pressEventCamera));
                        if (target >= 0 && target != index)
                        {
                            (_bindings[index], _bindings[target]) = (_bindings[target], _bindings[index]);
                            PersistBindingsAsync().Forget();
                        }
                        else if (!RectTransformUtility.RectangleContainsScreenPoint(_bar, eventData.position, eventData.pressEventCamera))
                        {
                            SaveSlotAsync(index, InventoryItemEnum.None).Forget();
                        }
                    });

                OnPointerEnterSubscription.Instance.Subscribe(slot.Key, e =>
                {
                    if (!InventoryUI.Instance.IsDragging && GetBinding(index) != InventoryItemEnum.None)
                    {
                        slot.Preview.SetActive(true);
                    }
                });

                OnPointerExitSubscription.Instance.Subscribe(slot.Key, e => slot.Preview.SetActive(false));
            }

            RefreshInventory();
        }

        private void Update()
        {
            if (_bar == null)
            {
                return;
            }

            var canvasSize = ((RectTransform)transform).rect.size;

            if (canvasSize != _lastCanvasSize)
            {
                _lastCanvasSize = canvasSize;
                var columns = canvasSize.x >= 740f ? 10 : 5;
                var cellSize = Mathf.Min(64f, (canvasSize.x - 32f - (columns - 1) * 6f) / columns);
                _layout.constraintCount = columns;
                _layout.cellSize = new Vector2(cellSize, cellSize);
                _bar.sizeDelta = new Vector2(columns * (cellSize + 6f) - 6f, (10 / columns) * (cellSize + 6f) - 6f);
                // Leave a separate bottom row for QuickAccess on narrow screens.
                _bar.anchoredPosition = new Vector2(0f, canvasSize.x < 1200f ? 84f : 16f);
            }

            if (InventoryUI.Instance.IsDragging)
            {
                foreach (var slot in _slots)
                {
                    slot.Preview.SetActive(false);
                }
            }

            var keyboard = Keyboard.current;

            if (keyboard == null || InputFocusUI.IsAnyInputFocused
                || keyboard.ctrlKey.isPressed || keyboard.altKey.isPressed || keyboard.shiftKey.isPressed)
            {
                return;
            }

            for (var i = 0; i < ShortcutKeys.Length; i++)
            {
                if (keyboard[ShortcutKeys[i]].wasPressedThisFrame)
                {
                    InventoryUI.Instance.UseActionBarItem(GetBinding(i));
                    break;
                }
            }
        }

        private InventoryItemEnum GetBinding(int index) => _bindings?[index] ?? InventoryItemEnum.None;

        public bool TryAssignDrop(GameObject target, InventoryItemDto item)
        {
            if (target == null)
            {
                return false;
            }

            var index = Array.FindIndex(_slots, x => x != null && target.transform.IsChildOf(x.Root));

            if (index < 0)
            {
                return false;
            }

            if (item?.Type.IsActionBarItem() == true)
            {
                SaveSlotAsync(index, item.Type).Forget();
            }
            else
            {
                Debug.Log($"Action bar assignment rejected. Item: {item?.Type}, Slot: {index}");
                LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey(TranslateKeyEnum.ActionBarsUsableOnly), color: ColorUI.Red).Forget();
            }

            return true;
        }

        private UniTask SaveSlotAsync(int index, InventoryItemEnum type)
        {
            _bindings[index] = type;
            return PersistBindingsAsync();
        }

        private async UniTask PersistBindingsAsync()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            var revision = ++_editRevision;
            var snapshot = _bindings.ToArray();
            RefreshInventory();

            try
            {
                await CharacterSettingsManager.Instance.SaveBindingsAsync(snapshot, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Action bar save failed. CharacterId: {UserManager.Instance.SelectedCharacterId}, Revision: {revision}, Error: {exception.Message}");

                if (!cancellationToken.IsCancellationRequested)
                {
                    if (revision == _editRevision)
                    {
                        InventoryUI.Instance.CancelDrag();
                        _bindings = CharacterSettingsManager.Instance.Dto.ActionBars.ToArray();
                        RefreshInventory();
                    }

                    LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey(TranslateKeyEnum.CharacterSettingsSaveFailed), color: ColorUI.Red).Forget();
                }
            }
        }

        public void RefreshInventory()
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                if (slot == null)
                {
                    continue;
                }

                var type = GetBinding(i);
                var count = InventoryManager.Instance.Dto?.Inventory?.Items
                    .Where(x => x.Type == type).Sum(x => x.Count) ?? 0;

                InventoryUI.Instance.Textures.TryGetValue(type, out var texture);
                slot.Image.texture = type == InventoryItemEnum.None ? null : texture ?? Resources.Load<Texture>($"Icons/{type}");
                slot.Image.color = type == InventoryItemEnum.None ? ColorUI.Black : count > 0 ? ColorUI.White : Color.gray;
                slot.Count.gameObject.SetActive(type != InventoryItemEnum.None);
                slot.Count.text = count.ToString();
                slot.Preview.SetActive(false);
                slot.Title.text = type == InventoryItemEnum.None ? string.Empty : TranslateManager.Instance.GetByKey($"{type}Title");
                slot.Description.text = type == InventoryItemEnum.None ? string.Empty
                    : InventoryUI.Instance.PrepareDescription(new InventoryItemDto { Type = type, Count = 1 })
                        + "\n" + TranslateManager.Instance.GetByKey(TranslateKeyEnum.ActionBarsClearHint);
            }
        }

        protected override void OnDestroy()
        {
            foreach (var slot in _slots.Where(x => x != null))
            {
                OnPointerEnterSubscription.Instance.Unsubscribe(slot.Key);
                OnPointerExitSubscription.Instance.Unsubscribe(slot.Key);
            }

            base.OnDestroy();
        }

        private sealed class Slot
        {
            public Transform Root;
            public RawImage Image;
            public TextMeshProUGUI Count;
            public GameObject Preview;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Description;
            public string Key;
        }
    }
}
