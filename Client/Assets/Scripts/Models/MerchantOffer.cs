using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{

    [Serializable]
    public class MerchantOffer
    {
        public InventoryItemEnum type;

        public int quantity;

        public int price;
    }
}
