using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
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
        private GearSlot _draggedGearSlot;
        private InventorySlot _draggedLootSlot;
        private InventoryItemDto _draggedItem;
        private bool _isDraggingMerchantItem;
        private RawImage _dragSourceImage;
        private bool _dragSourceImageWasEnabled;
        private Texture _dragSourceTexture;
        private Color _dragSourceColor;
        private TextMeshProUGUI _dragSourceMesh;
        private bool _dragSourceMeshWasEnabled;

        public bool IsDragging => _draggedSlotIndex >= 0
            || _draggedGearSlot != null
            || _draggedLootSlot != null
            || _isDraggingMerchantItem;

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
                   var preview = obj.transform.Find("Preview").gameObject;
                   var slot = new InventorySlot
                   {
                       Index = -1,
                       GameObject = obj,
                       Image = obj.transform.Find("Background").GetComponent<RawImage>(),
                       Mesh = mesh,
                       HoverUI = obj.GetComponent<HoverUI>(),
                       Button = obj.GetComponent<ButtonUI>(),
                       DragTrigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>(),
                       Preview = preview,
                       PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                       PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                   };

                   var key = obj.GetInstanceID().ToString();

                   OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
                   {
                       if (!IsDragging && slot.Item != null)
                       {
                           slot.Preview.SetActive(true);
                       }
                   });

                   OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
                   {
                       slot.Preview.SetActive(false);
                   });

                   AddDragEvent(slot.DragTrigger, EventTriggerType.BeginDrag, (eventData) => BeginLootDrag(slot, eventData));
                   AddDragEvent(slot.DragTrigger, EventTriggerType.Drag, Drag);
                   AddDragEvent(slot.DragTrigger, EventTriggerType.EndDrag, EndLootDrag);

                   return slot;
               },
               actionOnGet: (InventorySlot obj) =>
               {
                   obj.GameObject.SetActive(true);
                   obj.Image.enabled = true;
                   obj.Mesh.enabled = true;
                   obj.Mesh.gameObject.SetActive(true);
                   obj.HoverUI.enabled = true;
                   obj.DragTrigger.enabled = false;
               },
               actionOnRelease: (InventorySlot obj) =>
               {
                   obj.Item = null;
                   obj.Type = InventoryItemEnum.None;
                   obj.LootClientId = 0;
                   obj.DragTrigger.enabled = false;
                   obj.HoverUI.enabled = false;
                   obj.Button.OnRightClick.RemoveAllListeners();
                   obj.Preview.SetActive(false);
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

                var slotIndex = slot.Index;

                slot.Button.OnRightClick.AddListener(() =>
                {
                    if (Keyboard.current.altKey.isPressed)
                    {
                        SplitStack(slotIndex);

                        return;
                    }

                    UseItem(CreateUsableItem(item), UsableItemFromEnum.Inventory);
                });
            }
        }

        private void BeginDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left
                || slot.Type == InventoryItemEnum.None
                || slot.Index < 0
                || slot.Index >= InventoryManager.Instance.Dto.Inventory.Items.Count)
            {
                return;
            }

            var item = InventoryManager.Instance.Dto.Inventory.Items[slot.Index];

            ClearDragPreview();
            _draggedSlotIndex = slot.Index;
            _draggedItem = CreateUsableItem(item);

            HidePreviews();
            GearUI.Instance.HidePreviews();
            MerchantUI.Instance.HidePreviews();

            if (!CreateDragPreview(
                slot.GameObject,
                slot.Image.texture,
                slot.Mesh.text,
                slot.Mesh.gameObject.activeSelf,
                eventData))
            {
                ClearDragPreview();

                return;
            }

            SetDragSourcePlaceholder(slot.Image, slot.Mesh, null, ColorUI.Black);
        }

        public void ConfigureGearDrag(GearSlot slot)
        {
            slot.DragTrigger = slot.GameObject.GetComponent<EventTrigger>() ?? slot.GameObject.AddComponent<EventTrigger>();

            AddDragEvent(slot.DragTrigger, EventTriggerType.BeginDrag, (eventData) => BeginGearDrag(slot, eventData));
            AddDragEvent(slot.DragTrigger, EventTriggerType.Drag, Drag);
            AddDragEvent(slot.DragTrigger, EventTriggerType.EndDrag, EndGearDrag);
        }

        public EventTrigger ConfigureMerchantDrag(
            GameObject source,
            RawImage image,
            TextMeshProUGUI mesh,
            Func<InventoryItemDto> getItem)
        {
            var trigger = source.GetComponent<EventTrigger>() ?? source.AddComponent<EventTrigger>();

            AddDragEvent(trigger, EventTriggerType.BeginDrag, (eventData) => BeginMerchantDrag(source, image, mesh, getItem(), eventData));
            AddDragEvent(trigger, EventTriggerType.Drag, Drag);
            AddDragEvent(trigger, EventTriggerType.EndDrag, EndMerchantDrag);

            return trigger;
        }

        private void BeginGearDrag(GearSlot slot, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !slot.HasItem)
            {
                return;
            }

            ClearDragPreview();
            _draggedGearSlot = slot;
            _draggedItem = CreateUsableItem(slot.Item);

            HidePreviews();
            GearUI.Instance.HidePreviews();
            MerchantUI.Instance.HidePreviews();

            if (!CreateDragPreview(
                slot.GameObject,
                slot.Image.texture,
                slot.Mesh.text,
                slot.Mesh.enabled && slot.Mesh.gameObject.activeSelf,
                eventData))
            {
                ClearDragPreview();

                return;
            }

            SetDragSourcePlaceholder(
                slot.Image,
                slot.Mesh,
                Textures[slot.TemplateType],
                ColorUI.White);
        }

        private void BeginMerchantDrag(
            GameObject source,
            RawImage image,
            TextMeshProUGUI mesh,
            InventoryItemDto item,
            PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left
                || item == null
                || item.Type is InventoryItemEnum.None or InventoryItemEnum.Currency
                || item.Count <= 0)
            {
                return;
            }

            ClearDragPreview();
            _isDraggingMerchantItem = true;
            _draggedItem = CloneItem(item);

            HidePreviews();
            GearUI.Instance.HidePreviews();
            MerchantUI.Instance.HidePreviews();

            if (!CreateDragPreview(
                source,
                image.texture,
                mesh.text,
                mesh.gameObject.activeSelf,
                eventData))
            {
                ClearDragPreview();

                return;
            }

            SetDragSourcePlaceholder(image, mesh, null, ColorUI.Black);
        }

        private void BeginLootDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left
                || slot.Item == null
                || slot.Item.Type == InventoryItemEnum.None
                || slot.Item.Count <= 0)
            {
                return;
            }

            ClearDragPreview();
            _draggedLootSlot = slot;
            _draggedItem = CloneItem(slot.Item);

            HidePreviews();
            GearUI.Instance.HidePreviews();
            MerchantUI.Instance.HidePreviews();

            if (!CreateDragPreview(
                slot.GameObject,
                slot.Image.texture,
                slot.Mesh.text,
                slot.Mesh.enabled && slot.Mesh.gameObject.activeSelf,
                eventData))
            {
                ClearDragPreview();

                return;
            }

            SetDragSourcePlaceholder(slot.Image, slot.Mesh, null, ColorUI.Black);
        }

        private bool CreateDragPreview(
            GameObject source,
            Texture texture,
            string countText,
            bool showCount,
            PointerEventData eventData)
        {
            _dragCanvas = source.GetComponentInParent<Canvas>();

            if (_dragCanvas == null || _inventoryDragPreviewPrefab == null)
            {
                return false;
            }

            _dragPreview = Instantiate(_inventoryDragPreviewPrefab, _dragCanvas.transform);

            var previewRect = _dragPreview.GetComponent<RectTransform>();
            previewRect.SetAsLastSibling();

            var previewImage = _dragPreview.GetComponent<RawImage>();
            previewImage.texture = texture;

            var count = _dragPreview.transform.Find("Count").GetComponent<TextMeshProUGUI>();
            count.text = countText;
            count.gameObject.SetActive(showCount);

            UpdateDragPreviewPosition(eventData);

            return true;
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
            var item = _draggedItem;
            var targetGameObject = eventData.pointerCurrentRaycast.gameObject;
            var targetButton = targetGameObject?.GetComponentInParent<ButtonUI>();
            var targetInventorySlot = _inventorySlots.FirstOrDefault(x => x.Button == targetButton);
            var targetGearSlot = GearUI.Instance.GetSlot(targetButton);
            var targetMerchant = MerchantUI.Instance.IsDropTarget(targetGameObject);

            ClearDragPreview();

            if (targetInventorySlot != null)
            {
                if (targetInventorySlot.Index == sourceSlotIndex)
                {
                    return;
                }

                MoveInventorySubscription.Instance.Invoke(
                    UserManager.Instance.OwnerClientId.ToString(),
                    new MoveInventorySubscriptionEvent
                    {
                        SourceSlotIndex = sourceSlotIndex,
                        TargetSlotIndex = targetInventorySlot.Index,
                    });

                return;
            }

            if (targetGearSlot != null && item != null && item.Type.IsGear())
            {
                UseItem(item, UsableItemFromEnum.Inventory);

                return;
            }

            if (targetMerchant && item != null && item.Type != InventoryItemEnum.Currency)
            {
                UseItem(item, UsableItemFromEnum.Inventory);
            }
        }

        private void EndGearDrag(PointerEventData eventData)
        {
            if (_draggedGearSlot == null)
            {
                return;
            }

            var item = _draggedItem;
            var targetButton = eventData.pointerCurrentRaycast.gameObject?.GetComponentInParent<ButtonUI>();
            var targetInventorySlot = _inventorySlots.FirstOrDefault(x => x.Button == targetButton);

            ClearDragPreview();

            if (targetInventorySlot != null && item != null)
            {
                UseItem(item, UsableItemFromEnum.Gear);
            }
        }

        private void EndMerchantDrag(PointerEventData eventData)
        {
            if (!_isDraggingMerchantItem)
            {
                return;
            }

            var item = _draggedItem;
            var targetButton = eventData.pointerCurrentRaycast.gameObject?.GetComponentInParent<ButtonUI>();
            var targetInventorySlot = _inventorySlots?.FirstOrDefault(x => x.Button == targetButton);

            ClearDragPreview();

            if (targetInventorySlot != null && item != null)
            {
                MerchantUI.Instance.Purchase(item);
            }
        }

        private void EndLootDrag(PointerEventData eventData)
        {
            if (_draggedLootSlot == null)
            {
                return;
            }

            var sourceSlot = _draggedLootSlot;
            var targetButton = eventData.pointerCurrentRaycast.gameObject?.GetComponentInParent<ButtonUI>();
            var targetInventorySlot = _inventorySlots?.FirstOrDefault(x => x.Button == targetButton);

            ClearDragPreview();

            if (targetInventorySlot != null)
            {
                TakeLoot(sourceSlot);
            }
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
            RestoreDragSource();

            if (_dragPreview != null)
            {
                Destroy(_dragPreview);
            }

            _dragPreview = null;
            _dragCanvas = null;
            _draggedSlotIndex = -1;
            _draggedGearSlot = null;
            _draggedLootSlot = null;
            _draggedItem = null;
            _isDraggingMerchantItem = false;
        }

        private void SetDragSourcePlaceholder(
            RawImage image,
            TextMeshProUGUI mesh,
            Texture placeholderTexture,
            Color placeholderColor)
        {
            _dragSourceImage = image;
            _dragSourceImageWasEnabled = image != null && image.enabled;
            _dragSourceTexture = image?.texture;
            _dragSourceColor = image != null ? image.color : Color.white;
            _dragSourceMesh = mesh;
            _dragSourceMeshWasEnabled = mesh != null && mesh.enabled;

            if (_dragSourceImage != null)
            {
                _dragSourceImage.enabled = true;
                _dragSourceImage.texture = placeholderTexture;
                _dragSourceImage.color = placeholderColor;
            }

            if (_dragSourceMesh != null)
            {
                _dragSourceMesh.enabled = false;
            }
        }

        private void RestoreDragSource()
        {
            if (_dragSourceImage != null)
            {
                _dragSourceImage.texture = _dragSourceTexture;
                _dragSourceImage.color = _dragSourceColor;
                _dragSourceImage.enabled = _dragSourceImageWasEnabled;
            }

            if (_dragSourceMesh != null)
            {
                _dragSourceMesh.enabled = _dragSourceMeshWasEnabled;
            }

            _dragSourceImage = null;
            _dragSourceTexture = null;
            _dragSourceMesh = null;
        }

        public void CancelDrag()
        {
            ClearDragPreview();
        }

        private void HidePreviews()
        {
            if (_inventorySlots != null)
            {
                foreach (var inventorySlot in _inventorySlots)
                {
                    inventorySlot.Preview.SetActive(false);
                }
            }

            foreach (var lootSlot in _lootPoolObjects.Values)
            {
                lootSlot.Preview.SetActive(false);
            }
        }

        private static InventoryItemDto CreateUsableItem(InventoryItemDto item)
        {
            return new InventoryItemDto
            {
                Type = item.Type,
                Count = item.Type.IsAmmo() ? item.Count : 1,
            };
        }

        private static InventoryItemDto CloneItem(InventoryItemDto item)
        {
            return new InventoryItemDto
            {
                Type = item.Type,
                Count = item.Count,
            };
        }

        private static void UseItem(InventoryItemDto item, UsableItemFromEnum from)
        {
            UseItemSubscribtion.Instance.Invoke(UserManager.Instance.OwnerClientId.ToString(), new UseItemSubscribtionEvent
            {
                Item = item,
                From = from,
            });
        }

        private void SplitStack(int sourceSlotIndex)
        {
            if (!InventoryManager.Instance.CanSplit(sourceSlotIndex))
            {
                return;
            }

            SplitInventorySubscription.Instance.Invoke(UserManager.Instance.OwnerClientId.ToString(), new SplitInventorySubscriptionEvent
            {
                SourceSlotIndex = sourceSlotIndex,
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

        public void UpdateLoot(InventoryItemDto[] items, ulong clientId)
        {
            Loot.SetActive(true);

            foreach (var item in items)
            {
                if (!_lootPoolObjects.TryGetValue(item.Type, out var slot))
                {
                    slot = _lootObjectPool.Get();
                    var lootSlot = slot;
                    slot.Button.OnRightClick.AddListener(() => TakeLoot(lootSlot));
                    _lootPoolObjects.Add(item.Type, slot);
                }

                slot.Item = CloneItem(item);
                slot.Type = item.Type;
                slot.LootClientId = clientId;
                slot.Mesh.text = item.Count > 1000 ? $"~{item.Count / 1000}k" : item.Count.ToString();
                slot.Image.color = ColorUI.White;
                slot.Image.texture = Textures[item.Type];
                slot.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{item.Type}Title");
                slot.PreviewDescriptionMesh.text = PrepareDescription(item);
                slot.HoverUI.enabled = true;
                slot.DragTrigger.enabled = true;
            }
        }

        private void TakeLoot(InventorySlot slot)
        {
            if (slot?.Item == null
                || !_lootPoolObjects.TryGetValue(slot.Type, out var activeSlot)
                || activeSlot != slot)
            {
                return;
            }

            var item = CloneItem(slot.Item);
            var type = slot.Type;
            var request = new UpdateCharacterInventoryCommand
            {
                Add = new[] { item },
            };

            if (!InventoryManager.Instance.CanApply(request))
            {
                LogUI.Instance.ShowAsync(
                    TranslateManager.Instance.GetByKey(TranslateKeyEnum.InventoryFull),
                    color: ColorUI.Red)
                    .Forget();

                return;
            }

            UpdateInventorySubscription.Instance.Invoke(
                slot.LootClientId.ToString(),
                new UpdateInventorySubscriptionEvent
                {
                    Request = request,
                });

            _lootPoolObjects.Remove(type);

            _lootObjectPool.Release(slot);

            if (_lootPoolObjects.Count == 0)
            {
                Loot.SetActive(false);
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
                    if (!IsDragging && slot.Type != InventoryItemEnum.None)
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

            public InventoryItemDto Item { get; set; }

            public ulong LootClientId { get; set; }
        }

    }
}
