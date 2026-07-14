using System;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Player : NetworkBehaviour
    {
        private void Start()
        {
            if (IsOwner)
            {
                UserManager.Instance.OwnerClientId = OwnerClientId;
                LoadCharacterServerRpc(UserManager.Instance.Token);
            }

            if (IsServer)
            {
                AddExperienceSubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) =>
                {
                    var result = await UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
                    {
                        CharacterId = 1,
                        Amount = e.Amount,
                        type = e.Type,
                    }, e.ClientToken);

                    var character = UserManager.Instance.Characters[OwnerClientId];

                    if (result.Level > character.Levels[e.Type])
                    {
                        character.Levels[e.Type] = result.Level;

                        UpdateLevelClientRpc(e.Type, result.Level, new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new ulong[] { OwnerClientId }
                            }
                        });
                    }
                });

                AttackPlayerSubscription.Instance.Subscribe(OwnerClientId.ToString(), (e) =>
                {
                    var character = UserManager.Instance.Characters[OwnerClientId];

                    if (character.IsAttackDodged())
                    {
                        Debug.Log($"Player dodged attack. Dexterity: {character.Dexterity}");

                        return;
                    }

                    var damage = character.ApplyArmor(e.Value);

                    character.Health = Math.Max(character.Health - damage, 0);

                    AttackPlayerClientRpc(character.Health, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    });

                    UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
                    {
                        CharacterId = 1,
                        Health = character.Health
                    }, e.ClientToken)
                    .Forget();
                });
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

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                CharacterUI.Instance.Toggle();
            }

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                GearUI.Instance.Toggle();
            }
        }

        [ClientRpc]
        private void AttackPlayerClientRpc(int health, ClientRpcParams rpcParams = default)
        {
            var character = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId];
            character.Health = health;

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MonsterAttack, 0.4f);

            if (health == 0)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Death, 0.3f);

                // FIXME: set on server
                transform.position = new Vector3(3.562874f, 1.41359f, 4.244279f);
            }

            PlayerUI.Instance.SetHealth(character.Health);
        }

        [ServerRpc]
        private void LoadCharacterServerRpc(string clientToken)
        {
            LoadCharacterAsync(clientToken).Forget();
        }

        private async UniTask LoadCharacterAsync(string clientToken)
        {
            var character = await UnityWebRequestHelper.ExecuteGetAsync<CharacterDto>("Characters/1", clientToken);
            UserManager.Instance.Characters[OwnerClientId] = character;

            UpdatePlayerClientRpc(character, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            });
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

        public override void OnNetworkDespawn()
        {
            UserManager.Instance.Characters.Remove(OwnerClientId);

            if (IsServer)
            {
                var key = OwnerClientId.ToString();

                AddExperienceSubscription.Instance.Unsubscribe(key);
                AttackPlayerSubscription.Instance.Unsubscribe(key);
            }

            base.OnNetworkDespawn();
        }
        public override void OnDestroy()
        {
            UserManager.Instance.Characters.Remove(OwnerClientId);

            if (IsServer)
            {
                var key = OwnerClientId.ToString();

                AddExperienceSubscription.Instance.Unsubscribe(key);
                AttackPlayerSubscription.Instance.Unsubscribe(key);
            }

            base.OnDestroy();
        }
    }
}
