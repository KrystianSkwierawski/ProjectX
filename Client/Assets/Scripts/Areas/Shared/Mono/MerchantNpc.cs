using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Shared.Mono
{
public class MerchantNpc : MonoBehaviour
{
    [SerializeField]
    private InventoryItemDto[] _items;

    public InventoryItemDto[] Items
    {
        get
        {
            return _items.Union(SoldItems).ToArray();
        }
        private set
        {
            _items = value;
        }
    }

    public IList<InventoryItemDto> SoldItems { get; set; } = new List<InventoryItemDto>();
}
}