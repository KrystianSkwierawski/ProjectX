using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Mono
{
    public class CharacterInventory : NetworkBehaviour
    {
        private IList<InventoryItem> _currentLoot = new List<InventoryItem>();

        #region LootDictionary

        private readonly IDictionary<string, LootItem[]> _loot = new Dictionary<string, LootItem[]>
        {
            {
               "Bean(Clone)",
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.Can,
                        Chance = 50,
                        Min = 0,
                        Max = 2
                    }
                }
            },
            {
                nameof(CharacterInventoryTypeEnum.Fish),
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.Fish,
                        Chance = 90,
                        Min = 1,
                        Max = 1
                    },
                }
            },
            {
                "BlackRock(Clone)",
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.BlackOre,
                        Chance = 90,
                        Min = 1,
                        Max = 6
                    },
                }
            },
            {
                "CopperRock(Clone)",
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.CopperOre,
                        Chance = 90,
                        Min = 1,
                        Max = 3
                    },
                }
            },
            {
                "WhiteRock(Clone)",
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.WhiteOre,
                        Chance = 90,
                        Min = 1,
                        Max = 2
                    },
                }
            },
            {
                "PurpleRock(Clone)",
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.PurpleOre,
                        Chance = 90,
                        Min = 1,
                        Max = 4
                    },
                }
            },
        };

        #endregion

        public CharacterInventoryDto Inventory { get; set; }

        private async void Start()
        {
            var key = OwnerClientId.ToString();

            if (IsOwner)
            {
                await UpdateCharacterInventoryAsync();

                AddInventoryItemSubscription.Instance.Subscribe(key, (e) =>
                {
                    AddItemServerRpc(e.Item, e.ClientToken);
                    AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.AddItem, 0.5f);
                });

                RemoveInventoryItemSubscription.Instance.Subscribe(key, (e) =>
                {
                    var item = Inventory.inventory.items
                       .Where(x => x.type == e.Item.type)
                       .Where(x => x.count >= e.Item.count)
                       .First();

                    if (item.count == e.Item.count)
                    {
                        Inventory.inventory.items.Remove(item);
                    }
                    else
                    {
                        item.count -= e.Item.count;
                    }

                    InventoryUI.Instance.UpdateInventory(Inventory);
                    AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.AddItem, 0.5f);
                });
            }

            if (IsServer)
            {
                CheckLootSubscription.Instance.Subscribe(key, (e) =>
                {
                    if (_loot.TryGetValue(e.GameObjectName, out var drops))
                    {
                        ProcessLoot(drops);

                        if (_currentLoot.Any())
                        {
                            ShowLootClientRpc(_currentLoot.ToArray(), new ClientRpcParams
                            {
                                Send = new ClientRpcSendParams
                                {
                                    TargetClientIds = new ulong[] { OwnerClientId }
                                }
                            });
                        }
                    }
                });
            }
        }

        private void ProcessLoot(LootItem[] drops)
        {
            foreach (var drop in drops)
            {
                int trials = Mathf.Max(0, drop.Max - drop.Min);

                int successes = Enumerable.Range(0, trials).Count(_ => UnityEngine.Random.Range(0, 100) < drop.Chance);

                int count = drop.Min + successes;

                Debug.Log($"Drop calculated. Type: {drop.Type}, Min: {drop.Min}, Max: {drop.Max}, Trials: {trials}, Successes: {successes}, TotalCount: {count}");

                if (count > 0)
                {
                    var loot = _currentLoot
                        .Where(x => x.type == drop.Type)
                        .FirstOrDefault();

                    if (loot == null)
                    {
                        _currentLoot.Add(new InventoryItem
                        {
                            type = drop.Type,
                            count = count,
                        });

                        continue;
                    }

                    loot.count += count;
                }
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
            if (InventoryUI.Instance.Inventory.activeSelf)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);

                InventoryUI.Instance.Inventory.SetActive(false);

                return;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

            InventoryUI.Instance.Inventory.SetActive(true);
        }

        [ClientRpc]
        private void ShowLootClientRpc(InventoryItem[] items, ClientRpcParams rpcParams = default)
        {
            InventoryUI.Instance.UpdateLoot(items, OwnerClientId, UserManager.Instance.Token);
        }

        [ServerRpc]
        private void AddItemServerRpc(InventoryItem item, string clientToken)
        {
            AddItemAsync(item, clientToken).Forget();
        }

        private async UniTask AddItemAsync(InventoryItem item, string clientToken)
        {
            var serverItem = _currentLoot
                .Where(x => x.type == item.type)
                .First();

            CheckCharacterQuestSubscription.Instance.Invoke(OwnerClientId.ToString(), new CheckCharacterQuestSubscriptionEvent
            {
                Progress = serverItem.count,
                GameObjectName = serverItem.type.ToString(),
                ClientToken = clientToken,
            });

            await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterInventories", new AddCharacterInventoryItemCommand
            {
                characterId = 1,
                inventoryItem = serverItem
            }, clientToken);

            _currentLoot.Remove(serverItem);

            AddInventoryItemClientRpc(item, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            });
        }

        [ClientRpc]
        private void AddInventoryItemClientRpc(InventoryItem item, ClientRpcParams rpcParams = default)
        {
            var slot = Inventory.inventory.items
                .Where(x => x.type == item.type)
                .FirstOrDefault();

            if (slot == null && Inventory.inventory.items.Count >= Inventory.count)
            {
                // TODO: out of slots
                return;
            }

            if (slot != null)
            {
                slot.count += item.count;
            }
            else
            {
                Inventory.inventory.items.Add(item);
            }

            InventoryUI.Instance.UpdateInventory(Inventory);
        }

        private async UniTask UpdateCharacterInventoryAsync()
        {
            Inventory = await UnityWebRequestHelper.ExecuteGetAsync<CharacterInventoryDto>("CharacterInventories?CharacterId=1");

            InventoryUI.Instance.UpdateInventory(Inventory);
        }

        public override void OnNetworkDespawn()
        {
            var key = OwnerClientId.ToString();

            if (IsOwner)
            {
                AddInventoryItemSubscription.Instance.Unsubscribe(key);
                RemoveInventoryItemSubscription.Instance.Unsubscribe(key);
            }

            if (IsServer)
            {
                CheckLootSubscription.Instance.Unsubscribe(key);
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            var key = OwnerClientId.ToString();

            if (IsOwner)
            {
                AddInventoryItemSubscription.Instance.Unsubscribe(key);
                RemoveInventoryItemSubscription.Instance.Unsubscribe(key);
            }

            if (IsServer)
            {
                CheckLootSubscription.Instance.Unsubscribe(key);
            }

            base.OnDestroy();
        }

        private class LootItem
        {
            public CharacterInventoryTypeEnum Type { get; set; }

            public int Chance { get; set; }

            public int Min { get; set; }

            public int Max { get; set; }
        }
    }
}