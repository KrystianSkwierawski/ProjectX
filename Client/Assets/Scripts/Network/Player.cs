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

        private async void Start()
        {
            if (IsOwner)
            {
                await GetCharacterAsync();
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

                    if (e.Type == ExperienceTypeEnum.Main)
                    {
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
                    AttackPlayerClientRpc(e.Value, new ClientRpcParams
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
        private void AttackPlayerClientRpc(int value, ClientRpcParams rpcParams = default)
        {
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MonsterAttack, 0.4f);

            _character.health -= value;

            if (_character.health <= 0)
            {
                _character.health = 0;

                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Death, 0.3f);

                transform.position = new Vector3(3.562874f, 1.41359f, 4.244279f);
            }

            PlayerUI.Instance.SetHealth(_character.health);
        }

        private async UniTask GetCharacterAsync()
        {
            _character = await UnityWebRequestHelper.ExecuteGetAsync<CharacterDto>("Characters/1");

            PlayerUI.Instance.SetPlayer(_character);
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(byte level, ClientRpcParams rpcParams = default)
        {
            if (level > _character.mainLevel)
            {
                _character.mainLevel = level;
                PlayerUI.Instance.PlayerLevelText.text = $"Level: {level}";
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.LevelUp, 0.1f);
                Debug.Log($"LevelUp! Level: {level}");
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