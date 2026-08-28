using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Shared;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Inventory.Mono
{
    public class CharacterInventory : NetworkBehaviour, ICharacterBuffController
    {
        private IList<InventoryItemDto> _currentLoot = new List<InventoryItemDto>();
        private readonly IDictionary<InventoryItemEnum, ActiveBuff> _activeBuffs = new Dictionary<InventoryItemEnum, ActiveBuff>();

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
                await InventoryManager.Instance.LoadAsync(UserManager.Instance.SelectedCharacterId);

                InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);

                UpdateInventorySubscription.Instance.Subscribe(key, (e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.PlayerSessionId))
                    {
                        return;
                    }

                    if (!InventoryManager.Instance.CanApply(e.Request))
                    {
                        ShowInventoryFull();

                        return;
                    }

                    UpdateInventoryServerRpc(e.Request);
                });

                SplitInventorySubscription.Instance.Subscribe(key, (e) =>
                {
                    if (SplitInventory(e.SourceSlotIndex))
                    {
                        SplitInventoryServerRpc(e.SourceSlotIndex);
                    }
                });

                MoveInventorySubscription.Instance.Subscribe(key, (e) =>
                {
                    if (MoveInventory(e.SourceSlotIndex, e.TargetSlotIndex))
                    {
                        MoveInventoryServerRpc(e.SourceSlotIndex, e.TargetSlotIndex);
                    }
                });

                UseItemSubscribtion.Instance.Subscribe(key, (e) =>
                {
                    if (MerchantUI.Instance.Merchant.activeSelf)
                    {
                        return;
                    }

                    if (UseItem(e.Item, e.From, null))
                    {
                        UseItemServerRpc(e.Item, e.From);
                    }
                });
            }

            if (IsServer)
            {
                UpdateInventorySubscription.Instance.Subscribe(key, (e) => ProcessInventoryUpdateAsync(e).Forget());

                CheckLootSubscription.Instance.Subscribe(key, (e) =>
                {
                    if (_loot.TryGetValue(e.GameObjectName, out var drops))
                    {
                        ProcessLoot(drops);

                        if (_currentLoot.Count > 0)
                        {
                            ShowLootClientRpc(_currentLoot.ToArray(), OwnerClientId.ToClientRpcParams());
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
            UpdateActiveBuffs();

            if (IsOwner && !InputFocusUI.IsAnyInputFocused && Keyboard.current.bKey.wasPressedThisFrame)
            {
                InventoryUI.Instance.Toggle();
            }
        }

        [ClientRpc]
        private void ShowLootClientRpc(InventoryItemDto[] items, ClientRpcParams rpcParams = default)
        {
            InventoryUI.Instance.UpdateLoot(items, OwnerClientId);
        }

        [ClientRpc]
        private void UpdateInventoryClientRpc(UpdateCharacterInventoryCommand request, ClientRpcParams rpcParams = default)
        {
            if (!UpdateInventory(request))
            {
                ReloadInventoryAsync().Forget();
            }
        }

        [ClientRpc]
        private void ShowInventoryFullClientRpc(ClientRpcParams rpcParams = default)
        {
            ShowInventoryFull();
        }

        private bool UpdateInventory(UpdateCharacterInventoryCommand request)
        {
            if (!InventoryManager.Instance.Apply(request))
            {
                return false;
            }

            if (request.Add.Length > 0)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.AddItem, 0.5f);
            }

            // TODO: UpdatedInventorySubscription?
            InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);

            CraftingUI.Instance.UpdateRequirements();

            MerchantUI.Instance.UpdatePriceValidation();

            return true;
        }

        private async UniTask ReloadInventoryAsync()
        {
            await InventoryManager.Instance.LoadAsync(UserManager.Instance.SelectedCharacterId);

            InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);
            CraftingUI.Instance.UpdateRequirements();
            MerchantUI.Instance.UpdatePriceValidation();
        }

        private bool SplitInventory(int sourceSlotIndex)
        {
            if (!InventoryManager.Instance.Split(sourceSlotIndex))
            {
                return false;
            }

            InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.AddItem, 0.5f);
            //CraftingUI.Instance.UpdateRequirements();
            //MerchantUI.Instance.UpdatePriceValidation();

            return true;
        }

        private bool MoveInventory(int sourceSlotIndex, int targetSlotIndex)
        {
            if (!InventoryManager.Instance.Move(sourceSlotIndex, targetSlotIndex))
            {
                return false;
            }

            InventoryUI.Instance.UpdateInventory(InventoryManager.Instance.Dto);

            return true;
        }

        [ServerRpc]
        private void UpdateInventoryServerRpc(UpdateCharacterInventoryCommand request)
        {
            var isValid = request.Add.All(x =>
            {
                return _currentLoot
                    .Where(c => c.Type == x.Type)
                    .Where(c => c.Count >= x.Count)
                    .Any();
            });

            Debug.Log($"UpdateInventoryServerRpc -> IsValid: {isValid}");

            if (isValid)
            {
                var playerSessionId = UserManager.Instance.GetPlayerSessionId(OwnerClientId);
                ProcessLootInventoryUpdateAsync(request, playerSessionId).Forget();
            }
        }

        [ServerRpc]
        private void SplitInventoryServerRpc(int sourceSlotIndex)
        {
            var playerSessionId = UserManager.Instance.GetPlayerSessionId(OwnerClientId);
            SplitInventoryAsync(sourceSlotIndex, playerSessionId).Forget();
        }

        [ServerRpc]
        private void MoveInventoryServerRpc(int sourceSlotIndex, int targetSlotIndex)
        {
            var playerSessionId = UserManager.Instance.GetPlayerSessionId(OwnerClientId);
            MoveInventoryAsync(sourceSlotIndex, targetSlotIndex, playerSessionId).Forget();
        }

        private async UniTask SplitInventoryAsync(int sourceSlotIndex, string playerSessionId)
        {
            await UpdateInventoryAsync(new UpdateCharacterInventoryCommand
            {
                SplitSlotIndex = sourceSlotIndex,
            }, playerSessionId);
        }

        private async UniTask MoveInventoryAsync(int sourceSlotIndex, int targetSlotIndex, string playerSessionId)
        {
            await UpdateInventoryAsync(new UpdateCharacterInventoryCommand
            {
                MoveSourceSlotIndex = sourceSlotIndex,
                MoveTargetSlotIndex = targetSlotIndex,
            }, playerSessionId);
        }

        private async UniTask ProcessInventoryUpdateAsync(UpdateInventorySubscriptionEvent e)
        {
            if (!e.PersistInApi)
            {
                SendInventoryUpdateToOwner(e.Request);
                e.OnSucceeded?.Invoke();

                return;
            }

            var playerSessionId = UserManager.Instance.GetPlayerSessionId(OwnerClientId);
            UpdateCharacterInventoryStatusEnum status;

            try
            {
                status = await UpdateInventoryAsync(e.Request, playerSessionId);
            }
            catch
            {
                RejectInventoryUpdate(e);

                throw;
            }

            if (status == UpdateCharacterInventoryStatusEnum.InventoryFull)
            {
                RejectInventoryUpdate(e);
                SendInventoryFullToOwner();

                return;
            }

            SendInventoryUpdateToOwner(e.Request);
            e.OnSucceeded?.Invoke();
        }

        private void RejectInventoryUpdate(UpdateInventorySubscriptionEvent e)
        {
            e.OnRejected?.Invoke();

            if (!e.ResynchronizeCharacterOnRejected)
            {
                return;
            }

            ResynchronizeCharacterClientRpc(
                UserManager.Instance.Characters[OwnerClientId],
                OwnerClientId.ToClientRpcParams());
        }

        private async UniTask ProcessLootInventoryUpdateAsync(UpdateCharacterInventoryCommand request, string playerSessionId)
        {
            var status = await UpdateInventoryAsync(request, playerSessionId);

            if (status == UpdateCharacterInventoryStatusEnum.InventoryFull)
            {
                SendInventoryFullToOwner();
                ShowLootClientRpc(_currentLoot.ToArray(), OwnerClientId.ToClientRpcParams());

                return;
            }

            SendInventoryUpdateToOwner(request);
            _currentLoot.Clear();
        }

        private async UniTask<UpdateCharacterInventoryStatusEnum> UpdateInventoryAsync(
            UpdateCharacterInventoryCommand request,
            string playerSessionId)
        {
            var result = await InventoryManager.Instance.UpdateAsync(request, playerSessionId);

            if (result.Status != UpdateCharacterInventoryStatusEnum.Applied)
            {
                return result.Status;
            }

            var changedItemTypes = request.Add
                .Concat(request.Remove)
                .Select(x => x.Type)
                .Where(x => x != InventoryItemEnum.None)
                .Distinct();

            foreach (var itemType in changedItemTypes)
            {
                CheckCharacterQuestSubscription.Instance.Invoke(OwnerClientId.ToString(), new CheckCharacterQuestSubscriptionEvent
                {
                    Progress = request.Add
                        .Where(x => x.Type == itemType)
                        .Sum(x => x.Count),
                    GameObjectName = itemType.ToString(),
                    PlayerSessionId = playerSessionId,
                });
            }

            return result.Status;
        }

        private void SendInventoryUpdateToOwner(UpdateCharacterInventoryCommand request)
        {
            UpdateInventoryClientRpc(request, OwnerClientId.ToClientRpcParams());
        }

        private void SendInventoryFullToOwner()
        {
            ShowInventoryFullClientRpc(OwnerClientId.ToClientRpcParams());
        }

        private static void ShowInventoryFull()
        {
            LogUI.Instance.ShowAsync(
                TranslateManager.Instance.GetByKey(TranslateKeyEnum.InventoryFull),
                color: ColorUI.Red)
                .Forget();
        }

        [ClientRpc]
        private void ResynchronizeCharacterClientRpc(CharacterDto character, ClientRpcParams rpcParams = default)
        {
            RestoreActiveBuffs(character);
            UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId] = character;

            PlayerUI.Instance.SetPlayer();
            GearUI.Instance.UpdateLeftPanel();
            GearUI.Instance.UpdateRightPanel();
        }

        [ServerRpc]
        private void UseItemServerRpc(InventoryItemDto item, UsableItemFromEnum from)
        {
            UseItem(item, from, UserManager.Instance.GetPlayerSessionId(OwnerClientId));
        }

        private bool UseItem(InventoryItemDto item, UsableItemFromEnum from, string playerSessionId)
        {
            AbstractGearUsableItem gearItem = null;

            if (item.Type.IsAmmo())
            {
                gearItem = new AmmoUsableItem(item, playerSessionId, OwnerClientId);
            }
            else if (item.Type.IsWeapon())
            {
                gearItem = new WeaponUsableItem(item, playerSessionId, OwnerClientId);
            }
            else
            {
                gearItem = item.Type switch
                {
                    InventoryItemEnum.IronHelmet => new HelmetUsableItem(item, playerSessionId, OwnerClientId),
                    InventoryItemEnum.IronChest => new ChestUsableItem(item, playerSessionId, OwnerClientId),
                    InventoryItemEnum.IronBoots => new BootsUsableItem(item, playerSessionId, OwnerClientId),
                    _ => null
                };
            }

            if (gearItem != null)
            {
                return gearItem.TryUse(from);
            }

            IUsableItem usableItem = item.Type switch
            {
                InventoryItemEnum.HealthPotion => new HealthPotionUsableItem(item, playerSessionId, OwnerClientId),
                InventoryItemEnum.StrengthPotion => new StrengthPotionUsableItem(item, playerSessionId, OwnerClientId, this),
                InventoryItemEnum.SpeedPotion => new SpeedPotionUsableItem(item, playerSessionId, OwnerClientId, this),
                InventoryItemEnum.Currency => new CurrencyUsableItem(item, playerSessionId, OwnerClientId),
                _ => null
            };

            if (usableItem == null)
            {
                return false;
            }

            usableItem.Use(from);

            return true;
        }

        public void ApplyOrRefreshBuff(
            InventoryItemEnum type,
            float durationSeconds,
            Action<CharacterDto, bool> setActive)
        {
            ApplyOrRefreshBuffLocal(type, durationSeconds, setActive);

            if (!IsServer)
            {
                return;
            }

            ApplyOrRefreshBuffClientRpc(
                type,
                durationSeconds,
                OwnerClientId.ToClientRpcParams());
        }

        [ClientRpc]
        private void ApplyOrRefreshBuffClientRpc(
            InventoryItemEnum type,
            float durationSeconds,
            ClientRpcParams rpcParams = default)
        {
            Action<CharacterDto, bool> setActive = type switch
            {
                InventoryItemEnum.StrengthPotion => StrengthPotionUsableItem.ApplyBuff,
                InventoryItemEnum.SpeedPotion => SpeedPotionUsableItem.ApplyBuff,
                _ => null
            };

            if (setActive == null)
            {
                Debug.LogWarning($"CharacterInventory -> Unsupported buff type. Type: {type}");

                return;
            }

            ApplyOrRefreshBuffLocal(type, durationSeconds, setActive);
        }

        private void ApplyOrRefreshBuffLocal(
            InventoryItemEnum type,
            float durationSeconds,
            Action<CharacterDto, bool> setActive)
        {
            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            if (setActive == null)
            {
                throw new ArgumentNullException(nameof(setActive));
            }

            var character = UserManager.Instance.Characters[OwnerClientId];

            if (!_activeBuffs.TryGetValue(type, out var buff))
            {
                buff = new ActiveBuff
                {
                    SetActive = setActive
                };
                _activeBuffs.Add(type, buff);
                setActive(character, true);
            }

            buff.ExpiresAt = Time.unscaledTime + durationSeconds;
            buff.SetActive = setActive;

            if (IsOwner)
            {
                BuffUI.Instance?.ShowOrRefresh(type, durationSeconds);
                GearUI.Instance?.UpdateRightPanel();
            }
        }

        private void UpdateActiveBuffs()
        {
            if (_activeBuffs.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;

            foreach (var pair in _activeBuffs.ToArray())
            {
                var remaining = pair.Value.ExpiresAt - now;

                if (remaining > 0f)
                {
                    if (IsOwner)
                    {
                        BuffUI.Instance?.SetRemaining(pair.Key, remaining);
                    }

                    continue;
                }

                RemoveBuff(pair.Key, pair.Value);
            }
        }

        private void RemoveBuff(InventoryItemEnum type, ActiveBuff buff)
        {
            if (UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character))
            {
                buff.SetActive(character, false);
            }

            _activeBuffs.Remove(type);

            if (IsOwner)
            {
                BuffUI.Instance?.Hide(type);
                GearUI.Instance?.UpdateRightPanel();
            }
        }

        private void RestoreActiveBuffs(CharacterDto character)
        {
            foreach (var buff in _activeBuffs.Values)
            {
                buff.SetActive(character, true);
            }
        }

        private void ClearActiveBuffs()
        {
            foreach (var pair in _activeBuffs.ToArray())
            {
                RemoveBuff(pair.Key, pair.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            ClearActiveBuffs();

            UpdateInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            SplitInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            MoveInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            ClearActiveBuffs();

            UpdateInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            SplitInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            MoveInventorySubscription.Instance.Unsubscribe(OwnerClientId.ToString());

            base.OnDestroy();
        }

        private class LootItem
        {
            public InventoryItemEnum Type { get; set; }

            public int Chance { get; set; }

            public int Min { get; set; }

            public int Max { get; set; }
        }

        private sealed class ActiveBuff
        {
            public float ExpiresAt { get; set; }

            public Action<CharacterDto, bool> SetActive { get; set; }
        }
    }
}
