using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Models;
using UnityEngine;

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
