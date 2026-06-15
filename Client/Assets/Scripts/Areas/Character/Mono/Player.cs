using System;
using System.Linq;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Shared.Enums;
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
                SetCharacterServerRpc(UserManager.Instance.Token);
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

                    if (result.Level > UserManager.Instance.Character.Levels[e.Type])
                    {
                        UserManager.Instance.Character.Levels[e.Type] = result.Level;

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
                    UserManager.Instance.Character.Health = Math.Max(UserManager.Instance.Character.Health - e.Value, 0);

                    AttackPlayerClientRpc(UserManager.Instance.Character.Health, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    });

                    // TODO: call api
                });
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CharacterUI.Instance.Hide();
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                ToggleCharacter();
            }
        }

        private void ToggleCharacter()
        {
            if (CharacterUI.Instance.Character.activeSelf)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);

                CharacterUI.Instance.Hide();

                return;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

            CharacterUI.Instance.DescriptionText.text = string.Format(TranslateManager.Instance.GetByKey(TranslateKeyEnum.CharacterDescription), UserManager.Instance.Character.Levels.Values.Cast<object>().ToArray());

            CharacterUI.Instance.Show();
        }

        [ClientRpc]
        private void AttackPlayerClientRpc(int health, ClientRpcParams rpcParams = default)
        {
            UserManager.Instance.Character.Health = health;

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MonsterAttack, 0.4f);

            if (health == 0)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Death, 0.3f);

                // FIXME: set on server
                transform.position = new Vector3(3.562874f, 1.41359f, 4.244279f);
            }

            PlayerUI.Instance.SetHealth(UserManager.Instance.Character.Health);
        }

        [ServerRpc]
        private void SetCharacterServerRpc(string clientToken)
        {
            SetCharacterAsync(clientToken).Forget();
        }

        private async UniTask SetCharacterAsync(string clientToken)
        {
            UserManager.Instance.Character = await UnityWebRequestHelper.ExecuteGetAsync<CharacterDto>("Characters/1", clientToken);

            UpdatePlayerClientRpc(UserManager.Instance.Character, new ClientRpcParams
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
            UserManager.Instance.Character = character;
            PlayerUI.Instance.SetPlayer(UserManager.Instance.Character);
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(ExperienceTypeEnum type, byte level, ClientRpcParams rpcParams = default)
        {
            UserManager.Instance.Character.Levels[type] = level;

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