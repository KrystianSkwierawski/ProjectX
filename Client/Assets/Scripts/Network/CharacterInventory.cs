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

                UpdateInventorySubscription.Instance.Subscribe(key, (e) =>
                {
                    UpdateInventory(e.Request);

                    UpdateInventoryServerRpc(e.Request, e.ClientToken);
                });
            }

            if (IsServer)
            {
                UpdateInventorySubscription.Instance.Subscribe(key, (e) =>
                {
                    UpdateInventoryClientRpc(e.Request, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    });

                    UpdateInventoryAsync(e.Request, e.ClientToken).Forget();
                });

                CheckLootSubscription.Instance.Subscribe(key, (e) =>
                {
                    if (_loot.TryGetValue(e.GameObjectName, out var drops))
                    {
                        ProcessLoot(drops);

                        if (_currentLoot.Count > 0)
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

        [ClientRpc]
        private void UpdateInventoryClientRpc(UpdateCharacterInventoryCommand request, ClientRpcParams rpcParams = default)
        {
            UpdateInventory(request);
        }

        private void UpdateInventory(UpdateCharacterInventoryCommand request)
        {
            if (request.add.Count > 0)
            {
                foreach (var item in request.add)
                {
                    InventoryManager.Instance.Add(item);
                }

                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.AddItem, 0.5f);
            }

            foreach (var item in request.remove)
            {
                InventoryManager.Instance.Remove(item);
            }

            InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);
            CraftingUI.Instance.UpdateRequirements();
        }

        [ServerRpc]
        private void UpdateInventoryServerRpc(UpdateCharacterInventoryCommand request, string clientToken)
        {
            var isValid = request.add.All(x =>
            {
                return _currentLoot
                    .Where(c => c.type == x.type)
                    .Any();
            });

            if (isValid)
            {
                UpdateInventoryAsync(request, clientToken).Forget();

                _currentLoot.Clear();
            }
        }

        private async UniTask UpdateInventoryAsync(UpdateCharacterInventoryCommand request, string clientToken)
        {
            await InventoryManager.Instance.UpdateAsync(request, clientToken);

            foreach (var item in request.add)
            {
                CheckCharacterQuestSubscription.Instance.Invoke(OwnerClientId.ToString(), new CheckCharacterQuestSubscriptionEvent
                {
                    Progress = item.count,
                    GameObjectName = item.type.ToString(),
                    ClientToken = clientToken,
                });
            }
        }

        public override void OnNetworkDespawn()
        {
            UpdateInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            UpdateInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());

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