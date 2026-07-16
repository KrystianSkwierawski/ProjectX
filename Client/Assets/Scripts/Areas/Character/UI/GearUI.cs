using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Character.UI
{
    public class GearUI : MonoSingleton<GearUI>
    {
        #region GameObject

        public GameObject GearCanvas { get; private set; }

        public GameObject Gear { get; private set; }

        public GameObject LeftPanel { get; private set; }

        public GearSlot Helmet { get; private set; }

        public GearSlot Chest { get; private set; }

        public GearSlot Boots { get; private set; }

        public GearSlot Weapon { get; private set; }

        public GearSlot Ammo { get; private set; }

        public GameObject RightPanel { get; private set; }

        #endregion

        public void Start()
        {
            GearCanvas = GameObject.Find("GearCanvas");
            Gear = GearCanvas.transform.Find("Gear").gameObject;
            LeftPanel = Gear.transform.Find("LeftPanel").gameObject;
            Helmet = GetGearSlot(nameof(Helmet));
            Chest = GetGearSlot(nameof(Chest));
            Boots = GetGearSlot(nameof(Boots));
            Weapon = GetGearSlot(nameof(Weapon));
            Ammo = GetGearSlot(nameof(Ammo));
            RightPanel = Gear.transform.Find("RightPanel").gameObject;
        }

        public void Show()
        {
            if (Gear.activeSelf)
            {
                return;
            }

            // FIXME: array
            CraftingUI.Instance.Hide();
            QuestUI.Instance.Hide();
            MerchantUI.Instance.Hide();
            CharacterUI.Instance.Hide();
            Gear.SetActive(true);

            UpdateLeftPanel();
            UpdateRightPanel();
        }

        public void UpdateLeftPanel()
        {
            var character = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId];

            Wear(Helmet, new InventoryItemDto
            {
                Type = character.HelmetType,
                Count = 1
            });

            Wear(Chest, new InventoryItemDto
            {
                Type = character.ChestType,
                Count = 1
            });

            Wear(Boots, new InventoryItemDto
            {
                Type = character.BootsType,
                Count = 1
            });

            Wear(Weapon, new InventoryItemDto
            {
                Type = character.WeaponType,
                Count = 1
            });

            Wear(Ammo, new InventoryItemDto
            {
                Type = character.AmmoType,
                Count = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId].AmmoCount
            });
        }

        public void Wear(GearSlot slot, InventoryItemDto item)
        {
            slot.Button.OnRightClick.RemoveAllListeners();
            slot.Image.texture = InventoryUI.Instance.Textures[item.Type];

            if (item.Type == InventoryItemEnum.None || item.Type == InventoryItemEnum.HelmetTemplate || item.Type == InventoryItemEnum.ChestTemplate || item.Type == InventoryItemEnum.BootsTemplate || item.Type == InventoryItemEnum.WeaponTemplate || item.Type == InventoryItemEnum.AmmoTemplate)
            {
                slot.Button.interactable = false;
                slot.HoverUI.enabled = false;
                slot.Preview.SetActive(false);
                slot.Mesh.text = "0";
                slot.Mesh.enabled = false;

                return;
            }

            slot.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{item.Type}Title");
            slot.PreviewDescriptionMesh.text = InventoryUI.Instance.PrepareDescription(item);

            if (item.Type.IsAmmo())
            {
                slot.Mesh.text = item.Count > 1000 ? $"~{item.Count / 1000}k" : item.Count.ToString();
                slot.Mesh.enabled = true;
            }
           
            slot.Button.interactable = true;
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

            slot.Button.OnRightClick.AddListener(() =>
            {
                UseItemSubscribtion.Instance.Invoke(UserManager.Instance.OwnerClientId.ToString(), new UseItemSubscribtionEvent
                {
                    Item = item,
                    From = UsableItemFromEnum.Gear,
                });
            });
        }

        public void UpdateRightPanel()
        {
            if (Gear.activeSelf)
            {
                var character = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId];

                RightPanel.GetComponent<TextMeshProUGUI>().text = string.Format(
                    TranslateManager.Instance.GetByKey(TranslateKeyEnum.GearRightPanelDescription),
                    character.MaxHealth,
                    character.Strength,
                    character.Dexterity,
                    character.Speed,
                    character.Intellect,
                    character.Armor
                );
            }
        }

        private GearSlot GetGearSlot(string n)
        {
            var obj = LeftPanel.transform.Find(n).gameObject;

            var preview = obj.transform.Find("Preview").gameObject;

            return new GearSlot
            {
                GameObject = obj,
                Image = obj.transform.Find("Background").GetComponent<RawImage>(),
                Mesh = obj.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
                HoverUI = obj.GetComponent<HoverUI>(),
                Button = obj.GetComponent<ButtonUI>(),
                Preview = preview,
                PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
            };
        }

        public void Hide()
        {
            if (Gear.activeSelf)
            {
                Gear.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (Gear.activeSelf)
            {
                Hide();

                return;
            }

            Show();
        }
    }

    public class GearSlot
    {
        public GameObject GameObject { get; set; }

        public RawImage Image { get; set; }

        public TextMeshProUGUI Mesh { get; set; }

        public ButtonUI Button { get; set; }

        public HoverUI HoverUI { get; set; }

        public GameObject Preview { get; set; }

        public TextMeshProUGUI PreviewTitleMesh { get; set; }

        public TextMeshProUGUI PreviewDescriptionMesh { get; set; }
    }
}
