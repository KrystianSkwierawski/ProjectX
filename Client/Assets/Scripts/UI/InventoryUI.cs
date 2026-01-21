using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;
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

        public GameObject Loot { get; private set; }

        public GameObject LootContent { get; private set; }

        #endregion

        private InventorySlot[] _inventorySlots;

        public void Start()
        {
            InventoryCanvas = GameObject.Find("InventoryCanvas");
            Inventory = InventoryCanvas.transform.Find("Inventory").gameObject;
            Loot = InventoryCanvas.transform.Find("Loot").gameObject;
            LootContent = Loot.transform.Find("Viewport/Content").gameObject;
            InitTextures();
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
                    continue;
                }

                if (Textures.TryGetValue(item.type, out var texture))
                {
                    slot.Mesh.gameObject.SetActive(true);
                    slot.Mesh.text = item.count.ToString();
                    slot.Image.color = ColorUI.White;
                    slot.Image.texture = texture;
                }
            }
        }

        public void ShowLoot(InventoryItem[] items)
        {
            Loot.SetActive(true);

            foreach (var item in items)
            {
                var slot = Instantiate(_inventorySlotPrefab);
                slot.transform.SetParent(LootContent.transform);

                var image = slot.transform.Find("Background").GetComponent<RawImage>();
                var text = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>();

                if (Textures.TryGetValue(item.type, out var texture))
                {
                    text.gameObject.SetActive(true);
                    text.text = item.count.ToString();
                    image.color = ColorUI.White;
                    image.texture = texture;
                }

                slot.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // TODO: invoke add inventory
                    // TODO: pool
                    // TODO: hide
                    Destroy(slot);
                });
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
                var slot = Instantiate(_inventorySlotPrefab);
                slot.transform.SetParent(Inventory.transform);

                yield return new InventorySlot
                {
                    GameObject = slot,
                    Image = slot.transform.Find("Background").GetComponent<RawImage>(),
                    Mesh = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
                };
            }
        }

        private class InventorySlot
        {
            public GameObject GameObject { get; set; }

            public RawImage Image { get; set; }

            public TextMeshProUGUI Mesh { get; set; }
        }
    }
}
