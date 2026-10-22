using System;
using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Shared.Attributes
{
    // Presentation metadata only; does not change stats, prices or drop rates.
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ItemRarityAttribute : Attribute
    {
        public ItemRarityEnum Rarity { get; }

        public ItemRarityAttribute(ItemRarityEnum rarity)
        {
            Rarity = rarity;
        }
    }
}
