using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Mono
{
    public class CharacterInventory : NetworkBehaviour
    {
        public CharacterInventoryDto Inventory { get; set; }

        private async void Start()
        {
            await UniTask.WaitUntil(() => !string.IsNullOrEmpty(TokenManager.Instance.Token));

            if (IsOwner)
            {
                Inventory = await UnityWebRequestHelper.ExecuteGetAsync<CharacterInventoryDto>("CharacterInventories?CharacterId=1");
                UIManager.Instance.InitInventory(Inventory.count);
            }

            if (IsServer)
            {
                InventorySubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) =>
                {
                    // TODO: drop chance by enemy and inventory modo
                    int random = UnityEngine.Random.Range(0, 99);

                    if (random < 90)
                    {
                        var item = new InventoryItem
                        {
                            type = CharacterInventoryTypeEnum.Can,
                            count = 1
                        };

                        await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterInventories", new AddCharacterInventoryItemCommand
                        {
                            characterId = 1,
                            inventoryItem = item
                        }, e.ClientToken);

                        UpdateInventoryClientRpc(item, new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new ulong[] { ulong.Parse(e.Key) }
                            }
                        });
                    }
                });
            }
        }

        private void Update()
        {
            if (IsOwner && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                // TODO: wait?
                ToggleInventory();
            }
        }

        private static void ToggleInventory()
        {
            if (UIManager.Instance.Inventory.activeSelf)
            {
                AudioManager.Instance.PlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);

                UIManager.Instance.Inventory.SetActive(false);

                return;
            }

            AudioManager.Instance.PlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

            UIManager.Instance.Inventory.SetActive(true);
        }

        [ClientRpc]
        private void UpdateInventoryClientRpc(InventoryItem item, ClientRpcParams rpcParams = default)
        {
            UIManager.Instance.AddInventoryItem(item);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                InventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }
    }
}