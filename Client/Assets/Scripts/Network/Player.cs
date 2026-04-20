using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Network
{
    public class Player : NetworkBehaviour
    {
        private CharacterDto _character;

        private void Start()
        {
            if (IsOwner)
            {
                SetCharacterServerRpc(UserManager.Instance.Token);
            }

            if (IsServer)
            {
                AddExperienceSubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) =>
                {
                    var result = await UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
                    {
                        characterId = 1,
                        amount = e.Amount,
                        type = e.Type,
                    }, e.ClientToken);

                    if (e.Type == ExperienceTypeEnum.Main && result.level > _character.Levels[ExperienceTypeEnum.Main])
                    {
                        _character.Levels[ExperienceTypeEnum.Main] = result.level;

                        UpdateLevelClientRpc(result.level, new ClientRpcParams
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
        public void UpdateLevelClientRpc(byte level, ClientRpcParams rpcParams = default)
        {
            _character.Levels[ExperienceTypeEnum.Main] = level;
            PlayerUI.Instance.SetMainLevel(level);
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.LevelUp, 0.1f);
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