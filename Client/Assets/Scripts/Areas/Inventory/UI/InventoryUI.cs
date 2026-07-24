using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Inventory.UI
{
    public class InventoryUI : MonoSingleton<InventoryUI>
    {
        public readonly IDictionary<InventoryItemEnum, Texture> Textures = new Dictionary<InventoryItemEnum, Texture>();

        #region Prefab

        [SerializeField] private GameObject _inventorySlotPrefab;

        [SerializeField] private GameObject _inventoryDragPreviewPrefab;

        #endregion

        #region GameObject

        public GameObject InventoryCanvas { get; private set; }

        public GameObject Inventory { get; private set; }

        public GameObject InventoryContent { get; private set; }

        public GameObject Loot { get; private set; }

        public GameObject LootContent { get; private set; }

        #endregion

        private ObjectPool<InventorySlot> _lootObjectPool;
        private readonly IDictionary<InventoryItemEnum, InventorySlot> _lootPoolObjects = new Dictionary<InventoryItemEnum, InventorySlot>();
        private InventorySlot[] _inventorySlots;
        private GameObject _dragPreview;
        private Canvas _dragCanvas;
        private int _draggedSlotIndex = -1;

        public void Start()
        {
            InventoryCanvas = GameObject.Find("InventoryCanvas");
            Inventory = InventoryCanvas.transform.Find("Inventory").gameObject;
            InventoryContent = Inventory.transform.Find("Viewport/Content").gameObject;
            Loot = InventoryCanvas.transform.Find("Loot").gameObject;
            LootContent = Loot.transform.Find("Viewport/Content").gameObject;
            InitTextures();

            _lootObjectPool = new ObjectPool<InventorySlot>(
               createFunc: () =>
               {
                   var obj = Instantiate(_inventorySlotPrefab, LootContent.transform);

                   var mesh = obj.transform.Find("Text").GetComponent<TextMeshProUGUI>();

                   mesh.gameObject.SetActive(true);

                   var preview = obj.transform.Find("Preview").gameObject;

                   return new InventorySlot
                   {
                       GameObject = obj,
                       Image = obj.transform.Find("Background").GetComponent<RawImage>(),
                       Mesh = mesh,
                       Button = obj.GetComponent<ButtonUI>(),
                       Preview = preview,
                       PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                       PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                   };
               },
               actionOnGet: (InventorySlot obj) => obj.GameObject.SetActive(true),
               actionOnRelease: (InventorySlot obj) =>
               {
                   obj.Button.OnRightClick.RemoveAllListeners();

                   obj.GameObject.SetActive(false);
               }
            );
        }

        public void Toggle()
        {
            if (Inventory.activeSelf)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);

                ClearDragPreview();
                Inventory.SetActive(false);

                return;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

            Inventory.SetActive(true);
        }

        public void UpdateInventory(CharacterInventoryDto dto)
        {
            _inventorySlots ??= InstantiateInventorySlots(dto.Count).ToArray();

            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                var slot = _inventorySlots[i];
                var item = dto.Inventory.Items.ElementAtOrDefault(i);

                if (item == null || item.Type == InventoryItemEnum.None || item.Count <= 0)
                {
                    slot.Mesh.gameObject.SetActive(false);
                    slot.Mesh.text = "0";
                    slot.Image.color = ColorUI.Black;
                    slot.Image.texture = null;
                    slot.Type = InventoryItemEnum.None;
                    slot.HoverUI.enabled = false;
                    slot.DragTrigger.enabled = false;
                    slot.Button.OnRightClick.RemoveAllListeners();
                    slot.Preview.SetActive(false);

                    continue;
                }

                slot.Mesh.gameObject.SetActive(true);
                slot.Mesh.text = item.Count > 1000 ? $"~{item.Count / 1000}k" : item.Count.ToString();
                slot.Image.color = ColorUI.White;
                slot.Image.texture = Textures[item.Type];
                slot.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{item.Type}Title");

                var description = PrepareDescription(item);

                slot.PreviewDescriptionMesh.text = description.ToString();

                slot.Type = item.Type;
                slot.HoverUI.enabled = true;
                slot.DragTrigger.enabled = true;

                slot.Button.OnRightClick.RemoveAllListeners();

                var slotIndex = i;

                slot.Button.OnRightClick.AddListener(() =>
                {
                    if (Keyboard.current.altKey.isPressed)
                    {
                        SplitStack(slotIndex);

                        return;
                    }

                    UseItemSubscribtion.Instance.Invoke(UserManager.Instance.OwnerClientId.ToString(), new UseItemSubscribtionEvent
                    {
                        Item = new InventoryItemDto
                        {
                            Type = item.Type,
                            Count = item.Type.IsAmmo() ? item.Count : 1
                        },
                        From = UsableItemFromEnum.Inventory,
                    });
                });
            }
        }

        private void BeginDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (slot.Type == InventoryItemEnum.None
                || slot.Index < 0
                || slot.Index >= InventoryManager.Instance.Dto.Inventory.Items.Count)
            {
                return;
            }

            ClearDragPreview();
            _draggedSlotIndex = slot.Index;

            foreach (var inventorySlot in _inventorySlots)
            {
                inventorySlot.Preview.SetActive(false);
            }

            _dragCanvas = InventoryCanvas.GetComponentInParent<Canvas>();

            if (_dragCanvas == null)
            {
                _draggedSlotIndex = -1;

                return;
            }

            if (_inventoryDragPreviewPrefab == null)
            {
                _draggedSlotIndex = -1;

                return;
            }

            _dragPreview = Instantiate(_inventoryDragPreviewPrefab, _dragCanvas.transform);

            var previewRect = _dragPreview.GetComponent<RectTransform>();
            previewRect.SetAsLastSibling();

            var previewImage = _dragPreview.GetComponent<RawImage>();
            previewImage.texture = slot.Image.texture;

            var count = _dragPreview.transform.Find("Count").GetComponent<TextMeshProUGUI>();
            count.text = slot.Mesh.text;
            count.gameObject.SetActive(slot.Mesh.gameObject.activeSelf);

            UpdateDragPreviewPosition(eventData);
        }

        private void Drag(PointerEventData eventData)
        {
            if (_dragPreview != null)
            {
                UpdateDragPreviewPosition(eventData);
            }
        }

        private void EndDrag(PointerEventData eventData)
        {
            if (_draggedSlotIndex < 0)
            {
                return;
            }

            var sourceSlotIndex = _draggedSlotIndex;
            var targetButton = eventData.pointerCurrentRaycast.gameObject?.GetComponentInParent<ButtonUI>();
            var targetSlot = _inventorySlots.FirstOrDefault(x => x.Button == targetButton);

            ClearDragPreview();

            if (targetSlot == null || targetSlot.Index == sourceSlotIndex)
            {
                return;
            }

            MoveInventorySubscription.Instance.Invoke(
                UserManager.Instance.OwnerClientId.ToString(),
                new MoveInventorySubscriptionEvent
                {
                    CharacterId = InventoryManager.Instance.Dto.CharacterId,
                    SourceSlotIndex = sourceSlotIndex,
                    TargetSlotIndex = targetSlot.Index,
                    ClientToken = UserManager.Instance.Token,
                });
        }

        private void UpdateDragPreviewPosition(PointerEventData eventData)
        {
            if (_dragPreview == null || _dragCanvas == null)
            {
                return;
            }

            var canvasRect = (RectTransform)_dragCanvas.transform;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint))
            {
                ((RectTransform)_dragPreview.transform).anchoredPosition = localPoint;
            }
        }

        private void ClearDragPreview()
        {
            if (_dragPreview != null)
            {
                Destroy(_dragPreview);
            }

            _dragPreview = null;
            _dragCanvas = null;
            _draggedSlotIndex = -1;
        }

        private void SplitStack(int sourceSlotIndex)
        {
            if (!InventoryManager.Instance.CanSplit(sourceSlotIndex))
            {
                return;
            }

            SplitInventorySubscription.Instance.Invoke(UserManager.Instance.OwnerClientId.ToString(), new SplitInventorySubscriptionEvent
            {
                CharacterId = InventoryManager.Instance.Dto.CharacterId,
                SourceSlotIndex = sourceSlotIndex,
                ClientToken = UserManager.Instance.Token,
            });
        }

        public string PrepareDescription(InventoryItemDto item)
        {
            var sb = new StringBuilder(TranslateManager.Instance.GetByKey($"{item.Type}Description"));

            if (item.Type is not InventoryItemEnum.Currency and not InventoryItemEnum.Xp)
            {
                sb.AppendLine();
                sb.AppendLine($"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.Price)}: {MerchantManager.Instance.GetSellPrice(item)}");
            }

            var parameters = item.Type.GetInventoryItemParametersAttribute();

            if (parameters != null)
            {
                if (parameters.MaxHealth > 0)
                {
                    sb.AppendLine($"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.MaxHealth)}: {parameters.MaxHealth}");
                }

                if (parameters.Strength > 0)
                {
                    sb.AppendLine($"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.Strength)}: {parameters.Strength}");
                }

                if (parameters.Dexterity > 0)
                {
                    sb.AppendLine($"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.Dexterity)}: {parameters.Dexterity}");
                }

                if (parameters.Speed > 0)
                {
                    sb.AppendLine($"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.Speed)}: {parameters.Speed}");
                }

                if (parameters.Intellect > 0)
                {
                    sb.AppendLine($"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.Intellect)}: {parameters.Intellect}");
                }

                if (parameters.Armor > 0)
                {
                    sb.AppendLine($"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.Armor)}: {parameters.Armor}");
                }

                if (item.Type.IsAmmo() && parameters.WeaponCategory != WeaponCategoryEnum.None)
                {
                    sb.AppendLine($"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.Required)}: {TranslateManager.Instance.GetByKey(parameters.WeaponCategory.ToString())}");
                }
            }

            return sb.ToString();
        }

        public void UpdateLoot(InventoryItemDto[] items, ulong clientId, string clientToken)
        {
            Loot.SetActive(true);

            foreach (var item in items)
            {
                if (_lootPoolObjects.TryGetValue(item.Type, out var slot))
                {
                    slot.Mesh.text = item.Count.ToString();

                    continue;
                }

                slot = _lootObjectPool.Get();

                slot.Mesh.text = item.Count.ToString();
                slot.Image.color = ColorUI.White;
                slot.Image.texture = Textures[item.Type];
                slot.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{item.Type}Title");
                slot.PreviewDescriptionMesh.text = PrepareDescription(item).ToString();
                slot.Type = item.Type;

                slot.Button.OnRightClick.AddListener(() =>
                {
                    UpdateInventorySubscription.Instance.Invoke(clientId.ToString(), new UpdateInventorySubscriptionEvent
                    {
                        Request = new UpdateCharacterInventoryCommand
                        {
                            CharacterId = 1,
                            Add = new InventoryItemDto[] { item },
                        },
                        ClientToken = clientToken
                    });

                    _lootObjectPool.Release(slot);
                    _lootPoolObjects.Remove(item.Type);

                    if (_lootPoolObjects.Count == 0)
                    {
                        Loot.SetActive(false);
                    }
                });

                var key = slot.GameObject.GetInstanceID().ToString();

                OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
                {
                    slot.Preview.SetActive(true);
                });

                OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
                {
                    slot.Preview.SetActive(false);
                });

                _lootPoolObjects.Add(item.Type, slot);
            }
        }

        private void InitTextures()
        {
            foreach (var type in Enum.GetValues(typeof(InventoryItemEnum)).Cast<InventoryItemEnum>())
            {
                var texture = Resources.Load<Texture>($"Icons/{type}") ?? Resources.Load<Texture>($"Icons/{InventoryItemEnum.None}");

                Debug.Log($"UIManager -> Add texture. Type: {type}");

                Textures.Add(type, texture);
            }
        }

        private IEnumerable<InventorySlot> InstantiateInventorySlots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var slotGameObject = Instantiate(_inventorySlotPrefab, InventoryContent.transform);
                var preview = slotGameObject.transform.Find("Preview").gameObject;
                var slot = new InventorySlot
                {
                    Index = i,
                    GameObject = slotGameObject,
                    Image = slotGameObject.transform.Find("Background").GetComponent<RawImage>(),
                    Mesh = slotGameObject.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
                    HoverUI = slotGameObject.GetComponent<HoverUI>(),
                    Preview = preview,
                    PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                    PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                    Button = slotGameObject.GetComponent<ButtonUI>(),
                    DragTrigger = slotGameObject.AddComponent<EventTrigger>(),
                };

                var key = slotGameObject.GetInstanceID().ToString();

                OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
                {
                    if (_draggedSlotIndex < 0 && slot.Type != InventoryItemEnum.None)
                    {
                        slot.Preview.SetActive(true);
                    }
                });

                OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
                {
                    slot.Preview.SetActive(false);
                });

                AddDragEvent(slot.DragTrigger, EventTriggerType.BeginDrag, (eventData) => BeginDrag(slot, eventData));
                AddDragEvent(slot.DragTrigger, EventTriggerType.Drag, Drag);
                AddDragEvent(slot.DragTrigger, EventTriggerType.EndDrag, EndDrag);

                yield return slot;
            }
        }

        private static void AddDragEvent(EventTrigger trigger, EventTriggerType eventType, Action<PointerEventData> action)
        {
            var entry = new EventTrigger.Entry
            {
                eventID = eventType,
            };

            entry.callback.AddListener((eventData) => action((PointerEventData)eventData));

            trigger.triggers.Add(entry);
        }

        private class InventorySlot
        {
            public int Index { get; set; }

            public GameObject GameObject { get; set; }

            public RawImage Image { get; set; }

            public TextMeshProUGUI Mesh { get; set; }

            public HoverUI HoverUI { get; set; }

            public ButtonUI Button { get; set; }

            public EventTrigger DragTrigger { get; set; }

            public GameObject Preview { get; set; }

            public TextMeshProUGUI PreviewTitleMesh { get; set; }

            public TextMeshProUGUI PreviewDescriptionMesh { get; set; }

            public InventoryItemEnum Type { get; set; }
        }

    }
}
