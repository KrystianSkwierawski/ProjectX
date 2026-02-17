using System;
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
    public class InventoryUI : MonoSingleton<InventoryUI>
    {
        public readonly IDictionary<CharacterInventoryTypeEnum, Texture> Textures = new Dictionary<CharacterInventoryTypeEnum, Texture>();

        #region Prefab

        [SerializeField] private GameObject _inventorySlotPrefab;

        #endregion

        #region GameObject

        public GameObject InventoryCanvas { get; private set; }

        public GameObject Inventory { get; private set; }

        public GameObject InventoryContent { get; private set; }

        public GameObject Loot { get; private set; }

        public GameObject LootContent { get; private set; }

        #endregion

        private ObjectPool<InventorySlot> _lootObjectPool;
        private readonly IDictionary<CharacterInventoryTypeEnum, InventorySlot> _lootPoolObjects = new Dictionary<CharacterInventoryTypeEnum, InventorySlot>();
        private InventorySlot[] _inventorySlots;

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
                       Button = obj.GetComponent<Button>(),
                       Preview = preview,
                       PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                       PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                   };
               },
               actionOnGet: (InventorySlot obj) => obj.GameObject.SetActive(true),
               actionOnRelease: (InventorySlot obj) =>
               {
                   obj.Button.onClick.RemoveAllListeners();

                   obj.GameObject.SetActive(false);
               }
            );
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
                    slot.Mesh.gameObject.SetActive(false);
                    slot.Mesh.text = "0";
                    slot.Image.color = ColorUI.Black;
                    slot.Image.texture = null;
                    slot.Type = CharacterInventoryTypeEnum.None;
                    slot.HoverUI.enabled = false;

                    continue;
                }

                slot.Mesh.gameObject.SetActive(true);
                slot.Mesh.text = item.count.ToString();
                slot.Image.color = ColorUI.White;
                slot.Image.texture = Textures[item.type];
                slot.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{item.type}Title");
                slot.PreviewDescriptionMesh.text = TranslateManager.Instance.GetByKey($"{item.type}Description");
                slot.Type = item.type;
                slot.HoverUI.enabled = true;

                var key = slot.GameObject.GetInstanceID().ToString();

                OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
                {
                    slot.Preview.SetActive(true);
                });

                OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
                {
                    slot.Preview.SetActive(false);
                });
            }
        }

        public void UpdateLoot(InventoryItem[] items, ulong clientId, string clientToken)
        {
            Loot.SetActive(true);

            foreach (var item in items)
            {
                if (_lootPoolObjects.TryGetValue(item.type, out var slot))
                {
                    slot.Mesh.text = item.count.ToString();

                    continue;
                }

                slot = _lootObjectPool.Get();

                slot.Mesh.text = item.count.ToString();
                slot.Image.color = ColorUI.White;
                slot.Image.texture = Textures[item.type];
                slot.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{item.type}Title");
                slot.PreviewDescriptionMesh.text = TranslateManager.Instance.GetByKey($"{item.type}Description");
                slot.Type = item.type;

                slot.Button.onClick.AddListener(() =>
                {
                    AddInventoryItemSubscription.Instance.Invoke(clientId.ToString(), new AddInventoryItemSubscriptionEvent
                    {
                        Item = item,
                        ClientToken = clientToken
                    });

                    _lootObjectPool.Release(slot);
                    _lootPoolObjects.Remove(item.type);

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

                _lootPoolObjects.Add(item.type, slot);
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

        private IEnumerable<InventorySlot> InstantiateInventorySlots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(_inventorySlotPrefab, InventoryContent.transform);

                var preview = slot.transform.Find("Preview").gameObject;

                yield return new InventorySlot
                {
                    GameObject = slot,
                    Image = slot.transform.Find("Background").GetComponent<RawImage>(),
                    Mesh = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
                    HoverUI = slot.GetComponent<HoverUI>(),
                    Preview = preview,
                    PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                    PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                };
            }
        }

        private class InventorySlot
        {
            public GameObject GameObject { get; set; }

            public RawImage Image { get; set; }

            public TextMeshProUGUI Mesh { get; set; }

            public HoverUI HoverUI { get; set; }

            public Button Button { get; set; }

            public GameObject Preview { get; set; }

            public TextMeshProUGUI PreviewTitleMesh { get; set; }

            public TextMeshProUGUI PreviewDescriptionMesh { get; set; }

            public CharacterInventoryTypeEnum Type { get; set; }
        }
    }
}
