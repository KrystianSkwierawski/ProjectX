using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Shared;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Inventory.Mono
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

                // TODO: only server rpc?
                UpdateInventorySubscription.Instance.Subscribe(key, (e) =>
                {
                    UpdateInventory(e.Request);

                    UpdateInventoryServerRpc(e.Request, e.ClientToken);
                });

                UseItemSubscribtion.Instance.Subscribe(key, (e) =>
                {
                    if (MerchantUI.Instance.Merchant.activeSelf)
                    {
                        return;
                    }

                    UseItem(e.Item, e.From, UserManager.Instance.Token);

                    UseItemServerRpc(e.Item, e.From, UserManager.Instance.Token);
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
                        .Where(x => x.Type == drop.Type)
                        .FirstOrDefault();

                    if (loot == null)
                    {
                        _currentLoot.Add(new InventoryItemDto
                        {
                            Type = drop.Type,
                            Count = count,
                        });

                        continue;
                    }

                    loot.Count += count;
                }
            }
        }

        private void Update()
        {
            if (IsOwner && Keyboard.current.bKey.wasPressedThisFrame)
            {
                InventoryUI.Instance.Toggle();
            }
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
            if (request.Add.Length > 0)
            {
                foreach (var item in request.Add)
                {
                    InventoryManager.Instance.Add(item);
                }

                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.AddItem, 0.5f);
            }

            foreach (var item in request.Remove)
            {
                InventoryManager.Instance.Remove(item);
            }

            // TODO: UpdatedInventorySubscription?
            InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);
            CraftingUI.Instance.UpdateRequirements();
            MerchantUI.Instance.UpdatePriceValidation();
        }

        [ServerRpc]
        private void UpdateInventoryServerRpc(UpdateCharacterInventoryCommand request, string clientToken)
        {
            var isValid = request.Add.All(x =>
            {
                return _currentLoot
                    .Where(c => c.Type == x.Type)
                    // TODO: count?
                    .Any();
            });

            Debug.Log($"UpdateInventoryServerRpc -> IsValid: {isValid}");

            if (isValid)
            {
                UpdateInventoryAsync(request, clientToken).Forget();

                _currentLoot.Clear();
            }
        }

        private async UniTask UpdateInventoryAsync(UpdateCharacterInventoryCommand request, string clientToken)
        {
            await InventoryManager.Instance.UpdateAsync(request, clientToken);

            foreach (var item in request.Add)
            {
                CheckCharacterQuestSubscription.Instance.Invoke(OwnerClientId.ToString(), new CheckCharacterQuestSubscriptionEvent
                {
                    Progress = item.Count,
                    GameObjectName = item.Type.ToString(),
                    ClientToken = clientToken,
                });
            }
        }

        [ServerRpc]
        private void UseItemServerRpc(InventoryItemDto item, UsableItemFromEnum from, string clientToken)
        {
            UseItem(item, from, clientToken);
        }

        private void UseItem(InventoryItemDto item, UsableItemFromEnum from, string clientToken)
        {
            if (item.Type.IsAmmo())
            {
                new AmmoUsableItem(item, clientToken, OwnerClientId).Use(from);

                return;
            }

            IUsableItem usableItem = item.Type switch
            {
                InventoryItemEnum.HealthPotion => new HealthPotionUsableItem(item, clientToken, OwnerClientId),
                InventoryItemEnum.Currency => new CurrencyUsableItem(item, clientToken, OwnerClientId),
                InventoryItemEnum.IronHelmet => new HelmetUsableItem(item, clientToken, OwnerClientId),
                InventoryItemEnum.IronChest => new ChestUsableItem(item, clientToken, OwnerClientId),
                InventoryItemEnum.IronBoots => new BootsUsableItem(item, clientToken, OwnerClientId),
                InventoryItemEnum.IronSword => new WeaponUsableItem(item, clientToken, OwnerClientId),
                _ => null
            };

            if (usableItem != null)
            {
                usableItem.Use(from);
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
