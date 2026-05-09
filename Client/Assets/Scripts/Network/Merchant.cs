using System.Collections.Generic;
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
    private IDictionary<MerchantTypeEnum, UpdateCharacterInventoryCommand[]> _merchants = new Dictionary<MerchantTypeEnum, UpdateCharacterInventoryCommand[]>
    {
        {
            MerchantTypeEnum.Common, new[]
            {
                new UpdateCharacterInventoryCommand
                {
                    Add = new[] { new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 1 } },
                    Remove = new[] { new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 100 } }
                },
                new UpdateCharacterInventoryCommand
                {
                    Add = new[] { new InventoryItemDto { Type = InventoryItemEnum.Fish, Count = 1 } },
                    Remove = new[] { new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 200 } }
                },
                new UpdateCharacterInventoryCommand
                {
                    Add = new[] { new InventoryItemDto { Type = InventoryItemEnum.Can, Count = 1 } },
                    Remove = new[] { new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 2000 } }
                },
                new UpdateCharacterInventoryCommand
                {
                    Add = new[] { new InventoryItemDto { Type = InventoryItemEnum.CopperOre, Count = 1 } },
                    Remove = new[] { new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 100000 } }
                },
            }
        }
    };

    private const float _npcMaxDistance = 5f;
    private MerchantNpc _merchantNpc;

    private void Start()
    {
        if (IsOwner)
        {
            PurchaseItemSubscribtion.Instance.Subscribe(OwnerClientId.ToString(), (e) =>
            {
                if (InventoryManager.Instance.Currency < e.Offer.Remove[0].Count)
                {
                    Debug.Log("Not enough currency");

                    return;
                }

                PurchaseItemServerRpc(e.Offer, UserManager.Instance.Token);
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
            MerchantUI.Instance.Show(_merchants[_merchantNpc.Type]);
        }
    }

    [ServerRpc]
    private void PurchaseItemServerRpc(UpdateCharacterInventoryCommand offer, string clientToken)
    {
        // TODO: validate InventoryManager.Instance.Currency
        // TODO: validate merchant offer/type
        UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
        {
            Request = offer,
            ClientToken = clientToken
        });
    }
}
