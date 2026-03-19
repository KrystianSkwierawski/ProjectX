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
        private IList<InventoryItemDto> _currentLoot = new List<InventoryItemDto>();

        #region LootDictionary

        private readonly IDictionary<string, LootItem[]> _loot = new Dictionary<string, LootItem[]>
        {
            {
               "Bean(Clone)",
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = InventoryItemEnum.Can,
                        Chance = 50,
                        Min = 0,
                        Max = 2
                    }
                }
            },
            {
                nameof(InventoryItemEnum.Fish),
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = InventoryItemEnum.Fish,
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
                        Type = InventoryItemEnum.BlackOre,
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
                        Type = InventoryItemEnum.CopperOre,
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
                        Type = InventoryItemEnum.WhiteOre,
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
                        Type = InventoryItemEnum.PurpleOre,
                        Chance = 90,
                        Min = 1,
                        Max = 4
                    },
                }
            },
            {
                "Chamomile(Clone)",
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = InventoryItemEnum.Chamomile,
                        Chance = 90,
                        Min = 1,
                        Max = 4
                    },
                }
            },
            {
            "Tree(Clone)",
                new LootItem[]
                {
                    new LootItem
                    {
                        Type = InventoryItemEnum.Wood,
                        Chance = 90,
                        Min = 1,
                        Max = 4
                    },
                }
            },
        };

        #endregion

        private async void Start()
        {
            var key = OwnerClientId.ToString();

            if (IsOwner)
            {
                await InventoryManager.Instance.LoadAsync();

                InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);

                AddInventoryItemSubscription.Instance.Subscribe(key, (e) =>
                {
                    AddItemServerRpc(e.Item, e.ClientToken);
                });
            }

            if (IsServer)
            {
                AddInventoryItemSubscription.Instance.Subscribe(key, async (e) =>
                {
                    _currentLoot.Add(new InventoryItemDto
                    {
                        type = e.Item.type,
                        count = e.Item.count,
                    });

                    await AddItemAsync(e.Item, e.ClientToken);
                });

                RemoveInventoryItemSubscription.Instance.Subscribe(key, async (e) =>
                {
                    await RemoveItemAsync(e.Item, e.ClientToken);
                });

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
                        _currentLoot.Add(new InventoryItemDto
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
        private void ShowLootClientRpc(InventoryItemDto[] items, ClientRpcParams rpcParams = default)
        {
            InventoryUI.Instance.UpdateLoot(items, OwnerClientId, UserManager.Instance.Token);
        }

        [ServerRpc]
        private void AddItemServerRpc(InventoryItemDto item, string clientToken)
        {
            AddItemAsync(item, clientToken).Forget();
        }

        private async UniTask AddItemAsync(InventoryItemDto item, string clientToken)
        {
            AddInventoryItemClientRpc(item, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            });

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
        }

        private async UniTask RemoveItemAsync(InventoryItemDto item, string clientToken)
        {
            RemoveItemClientRpc(item, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            });

            // TODO: remvoe
            await UniTask.Delay(1000);
            //await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterInventories", new AddCharacterInventoryItemCommand
            //{
            //    characterId = 1,
            //}, clientToken);
        }

        [ClientRpc]
        private void RemoveItemClientRpc(InventoryItemDto item, ClientRpcParams rpcParams = default)
        {
            InventoryManager.Instance.Remove(item);

            InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.AddItem, 0.5f);

            CraftingUI.Instance.UpdateRequirements();
        }

        [ClientRpc]
        private void AddInventoryItemClientRpc(InventoryItemDto item, ClientRpcParams rpcParams = default)
        {
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.AddItem, 0.5f);

            InventoryManager.Instance.Add(item);

            InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);
        }
        public override void OnNetworkDespawn()
        {
            var key = OwnerClientId.ToString();

            AddInventoryItemSubscription.Instance.Unsubscribe(key);
            RemoveInventoryItemSubscription.Instance.Unsubscribe(key);
            CheckLootSubscription.Instance.Unsubscribe(key);

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            var key = OwnerClientId.ToString();

            AddInventoryItemSubscription.Instance.Unsubscribe(key);
            CheckLootSubscription.Instance.Unsubscribe(key);
            RemoveInventoryItemSubscription.Instance.Unsubscribe(key);

            base.OnDestroy();
        }

        private class LootItem
        {
            public InventoryItemEnum Type { get; set; }

            public int Chance { get; set; }

            public int Min { get; set; }

            public int Max { get; set; }
        }
    }
}