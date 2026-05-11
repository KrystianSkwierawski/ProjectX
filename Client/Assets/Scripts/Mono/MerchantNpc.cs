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
            return _items;
        }
    }

    private void Start()
    {
    }

    private void Update()
    {
    }
}
