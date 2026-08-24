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
            { InventoryItemEnum.Can, 10 },
            { InventoryItemEnum.Currency, 1 },

            { InventoryItemEnum.Fish, 20 },
            { InventoryItemEnum.CookedFish, 50 },
            { InventoryItemEnum.Rice, 15 },
            { InventoryItemEnum.Sushi, 80 },

            { InventoryItemEnum.PurpleOre, 100 },
            { InventoryItemEnum.WhiteOre, 60 },
            { InventoryItemEnum.CopperOre, 40 },
            { InventoryItemEnum.BlackOre, 120 },

            { InventoryItemEnum.PurpleBar, 300 },
            { InventoryItemEnum.WhiteBar, 180 },
            { InventoryItemEnum.CopperBar, 120 },
            { InventoryItemEnum.BlackBar, 360 },

            { InventoryItemEnum.Wood, 8 },
            { InventoryItemEnum.Chamomile, 25 },
            { InventoryItemEnum.HealthPotion, 10 },
            { InventoryItemEnum.StrengthPotion, 25 },
            { InventoryItemEnum.SpeedPotion, 25 },

            { InventoryItemEnum.IronHelmet, 20 },
            { InventoryItemEnum.IronChest, 30 },
            { InventoryItemEnum.IronBoots, 10 },

            { InventoryItemEnum.IronSword, 40 },
            { InventoryItemEnum.IronWand, 40 },
            { InventoryItemEnum.IronBow, 40 },

            { InventoryItemEnum.AmmoArrow1, 5 },
            { InventoryItemEnum.AmmoArrow2, 10 },
            { InventoryItemEnum.AmmoArrow3, 15 },

            { InventoryItemEnum.AmmoRune1, 5 },
            { InventoryItemEnum.AmmoRune2, 10 },
            { InventoryItemEnum.AmmoRune3, 15 },

            { InventoryItemEnum.AmmoFeather1, 5 },
            { InventoryItemEnum.AmmoFeather2, 10 },
            { InventoryItemEnum.AmmoFeather3, 15 },

            { InventoryItemEnum.AmmoOil1, 5 },
            { InventoryItemEnum.AmmoOil2, 10 },
            { InventoryItemEnum.AmmoOil3, 15 },
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
                return (_prices[item.Type] * item.Count) / 2;
            }

            return (_prices.Min(x => x.Value) * item.Count) / 2;
        }

        public int GetCurrency(InventoryItemEnum type = InventoryItemEnum.Currency) => InventoryManager.Instance.Dto.Inventory.Items
            .Where(x => x.Type == type)
            .Select(x => x.Count)
            .Sum();
    }
}
