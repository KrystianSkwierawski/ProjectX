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
            { InventoryItemEnum.HealthPotion, 150 }
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
