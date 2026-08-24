using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class MerchantUI : MonoSingleton<MerchantUI>
    {
        #region Prefab

        [SerializeField] private GameObject _inventorySlotPrefab;

        #endregion

        #region GameObject

        public GameObject MerchantCanvas { get; private set; }

        public GameObject Merchant { get; private set; }

        public GameObject Offers { get; private set; }

        public GameObject OffersContent { get; private set; }

        #endregion

        private ObjectPool<ItemPoolObject> _itemPool;
        private IList<ItemPoolObject> _itemObjects = new List<ItemPoolObject>();

        private void Start()
        {
            MerchantCanvas = GameObject.Find("MerchantCanvas");
            Merchant = MerchantCanvas.transform.Find("Merchant").gameObject;
            Offers = Merchant.transform.Find("Offers").gameObject;
            OffersContent = Offers.transform.Find("Viewport/Content").gameObject;

            _itemPool = new ObjectPool<ItemPoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_inventorySlotPrefab, OffersContent.transform);

                    var preview = obj.transform.Find("Preview").gameObject;

                    var itemObject = new ItemPoolObject
                    {
                        GameObject = obj,
                        Image = obj.transform.Find("Background").GetComponent<RawImage>(),
                        Mesh = obj.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
                        Button = obj.GetComponent<ButtonUI>(),
                        HoverUI = obj.GetComponent<HoverUI>(),
                        Preview = preview,
                        PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                        PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                    };

                    itemObject.DragTrigger = InventoryUI.Instance.ConfigureMerchantDrag(
                        itemObject.GameObject,
                        itemObject.Image,
                        itemObject.Mesh,
                        () => itemObject.Item);

                    var key = itemObject.GameObject.GetInstanceID().ToString();

                    OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
                    {
                        if (!InventoryUI.Instance.IsDragging && itemObject.CanPurchase)
                        {
                            itemObject.Preview.SetActive(true);
                        }
                    });

                    OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
                    {
                        itemObject.Preview.SetActive(false);
                    });

                    return itemObject;
                },
                actionOnGet: (ItemPoolObject obj) =>
                {
                    obj.GameObject.SetActive(true);
                    obj.Image.color = ColorUI.White;
                    obj.Mesh.gameObject.SetActive(true);
                    obj.HoverUI.enabled = true;
                    obj.GameObject.transform.SetAsLastSibling();
                    obj.CanPurchase = false;
                    obj.DragTrigger.enabled = false;
                },
                actionOnRelease: (ItemPoolObject obj) =>
                {
                    obj.Item = null;
                    obj.CanPurchase = false;
                    obj.DragTrigger.enabled = false;
                    obj.GameObject.SetActive(false);
                    obj.Mesh.gameObject.SetActive(false);
                    obj.Mesh.text = string.Empty;
                    obj.Mesh.color = ColorUI.White;
                    obj.Image.color = ColorUI.Black;
                    obj.Image.texture = null;
                    obj.HoverUI.enabled = false;
                    obj.Button.OnRightClick.RemoveAllListeners();
                    obj.Preview.SetActive(false);
                }
            );
        }

        // FIXME: fix random order
        public void Show(InventoryItemDto[] items)
        {
            if (items.Length == 0 || Merchant.activeSelf)
            {
                return;
            }

            // FIXME: array
            QuestUI.Instance.Hide();
            CharacterUI.Instance.Hide();
            CraftingUI.Instance.Hide();
            GearUI.Instance.Hide();
            Merchant.SetActive(true);

            ClearOffers();
            AddOffers(items);
        }

        public void ClearOffers()
        {
            foreach (var itemObject in _itemObjects)
            {
                _itemPool.Release(itemObject);
            }

            _itemObjects.Clear();
        }

        public void AddOffers(InventoryItemDto[] items)
        {
            var currency = MerchantManager.Instance.GetCurrency();

            foreach (var item in items)
            {
                var itemObj = _itemPool.Get();

                itemObj.Item = item;
                itemObj.CanPurchase = true;
                itemObj.DragTrigger.enabled = true;
                itemObj.Mesh.text = itemObj.Item.Count.ToString();
                itemObj.Image.texture = InventoryUI.Instance.Textures[itemObj.Item.Type];
                itemObj.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{itemObj.Item.Type}Title");
                itemObj.PreviewDescriptionMesh.text = InventoryUI.Instance.PrepareDescription(itemObj.Item);

                var currencyObj = _itemPool.Get();

                currencyObj.Item = new InventoryItemDto
                {
                    Type = InventoryItemEnum.Currency,
                    Count = MerchantManager.Instance.GetPurchasePrice(item)
                };
                currencyObj.Mesh.text = currencyObj.Item.Count > 1000 ? $"~{currencyObj.Item.Count / 1000}k" : currencyObj.Item.Count.ToString();
                currencyObj.Mesh.color = currency < currencyObj.Item.Count ? ColorUI.Red : ColorUI.White;
                currencyObj.Image.texture = InventoryUI.Instance.Textures[InventoryItemEnum.Currency];
                currencyObj.HoverUI.enabled = false;
                currencyObj.DragTrigger.enabled = false;
                currencyObj.Preview.SetActive(false);

                itemObj.Button.OnRightClick.AddListener(() =>
                {
                    Purchase(itemObj.Item);
                });

                _itemObjects.Add(itemObj);
                _itemObjects.Add(currencyObj);
            }
        }

        public void Purchase(InventoryItemDto item)
        {
            if (!Merchant.activeSelf
                || item == null
                || item.Type is InventoryItemEnum.None or InventoryItemEnum.Currency
                || item.Count <= 0)
            {
                return;
            }

            PurchaseItemSubscribtion.Instance.Invoke(UserManager.Instance.OwnerClientId.ToString(), new PurchaseItemSubscribtionEvent
            {
                item = item,
            });
        }

        public bool IsDropTarget(GameObject target)
        {
            return Merchant.activeSelf
                && target != null
                && (target == Merchant || target.transform.IsChildOf(Merchant.transform));
        }

        public void HidePreviews()
        {
            foreach (var itemObject in _itemObjects)
            {
                itemObject.Preview.SetActive(false);
            }
        }

        public void UpdatePriceValidation()
        {
            if (!Merchant.activeSelf)
            {
                return;
            }

            var currency = MerchantManager.Instance.GetCurrency();

            foreach (var itemObject in _itemObjects.Where(x => x.Item.Type == InventoryItemEnum.Currency))
            {
                itemObject.Mesh.color = currency < itemObject.Item.Count ? ColorUI.Red : ColorUI.White;
            }
        }

        public void Hide()
        {
            if (Merchant.activeSelf)
            {
                InventoryUI.Instance.CancelDrag();

                Merchant.SetActive(false);
            }
        }

        private class ItemPoolObject
        {
            public GameObject GameObject { get; set; }

            public RawImage Image { get; set; }

            public TextMeshProUGUI Mesh { get; set; }

            public HoverUI HoverUI { get; set; }

            public GameObject Preview { get; set; }

            public ButtonUI Button { get; set; }

            public EventTrigger DragTrigger { get; set; }

            public TextMeshProUGUI PreviewTitleMesh { get; set; }

            public TextMeshProUGUI PreviewDescriptionMesh { get; set; }

            public InventoryItemDto Item { get; set; }

            public bool CanPurchase { get; set; }
        }
    }
}
