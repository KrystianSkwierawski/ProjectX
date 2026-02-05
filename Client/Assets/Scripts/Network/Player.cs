using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
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
                    var experience = await UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
                    {
                        characterId = 1,
                        type = ExperienceTypeEnum.Combat
                    }, e.ClientToken);

                    if (experience.leveledUp)
                    {
                        Debug.Log($"LevelUp! Level: {experience.level}, SkillPoints: {experience.skillPoints}, Experience: {experience.experience}");

                        UpdateLevelClientRpc(experience.level, new ClientRpcParams
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

        private async UniTask GetCharacterAsync()
        {
            _character = await UnityWebRequestHelper.ExecuteGetAsync<CharacterDto>("Characters/1");

            PlayerUI.Instance.SetPlayer(_character);
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(int level, ClientRpcParams rpcParams = default)
        {
            PlayerUI.Instance.PlayerLevelText.text = $"Level: {level}";
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.LevelUp, 0.1f);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                AddExperienceSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }
        public override void OnDestroy()
        {
            if (IsServer)
            {
                AddExperienceSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnDestroy();
        }
    }
}