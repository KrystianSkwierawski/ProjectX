using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Inventory.UI
{
    public static class ItemRarityUI
    {
        public static Color Accent(ItemRarityEnum rarity) => rarity switch
        {
            ItemRarityEnum.Uncommon => ColorUI.ItemRarityUncommon,
            ItemRarityEnum.Rare => ColorUI.ItemRarityRare,
            ItemRarityEnum.Epic => ColorUI.ItemRarityEpic,
            ItemRarityEnum.Legendary => ColorUI.ItemRarityLegendary,
            _ => ColorUI.ItemRarityCommon,
        };

        public static string Label(InventoryItemEnum type)
        {
            var rarity = type.GetRarity();
            var label = TranslateManager.Instance.GetByKey($"ItemRarity{rarity}");

            return $"<color=#{ColorUtility.ToHtmlStringRGB(Accent(rarity))}>{label}</color>";
        }

        public static void Apply(Transform slot, InventoryItemEnum type)
        {
            var backdrop = slot.Find("IconBackdrop")?.GetComponent<Graphic>();

            if (backdrop == null)
            {
                return;
            }

            var rarity = type.GetRarity();

            backdrop.color = rarity == ItemRarityEnum.Common
                ? ColorUI.Black
                : Color.Lerp(ColorUI.Background, Accent(rarity), 0.3f);

            var outline = slot.Find("Outline")?.GetComponent<Graphic>();

            if (outline != null)
            {
                outline.color = rarity == ItemRarityEnum.Common
                    ? ColorUI.Border
                    : Color.Lerp(ColorUI.Border, Accent(rarity), 0.55f);
            }
        }
    }
}
