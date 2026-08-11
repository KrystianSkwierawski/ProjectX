using System.Linq;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Merchant : NetworkBehaviour
    {
        private StarterAssetsInputs _input;
        private const float _npcMaxDistance = 5f;
        private MerchantNpc _merchantNpc;

        private void Start()
        {
            if (IsOwner)
            {
                _input = GetComponent<StarterAssetsInputs>();


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

                    PurchaseItemServerRpc(e.item);
                });

                UseItemSubscribtion.Instance.Subscribe(OwnerClientId.ToString(), (e) =>
                {
                    if (e.Item.Type != InventoryItemEnum.Currency && MerchantUI.Instance.Merchant.activeSelf)
                    {
                        _merchantNpc.SoldItems.Add(e.Item);

                        // TODO: update one item and update prices?
                        MerchantUI.Instance.ClearOffers();
                        MerchantUI.Instance.AddOffers(_merchantNpc.Items);

                        SellItemServerRpc(e.Item);
                    }
                });
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                CheckHide();
                CheckNpcClicked();
            }
        }

        private void CheckHide()
        {
            if (_merchantNpc != null && _input.Move != Vector2.zero && _merchantNpc.transform.IsFarToTarget(transform.gameObject, _npcMaxDistance) || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                MerchantUI.Instance.Hide();
                _merchantNpc = null;
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
        private void PurchaseItemServerRpc(InventoryItemDto item)
        {
            var playerSessionId = UserManager.Instance.GetPlayerSessionId(OwnerClientId);

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
                PlayerSessionId = playerSessionId
            });
        }

        [ServerRpc]
        private void SellItemServerRpc(InventoryItemDto item)
        {
            var playerSessionId = UserManager.Instance.GetPlayerSessionId(OwnerClientId);

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
                PlayerSessionId = playerSessionId
            });
        }
    }
}
