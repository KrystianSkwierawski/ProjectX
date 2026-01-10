using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Network
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private UIManager _uiManager;

        private async void Start()
        {
            if (IsOwner)
            {
                await GetCharacterAsync();
            }

            if (IsServer)
            {
                ExperienceSubscription.Instance.Subscribe(OwnerClientId.ToString(), async (e) =>
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
                                TargetClientIds = new ulong[] { ulong.Parse(e.Key) }
                            }
                        });
                    }
                });
            }
        }

        private async UniTask GetCharacterAsync()
        {
            var result = await UnityWebRequestHelper.ExecuteGetAsync<CharacterDto>("Characters/1");

            UIManager.Instance.SetPlayer(result.name, result.health.ToString(), result.level.ToString());
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(int level, ClientRpcParams rpcParams = default)
        {
            UIManager.Instance.PlayerLevelText.text = $"Level: {level}";
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.LevelUp, 0.3f);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                ExperienceSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }
    }
}