using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Network
{
    public class Player : NetworkBehaviour
    {
        private CharacterDto _character;

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

                    if (result.Level > _character.Levels[e.Type])
                    {
                        _character.Levels[e.Type] = result.Level;

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
                    _character.Health -= _character.Health <= 0 ? 0 : e.Value;

                    if (_character.Health <= 0)
                    {
                        _character.Health = 0;
                    }

                    AttackPlayerClientRpc(_character.Health, new ClientRpcParams
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
            if (IsOwner && Keyboard.current.cKey.wasPressedThisFrame)
            {
                ToggleCharacter();
            }
        }

        private void ToggleCharacter()
        {
            if (CharacterUI.Instance.Character.activeSelf)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);

                CharacterUI.Instance.Character.SetActive(false);

                return;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

            CharacterUI.Instance.DescriptionText.text = string.Format(TranslateManager.Instance.GetByKey(TranslateKeyEnum.CharacterDescription), _character.Levels.Values.Cast<object>().ToArray());

            CharacterUI.Instance.Show();
        }

        [ClientRpc]
        private void AttackPlayerClientRpc(int health, ClientRpcParams rpcParams = default)
        {
            _character.Health = health;

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MonsterAttack, 0.4f);

            if (health == 0)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Death, 0.3f);

                // FIXME: set on server
                transform.position = new Vector3(3.562874f, 1.41359f, 4.244279f);
            }

            PlayerUI.Instance.SetHealth(_character.Health);
        }

        [ServerRpc]
        private void SetCharacterServerRpc(string clientToken)
        {
            SetCharacterAsync(clientToken).Forget();
        }

        private async UniTask SetCharacterAsync(string clientToken)
        {
            _character = await UnityWebRequestHelper.ExecuteGetAsync<CharacterDto>("Characters/1", clientToken);

            UpdatePlayerClientRpc(_character, new ClientRpcParams
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
            _character = character;
            PlayerUI.Instance.SetPlayer(_character);
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(ExperienceTypeEnum type, byte level, ClientRpcParams rpcParams = default)
        {
            _character.Levels[type] = level;

            if (type == ExperienceTypeEnum.Main)
            {
                PlayerUI.Instance.SetMainLevel(level);
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.LevelUp, 0.1f);
            }
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