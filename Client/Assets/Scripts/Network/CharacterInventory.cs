using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Network;
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

                    UseItem(e.Item, UserManager.Instance.Token);

                    UseItemServerRpc(e.Item, UserManager.Instance.Token);
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
        private void UseItemServerRpc(InventoryItemDto item, string clientToken)
        {
            UseItem(item, clientToken);
        }

        private void UseItem(InventoryItemDto item, string clientToken)
        {
            IUsableItem usableItem = item.Type switch
            {
                InventoryItemEnum.HealthPotion => new HealthPotionUsableItem()
                    .WithClientToken(clientToken)
                    .WithOwnerClientId(OwnerClientId)
                    .WithCharacter(gameObject.GetComponent<Player>().Character),

                InventoryItemEnum.Currency => new CurrencyUsableItem(),

                _ => null
            };

            if (usableItem != null)
            {
                usableItem?.Use();
            }
        }

        public interface IUsableItem
        {
            InventoryItemEnum Type { get; }

            void Use();

            IUsableItem WithClientToken(string clientToken);

            IUsableItem WithOwnerClientId(ulong ownerClientId);

            IUsableItem WithCharacter(CharacterDto character);
        }

        public abstract class AbstractUsableItem : IUsableItem
        {
            public abstract InventoryItemEnum Type { get; }

            protected string ClientToken { get; private set; }

            protected ulong OwnerClientId { get; private set; }

            protected CharacterDto Character { get; private set; }

            public virtual void Use()
            {
#if UNITY_SERVER && !UNITY_EDITOR
                UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
                {
                    Request = new UpdateCharacterInventoryCommand
                    {
                        Remove = new InventoryItemDto[]
                        {
                            new InventoryItemDto
                            {
                                Type = Type,
                                Count = 1,
                            }
                        },
                    },
                    ClientToken = ClientToken,
                });
#endif
            }

            public IUsableItem WithClientToken(string clientToken)
            {
                ClientToken = clientToken;

                return this;
            }

            public IUsableItem WithOwnerClientId(ulong ownerClientId)
            {
                OwnerClientId = ownerClientId;

                return this;
            }

            public IUsableItem WithCharacter(CharacterDto character)
            {
                Character = character;

                return this;
            }
        }

        public class HealthPotionUsableItem : AbstractUsableItem
        {
            public override InventoryItemEnum Type { get; } = InventoryItemEnum.HealthPotion;

            public override void Use()
            {
                if (Character.Health >= 100)
                {
                    return;
                }

                // TODO: set on api
                Character.Health = Math.Min(Character.Health + 20, 100);

#if UNITY_EDITOR
                PlayerUI.Instance.SetHealth(Character.Health);
#endif

                base.Use();
            }
        }

        public class CurrencyUsableItem : AbstractUsableItem
        {
            public override InventoryItemEnum Type { get; } = InventoryItemEnum.Currency;

            public override void Use()
            {
#if UNITY_EDITOR
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Currency, 0.5f);
#endif
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