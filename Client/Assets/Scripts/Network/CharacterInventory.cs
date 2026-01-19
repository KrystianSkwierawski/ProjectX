using System.Collections.Generic;
using System.Linq;
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
        private InventoryItem[] _currentLoot;

        private readonly IDictionary<string, IList<LootItem>> _loot = new Dictionary<string, IList<LootItem>>
        {
            {
                "Bean(Clone)",
                new List<LootItem>
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
                new List<LootItem>
                {
                    new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.Fish,
                        Chance = 90,
                        Min = 1,
                        Max = 1
                    },
                     new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.Can,
                        Chance = 90,
                        Min = 1,
                        Max = 99
                    },
                      new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.Can,
                        Chance = 90,
                        Min = 5,
                        Max = 99
                    },
                       new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.Fish,
                        Chance = 90,
                        Min = 6,
                        Max = 99
                    },
                        new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.Can,
                        Chance = 90,
                        Min = 7,
                        Max = 99
                    },
                         new LootItem
                    {
                        Type = CharacterInventoryTypeEnum.Fish,
                        Chance = 90,
                        Min = 8,
                        Max = 99
                    }
                }
            },
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
                CheckLootSubscription.Instance.Subscribe(OwnerClientId.ToString(), (e) =>
                {
                    if (_loot.TryGetValue(e.GameObjectName, out var drops))
                    {
                        _currentLoot = ProcessLoot(e, drops).ToArray();

                        ShowLootClientRpc(_currentLoot, new ClientRpcParams
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

        private IEnumerable<InventoryItem> ProcessLoot(UpdateInventorySubscriptionEvent e, IList<LootItem> drops)
        {
            foreach (var drop in drops)
            {
                int trials = Mathf.Max(0, drop.Max - drop.Min);

                int successes = Enumerable.Range(0, trials).Count(_ => Random.Range(0, 100) < drop.Chance);

                int count = drop.Min + successes;

                Debug.Log($"Drop calculated. Type: {drop.Type}, Min: {drop.Min}, Max: {drop.Max}, Trials: {trials}, Successes: {successes}, TotalCount: {count}");

                if (count > 0)
                {
                    yield return new InventoryItem
                    {
                        type = drop.Type,
                        count = count
                    };
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
        private void ShowLootClientRpc(InventoryItem[] items, ClientRpcParams rpcParams = default)
        {
            UIManager.Instance.ShowLoot(items);
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
                CheckLootSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                CheckLootSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
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