using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;

namespace Assets.Scripts.Shared
{
    public class MerchantManager : Singleton<MerchantManager>
    {
        private readonly IDictionary<InventoryItemEnum, int> _prices = new Dictionary<InventoryItemEnum, int>
        {
            { InventoryItemEnum.Can, 100 },
            { InventoryItemEnum.Fish, 200 },
            { InventoryItemEnum.CookedFish, 2000 },
            { InventoryItemEnum.Rice, 100000 }
            // TODO: add more items and balance prices
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
