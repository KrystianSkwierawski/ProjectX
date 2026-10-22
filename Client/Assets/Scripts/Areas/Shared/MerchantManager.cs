using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Inventory;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public class MerchantManager : Singleton<MerchantManager>
    {
        private readonly IDictionary<InventoryItemEnum, int> _prices = new Dictionary<InventoryItemEnum, int>
        {
            { InventoryItemEnum.Can, 1 },
            { InventoryItemEnum.Currency, 1 },

            { InventoryItemEnum.Fish, 1 },
            { InventoryItemEnum.CookedFish, 1 },
            { InventoryItemEnum.Rice, 1 },
            { InventoryItemEnum.Sushi, 1 },

            { InventoryItemEnum.PurpleOre, 1 },
            { InventoryItemEnum.WhiteOre, 1 },
            { InventoryItemEnum.CopperOre, 1 },
            { InventoryItemEnum.BlackOre, 1 },

            { InventoryItemEnum.PurpleBar, 1 },
            { InventoryItemEnum.WhiteBar, 1 },
            { InventoryItemEnum.CopperBar, 1 },
            { InventoryItemEnum.BlackBar, 1 },

            { InventoryItemEnum.Wood, 1 },
            { InventoryItemEnum.Chamomile, 1 },
            { InventoryItemEnum.ChamomileSeed, 1 },
            { InventoryItemEnum.Strawberry, 1 },
            { InventoryItemEnum.StrawberrySeed, 1 },
            { InventoryItemEnum.Mint, 1 },
            { InventoryItemEnum.MintSeed, 1 },
            { InventoryItemEnum.Lavender, 1 },
            { InventoryItemEnum.LavenderSeed, 1 },
            { InventoryItemEnum.Calendula, 1 },
            { InventoryItemEnum.CalendulaSeed, 1 },
            { InventoryItemEnum.Raspberry, 1 },
            { InventoryItemEnum.RaspberrySeed, 1 },

            { InventoryItemEnum.HealthPotion, 1 },
            { InventoryItemEnum.StrengthPotion, 1 },
            { InventoryItemEnum.SpeedPotion, 1 },

            { InventoryItemEnum.IronHelmet, 1 },
            { InventoryItemEnum.IronChest, 1 },
            { InventoryItemEnum.IronBoots, 1 },

            { InventoryItemEnum.IronSword, 1 },
            { InventoryItemEnum.IronWand, 1 },
            { InventoryItemEnum.IronBow, 1 },

            { InventoryItemEnum.AmmoArrow1, 1 },
            { InventoryItemEnum.AmmoArrow2, 1 },
            { InventoryItemEnum.AmmoArrow3, 1 },

            { InventoryItemEnum.AmmoRune1, 1 },
            { InventoryItemEnum.AmmoRune2, 1 },
            { InventoryItemEnum.AmmoRune3, 1 },

            { InventoryItemEnum.AmmoFeather1, 1 },
            { InventoryItemEnum.AmmoFeather2, 1 },
            { InventoryItemEnum.AmmoFeather3, 1 },

            { InventoryItemEnum.AmmoOil1, 1 },
            { InventoryItemEnum.AmmoOil2, 1 },
            { InventoryItemEnum.AmmoOil3, 1 },
        };

        public bool HasCurrency(InventoryItemDto item) => HasCurrency(GetPurchasePrice(item));

        public bool HasCurrency(int price) => GetCurrency() >= price;

        public int GetPurchasePrice(InventoryItemDto item)
        {
            if (_prices.TryGetValue(item.Type, out var price))
            {
                return _prices[item.Type] * item.Count;
            }

            return _prices.Max(x => x.Value) * item.Count;
        }

        public int GetSellPrice(InventoryItemDto item)
        {
            if (_prices.TryGetValue(item.Type, out var price))
            {
                return System.Math.Max(1, (_prices[item.Type] * item.Count) / 2);
            }

            return System.Math.Max(1, (_prices.Min(x => x.Value) * item.Count) / 2);
        }

        public int GetCurrency(InventoryItemEnum type = InventoryItemEnum.Currency) => InventoryManager.Instance.Dto.Inventory.Items
            .Where(x => x.Type == type)
            .Select(x => x.Count)
            .Sum();
    }
}
