using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Extensions;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Assets.Scripts.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Merchant : NetworkBehaviour
{
    private const float _npcMaxDistance = 5f;
    private MerchantNpc _merchantNpc;

    private void Start()
    {
        if (IsOwner)
        {
            PurchaseItemSubscribtion.Instance.Subscribe(OwnerClientId.ToString(), (e) =>
            {
                if (!MerchantManager.Instance.HasCurrency(e.item))
                {
                    Debug.Log("Not enough currency");

                    return;
                }

                var itemToRemove = _merchantNpc.SoldItems
                    .Where(x => x.Type == e.item.Type)
                    .Where(x => x.Count == e.item.Count)
                    .FirstOrDefault();

                if (itemToRemove != null)
                {
                    _merchantNpc.SoldItems.Remove(itemToRemove);

                    // TODO: update one item and update prices?
                    MerchantUI.Instance.ClearOffers();
                    MerchantUI.Instance.AddOffers(_merchantNpc.Items);
                }

                PurchaseItemServerRpc(e.item, UserManager.Instance.Token);
            });

            SellItemSubscribtion.Instance.Subscribe(OwnerClientId.ToString(), (e) =>
            {
                _merchantNpc.SoldItems.Add(e.item);

                // TODO: update one item and update prices?
                MerchantUI.Instance.ClearOffers();
                MerchantUI.Instance.AddOffers(_merchantNpc.Items);

                SellItemServerRpc(e.item, UserManager.Instance.Token);
            });
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            // TODO: cancel button
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _merchantNpc = null;
                MerchantUI.Instance.Hide();
            }

            CheckNpcClicked();
        }
    }

    private void CheckNpcClicked()
    {
        var mouse = Mouse.current;

        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

        var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "MerchantNpc";

        if (!hover)
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        if (hit.transform.IsFarToTarget(transform.gameObject, _npcMaxDistance))
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        _merchantNpc = hit.transform.GetComponent<MerchantNpc>();

        CursorUI.Instance.ShowPointer();

        if (mouse.rightButton.wasPressedThisFrame)
        {
            MerchantUI.Instance.Show(_merchantNpc.Items);
        }
    }

    [ServerRpc]
    private void PurchaseItemServerRpc(InventoryItemDto item, string clientToken)
    {
        // TODO: validate npc position
        // TODO: validate currency
        UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
        {
            Request = new UpdateCharacterInventoryCommand
            {
                CharacterId = 1,
                Add = new[] { item },
                Remove = new[] { new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = MerchantManager.Instance.GetPurchasePrice(item) } },
            },
            ClientToken = clientToken
        });
    }

    [ServerRpc]
    private void SellItemServerRpc(InventoryItemDto item, string clientToken)
    {
        // TODO: validate npc position
        // TODO: validate item ownership
        UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
        {
            Request = new UpdateCharacterInventoryCommand
            {
                CharacterId = 1,
                Add = new[] { new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = MerchantManager.Instance.GetSellPrice(item) } },
                Remove = new[] { item },
            },
            ClientToken = clientToken
        });
    }
}
