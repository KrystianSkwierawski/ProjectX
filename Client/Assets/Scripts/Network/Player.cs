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
                CombatManager.Instance.OnKillEvent.AddListener(async (KillEventModel killEvent) =>
                {
                    var experience = await UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
                    {
                        characterId = 1,
                        type = ExperienceTypeEnum.Combat
                    }, killEvent.ClientToken);

                    if (experience.leveledUp)
                    {
                        Debug.Log($"LevelUp! Level: {experience.level}, SkillPoints: {experience.skillPoints}, Experience: {experience.experience}");

                        UpdateLevelClientRpc(experience.level, killEvent.ClientId);
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
        public void UpdateLevelClientRpc(int level, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                UIManager.Instance.PlayerLevelText.text = $"Level: {level}";
                AudioManager.Instance.PlayOneShot(AudioTypeEnum.LevelUp, 0.3f);
            }
        }
    }
}