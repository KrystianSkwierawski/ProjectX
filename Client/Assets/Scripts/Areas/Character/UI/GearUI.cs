using System;
using System.Linq;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Character.UI
{
    public class GearUI : MonoSingleton<GearUI>
    {
        private readonly static InventoryItemEnum[] _templates = new InventoryItemEnum[]
        {
            InventoryItemEnum.HelmetTemplate,
            InventoryItemEnum.ChestTemplate,
            InventoryItemEnum.BootsTemplate,
            InventoryItemEnum.WeaponTemplate
        };

        #region GameObject

        public GameObject GearCanvas { get; private set; }

        public GameObject Gear { get; private set; }

        public GameObject LeftPanel { get; private set; }

        public GearSlot Helmet { get; private set; }

        public GearSlot Chest { get; private set; }

        public GearSlot Boots { get; private set; }

        public GearSlot Weapon { get; private set; }

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

        private void UpdateLeftPanel()
        {
            Wear(Helmet, UserManager.Instance.Character.Helmet);
            Wear(Chest, UserManager.Instance.Character.Chest);
            Wear(Boots, UserManager.Instance.Character.Boots);
            Wear(Weapon, UserManager.Instance.Character.Weapon);
        }

        private void Wear(GearSlot slot, InventoryItemEnum type)
        {
            slot.Image.color = ColorUI.White;
            slot.Image.texture = InventoryUI.Instance.Textures[type];

            if (_templates.Contains(type))
            {
                slot.Button.interactable = false;
                slot.HoverUI.enabled = false;

                return;
            }

            slot.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{type}Title");
            slot.PreviewDescriptionMesh.text = TranslateManager.Instance.GetByKey($"{type}Description");

            // TODO: right button
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
        }

        private void UpdateRightPanel()
        {
            RightPanel.GetComponent<TextMeshProUGUI>().text = string.Format(
                TranslateManager.Instance.GetByKey(TranslateKeyEnum.GearRightPanelDescription),
                UserManager.Instance.Character.Strength,
                UserManager.Instance.Character.Agility,
                UserManager.Instance.Character.Stamina,
                UserManager.Instance.Character.Intelligence,
                UserManager.Instance.Character.Spirit,
                UserManager.Instance.Character.Arrmor
            );
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
