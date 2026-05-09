using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Assets.Scripts.UI
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

                    return new ItemPoolObject
                    {
                        GameObject = obj,
                        Image = obj.transform.Find("Background").GetComponent<RawImage>(),
                        Mesh = obj.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
                        Button = obj.GetComponent<Button>(),
                        HoverUI = obj.GetComponent<HoverUI>(),
                        Preview = preview,
                        PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                        PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                    };
                },
                actionOnGet: (ItemPoolObject obj) =>
                {
                    obj.GameObject.SetActive(true);
                    obj.Image.color = ColorUI.White;
                    obj.Mesh.gameObject.SetActive(true);
                    obj.HoverUI.enabled = true;
                },
                actionOnRelease: (ItemPoolObject obj) =>
                {
                    obj.Item = null;
                    obj.GameObject.SetActive(false);
                    obj.Mesh.gameObject.SetActive(false);
                    obj.Mesh.text = string.Empty;
                    obj.Mesh.color = ColorUI.White;
                    obj.Image.color = ColorUI.Black;
                    obj.Image.texture = null;
                    obj.HoverUI.enabled = false;
                    obj.Button.onClick.RemoveAllListeners();
                }
            );
        }

        public void Show(UpdateCharacterInventoryCommand[] offers)
        {
            if (offers.Length == 0 || Merchant.activeSelf)
            {
                return;
            }

            // FIXME: array
            QuestUI.Instance.Hide();
            CharacterUI.Instance.Hide();
            CraftingUI.Instance.Hide();
            Merchant.SetActive(true);

            ClearOffers();
            AddOffers(offers);
        }

        public void ClearOffers()
        {
            foreach (var itemObject in _itemObjects)
            {
                _itemPool.Release(itemObject);
            }

            _itemObjects.Clear();
        }

        public void AddOffers(UpdateCharacterInventoryCommand[] offers)
        {
            var currency = InventoryManager.Instance.Currency;

            foreach (var offer in offers)
            {
                var itemObj = _itemPool.Get();

                itemObj.Item = offer.Add[0];
                itemObj.Mesh.text = itemObj.Item.Count.ToString();
                itemObj.Image.texture = InventoryUI.Instance.Textures[itemObj.Item.Type];
                itemObj.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{itemObj.Item.Type}Title");
                itemObj.PreviewDescriptionMesh.text = TranslateManager.Instance.GetByKey($"{itemObj.Item.Type}Description");

                var currencyObj = _itemPool.Get();

                currencyObj.Item = offer.Remove[0];
                currencyObj.Mesh.text = currencyObj.Item.Count > 1000 ? $"~{currencyObj.Item.Count / 1000}k" : currencyObj.Item.Count.ToString();
                currencyObj.Mesh.color = currency < currencyObj.Item.Count ? ColorUI.Red : ColorUI.White;
                currencyObj.Image.texture = InventoryUI.Instance.Textures[InventoryItemEnum.Currency];

                itemObj.Button.onClick.AddListener(() =>
                {
                    PurchaseItemSubscribtion.Instance.Invoke(UserManager.Instance.OwnerClientId.ToString(), new PurchaseItemSubscribtionEvent
                    {
                        Offer = offer
                    });
                });

                var key = itemObj.GameObject.GetInstanceID().ToString();

                OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
                {
                    itemObj.Preview.SetActive(true);
                });

                OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
                {
                    itemObj.Preview.SetActive(false);
                });

                _itemObjects.Add(itemObj);
                _itemObjects.Add(currencyObj);
            }
        }

        public void UpdatePriceValidation()
        {
            var currency = InventoryManager.Instance.Currency;

            foreach (var itemObject in _itemObjects.Where(x => x.Item.Type == InventoryItemEnum.Currency))
            {
                itemObject.Mesh.color = currency < itemObject.Item.Count ? ColorUI.Red : ColorUI.White;
            }
        }

        public void Hide()
        {
            if (Merchant.activeSelf)
            {
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

            public Button Button { get; set; }

            public TextMeshProUGUI PreviewTitleMesh { get; set; }

            public TextMeshProUGUI PreviewDescriptionMesh { get; set; }

            public InventoryItemDto Item { get; set; }
        }
    }
}
