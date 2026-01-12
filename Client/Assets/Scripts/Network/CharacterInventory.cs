using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Mono
{
    public class CharacterInventory : NetworkBehaviour
    {
        private readonly IDictionary<string, IList<DropItem>> _drops = new Dictionary<string, IList<DropItem>>
        {
            {
                "Bean(Clone)",
                new List<DropItem>
                {
                    new DropItem
                    {
                        Type = CharacterInventoryTypeEnum.Can,
                        Chance = 50,
                        Min = 0,
                        Max = 2
                    }
                }
            }
        };

        public CharacterInventoryDto Inventory { get; set; }

        private async void Start()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            await UniTask.WaitUntil(
                () => !string.IsNullOrEmpty(TokenManager.Instance.Token),
                cancellationToken: cancellationToken
            );

            if (IsOwner)
            {
                await UpdateCharacterInventoryAsync();
            }

            if (IsServer)
            {
                UpdateInventorySubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) =>
                {
                    bool update = false;

                    if (_drops.TryGetValue(e.GameObjectName, out var drops))
                    {
                        foreach (var drop in drops)
                        {
                            int trials = Mathf.Max(0, drop.Max - drop.Min);

                            int successes = Enumerable.Range(0, trials).Count(_ => Random.Range(0, 100) < drop.Chance);

                            int count = drop.Min + successes;

                            Debug.Log($"Drop calculated. Type: {drop.Type}, Min: {drop.Min}, Max: {drop.Max}, Trials: {trials}, Successes: {successes}, TotalCount: {count}");

                            if (count > 0)
                            {
                                var item = new InventoryItem
                                {
                                    type = drop.Type,
                                    count = count
                                };

                                CheckCharacterQuestSubscription.Instance.Invoke(OwnerClientId.ToString(), new CheckCharacterQuestSubscriptionEvent
                                {
                                    Progress = item.count,
                                    GameObjectName = item.type.ToString(),
                                    ClientToken = e.ClientToken,
                                });

                                // FIXME: reduce multiple calls
                                await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterInventories", new AddCharacterInventoryItemCommand
                                {
                                    characterId = 1,
                                    inventoryItem = item
                                }, e.ClientToken);

                                update = true;
                            }
                        }
                    }

                    if (update)
                    {
                        UpdateInventoryItemClientRpc(new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new ulong[] { OwnerClientId }
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
        private void UpdateInventoryItemClientRpc(ClientRpcParams rpcParams = default)
        {
            _ = UpdateCharacterInventoryAsync();
        }

        private async UniTask UpdateCharacterInventoryAsync()
        {
            Inventory = await UnityWebRequestHelper.ExecuteGetAsync<CharacterInventoryDto>("CharacterInventories?CharacterId=1");
            UIManager.Instance.UpdateInventory(Inventory);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                UpdateInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                UpdateInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnDestroy();
        }

        private class DropItem
        {
            public CharacterInventoryTypeEnum Type { get; set; }

            public int Chance { get; set; }

            public int Min { get; set; }

            public int Max { get; set; }
        }
    }
}