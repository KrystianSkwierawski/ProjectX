using System;
using System.Threading;
using Assets.Scripts.Areas.Inventory.Mono;
using Assets.Scripts.Areas.Trade.Mono;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Friends.Mono;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Shared;
using Assets.Scripts.Areas.Professions.Mono;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using PartyController = Assets.Scripts.Areas.Party.Mono.Party;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Player : NetworkBehaviour
    {
        private CancellationTokenSource _spawnLifetime;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _spawnLifetime?.Dispose();
            _spawnLifetime = new CancellationTokenSource();
        }

        private bool IsCurrentSpawn(CancellationToken token) => IsSpawned && _spawnLifetime != null
            && _spawnLifetime.Token == token && !token.IsCancellationRequested;

        private void Start()
        {
            if (IsOwner)
            {
                UserManager.Instance.OwnerClientId = OwnerClientId;
                LoadCharacterServerRpc();
            }

            if (IsServer)
            {
                AddExperienceSubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) =>
                {
                    var playerSessionId = GetCurrentPlayerSessionId();

                    var result = await UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
                    {
                        Amount = e.Amount,
                        type = e.Type,
                    }, playerSessionId);

                    ApplyPersistedExperienceLevel(e.Type, result.Level);
                });

                AttackPlayerSubscription.Instance.Subscribe(OwnerClientId.ToString(), (e) =>
                {
                    var playerSessionId = GetCurrentPlayerSessionId();
                    var character = UserManager.Instance.Characters[OwnerClientId];

                    if (character.IsAttackDodged())
                    {
                        Debug.Log($"Player dodged attack. Dexterity: {character.Dexterity}");

                        return;
                    }

                    var damage = character.ApplyArmor(e.Value);

                    if (character.AmmoType.IsArmorAmmo())
                    {
                        ConsumeAmmo();
                    }

                    character.Health = Math.Max(character.Health - damage, 0);
                    PartyController.NotifyCharacterChanged(OwnerClientId);

                    var respawn = GetComponent<DungeonTravel>().GetRespawnPose();

                    if (character.Health == 0)
                    {
                        GetComponent<TargetSelector>().ResetForTravel();
                        transform.SetPositionAndRotation(respawn.position, respawn.rotation);
                        Debug.Log($"Player respawned. ClientId: {OwnerClientId}, InstanceId: {DungeonTravel.GetInstanceId(OwnerClientId)}.");
                    }

                    AttackPlayerClientRpc(character.Health, respawn.position, respawn.rotation, OwnerClientId.ToClientRpcParams());

                    PersistHealthAsync(character, playerSessionId, OwnerClientId).Forget();
                });
            }
        }

        private async UniTask PersistHealthAsync(CharacterDto character, string playerSessionId, ulong clientId)
        {
            var inventory = GetComponent<CharacterInventory>();
            using var mutation = TradeServerState.TrackInventoryMutation(clientId);
            var operationToken = TradeCommitCoordinator.GetServerLifetimeCancellationToken();

            await inventory.WaitForEquipmentMutationAsync(operationToken);

            try
            {
                // Damage is immediate; its save waits for potion/gear persistence and captures the resulting HP.
                var current = UserManager.Instance.Characters.TryGetValue(clientId, out var connectedCharacter)
                    ? connectedCharacter
                    : character;
                var health = current.Health;

                await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
                {
                    Health = health
                }, playerSessionId, cancellationToken: operationToken);

                Debug.Log($"Health persisted after pending character mutations. ClientId: {clientId}, Health: {health}.");
            }
            finally
            {
                inventory.ReleaseEquipmentMutation();
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            // TODOL: close panels
            //if (Keyboard.current.escapeKey.wasPressedThisFrame)
            //{
            //    CharacterUI.Instance.Hide();
            //}

            if (!InputFocusUI.IsAnyInputFocused && Keyboard.current.cKey.wasPressedThisFrame)
            {
                CharacterUI.Instance.Toggle();
            }

            if (!InputFocusUI.IsAnyInputFocused && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                GearUI.Instance.Toggle();
            }
        }

        [ClientRpc]
        private void AttackPlayerClientRpc(int health, Vector3 respawnPosition, Quaternion respawnRotation, ClientRpcParams rpcParams = default)
        {
            var character = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId];

            character.Health = health;

            GetComponent<Crafting>()?.InterruptCrafting();

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MonsterAttack, 0.4f);

            if (health == 0)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Death, 0.3f);

                var previousPosition = transform.position;
                var controller = GetComponent<CharacterController>();
                controller.enabled = false;
                transform.SetPositionAndRotation(respawnPosition, respawnRotation);
                GetComponent<ClientNetworkTransform>().Teleport(respawnPosition, respawnRotation, transform.localScale);
                Physics.SyncTransforms();
                controller.enabled = true;
                GetComponent<StarterAssets.ThirdPersonController>().ResetAfterTeleport(respawnPosition - previousPosition);
            }

            PlayerUI.Instance.SetHealth(character.Health);
        }

        [ServerRpc]
        private void LoadCharacterServerRpc()
        {
            var playerSessionId = GetCurrentPlayerSessionId();
            LoadCharacterAsync(playerSessionId, _spawnLifetime.Token).Forget();
        }

        private async UniTask LoadCharacterAsync(string playerSessionId, CancellationToken token)
        {
            var characterId = UserManager.Instance.GetPlayerCharacterId(OwnerClientId);
            CharacterDto character;

            try
            {
                character = await UnityWebRequestHelper.ExecuteGetAsync<CharacterDto>("Characters/Current", playerSessionId,
                    cancellationToken: token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }

            if (!IsCurrentSpawn(token))
            {
                return;
            }

            if (character.Id != characterId)
            {
                throw new InvalidOperationException("The API returned a different character than the authenticated player session.");
            }

            UserManager.Instance.Characters[OwnerClientId] = character;
            PartyController.NotifyCharacterChanged(OwnerClientId);
            FriendList.NotifyFriendStateChanged();

            UpdatePlayerClientRpc(character, OwnerClientId.ToClientRpcParams());
        }

        [ClientRpc]
        public void UpdatePlayerClientRpc(CharacterDto character, ClientRpcParams rpcParams = default)
        {
            UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId] = character;

            PlayerUI.Instance.SetPlayer();

            GearUI.Instance.UpdateLeftPanel();
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(ExperienceTypeEnum type, byte level, ClientRpcParams rpcParams = default)
        {
            UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId].Levels[type] = level;
            CharacterUI.Instance.RefreshDescription();

            if (type == ExperienceTypeEnum.Main)
            {
                PlayerUI.Instance.SetMainLevel(level);

                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.LevelUp, 0.1f);

                var message = string.Format(TranslateManager.Instance.GetByKey(TranslateKeyEnum.LevelUp), level);

                LogUI.Instance.ShowAsync(message).Forget();
            }

            CraftingUI.Instance.UpdateRequirements(InventoryItemEnum.Xp);
        }

        internal void ApplyPersistedExperienceLevel(ExperienceTypeEnum type, byte level)
        {
            if (!UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character) ||
                level <= character.Levels[type])
            {
                return;
            }

            character.Levels[type] = level;
            PartyController.NotifyCharacterChanged(OwnerClientId);

            if (type == ExperienceTypeEnum.Main)
            {
                FriendList.NotifyFriendStateChanged();
            }

            UpdateLevelClientRpc(type, level, OwnerClientId.ToClientRpcParams());
        }

        public void ConsumeAmmo()
        {
            ConsumeAmmoAsync(_spawnLifetime.Token).Forget();
        }

        private async UniTask ConsumeAmmoAsync(CancellationToken spawnToken)
        {
            var inventory = GetComponent<CharacterInventory>();
            var clientId = OwnerClientId;
            var character = UserManager.Instance.Characters[clientId];
            using var mutation = TradeServerState.TrackInventoryMutation(clientId);
            var operationToken = TradeCommitCoordinator.GetServerLifetimeCancellationToken();
            var playerSessionId = GetCurrentPlayerSessionId();

            await inventory.WaitForEquipmentMutationAsync(operationToken);

            try
            {
                // Keep the accepted hit's state after despawn; a live equipment recovery may replace the DTO.
                if (IsCurrentSpawn(spawnToken)
                    && UserManager.Instance.Characters.TryGetValue(clientId, out var currentCharacter))
                {
                    character = currentCharacter;
                }

                if (character.AmmoType != InventoryItemEnum.AmmoTemplate && character.AmmoCount > 0)
                {
                    if (IsCurrentSpawn(spawnToken))
                    {
                        ConsumeAmmoClientRpc(clientId.ToClientRpcParams());
                    }

                    character.AmmoCount--;

                    if (character.AmmoCount <= 0)
                    {
                        AbstractGearUsableItem.RemoveStats(character, character.AmmoType);

                        character.AmmoType = InventoryItemEnum.AmmoTemplate;
                        character.AmmoCount = 0;
                    }

                    // TODO: split to smaller commands
                    await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
                    {
                        MaxHealth = character.MaxHealth,
                        Strength = character.Strength,
                        Dexterity = character.Dexterity,
                        Speed = character.Speed,
                        Intellect = character.Intellect,
                        Armor = character.Armor,
                        AmmoType = character.AmmoType,
                        AmmoCount = character.AmmoCount,
                    }, playerSessionId, cancellationToken: operationToken);

                    Debug.Log($"Accepted ammo consumption persisted. ClientId: {clientId}, CharacterId: {character.Id}, AmmoType: {character.AmmoType}, AmmoCount: {character.AmmoCount}.");
                }
            }
            finally
            {
                inventory.ReleaseEquipmentMutation();
            }
        }

        [ClientRpc]
        private void ConsumeAmmoClientRpc(ClientRpcParams rpcParams = default)
        {
            var character = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId];

            if (character.AmmoType != InventoryItemEnum.AmmoTemplate && character.AmmoCount > 0)
            {
                character.AmmoCount--;

                if (character.AmmoCount <= 0)
                {
                    AbstractGearUsableItem.RemoveStats(character, character.AmmoType);

                    character.AmmoType = InventoryItemEnum.AmmoTemplate;
                    character.AmmoCount = 0;

                    GearUI.Instance.UpdateRightPanel();

                    PlayerUI.Instance.SetMaxHealth(character.MaxHealth);
                }

                GearUI.Instance.Wear(GearUI.Instance.Ammo, new InventoryItemDto
                {
                    Type = character.AmmoType,
                    Count = character.AmmoCount
                });
            }
        }

        public override void OnNetworkDespawn()
        {
            _spawnLifetime?.Cancel();

            var characterRemoved = UserManager.Instance.Characters.Remove(OwnerClientId);

            if (IsServer)
            {
                if (characterRemoved)
                {
                    FriendList.NotifyFriendStateChanged();
                }

                var key = OwnerClientId.ToString();

                AddExperienceSubscription.Instance.Unsubscribe(key);
                AttackPlayerSubscription.Instance.Unsubscribe(key);
            }

            base.OnNetworkDespawn();
        }
        public override void OnDestroy()
        {
            _spawnLifetime?.Cancel();
            _spawnLifetime?.Dispose();
            _spawnLifetime = null;

            var characterRemoved = UserManager.Instance.Characters.Remove(OwnerClientId);

            if (IsServer)
            {
                if (characterRemoved)
                {
                    FriendList.NotifyFriendStateChanged();
                }

                var key = OwnerClientId.ToString();

                AddExperienceSubscription.Instance.Unsubscribe(key);
                AttackPlayerSubscription.Instance.Unsubscribe(key);
            }

            base.OnDestroy();
        }

        private string GetCurrentPlayerSessionId()
        {
            return UserManager.Instance.GetPlayerSessionId(OwnerClientId);
        }
    }
}
