using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Network
{
    public class Health : NetworkBehaviour
    {
        public float Value { get; private set; } = 100;


        public async UniTask DealDamageAsync(float damage, string token, ulong clientId)
        {
            Value -= damage;
            Debug.Log($"Object damaged. Damage: {damage}, CurrentValue: {Value}");

            if (Value <= 0)
            {
                Debug.Log("Object killed");

                HideTargetCanvasClientRpc();

                await HandleKillAsync(token, clientId);

                return;
            }

            UpdateTargetCanvasClientRpc(Value);

            return;
        }

        // FIXME: refactor and optimization!
        private async UniTask HandleKillAsync(string clientToken, ulong clientId)
        {
            var experience = await UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
            {
                characterId = 1,
                type = ExperienceTypeEnum.Combat
            }, clientToken);

            var progres = await QuestManager.Instance.CheckCharacterQuestProgresAsync(1, gameObject.name, 1, clientToken);

            if (progres.status != CharacterQuestStatusEnum.None)
            {
                UpdateQuestLogClientRpc(progres.characterQuestId, 1, progres.status, clientId);
            }

            // TODO: drop chance by enemy
            int random = UnityEngine.Random.Range(0, 99);

            if (random < 90)
            {
                var item = new InventoryItem
                {
                    type = CharacterInventoryTypeEnum.Can,
                    count = 1
                };

                await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterInventories", new AddCharacterInventoryItemCommand
                {
                    characterId = 1,
                    inventoryItem = item
                }, clientToken);

                progres = await QuestManager.Instance.CheckCharacterQuestProgresAsync(1, nameof(CharacterInventoryTypeEnum.Can), 1, clientToken);

                UpdateInventoryClientRpc(item, clientId);
            }

            if (experience.leveledUp)
            {
                Debug.Log($"LevelUp! Level: {experience.level}, SkillPoints: {experience.skillPoints}, Experience: {experience.experience}");

                UpdateLevelClientRpc(experience.level, clientId);
            }

            if (progres.status != CharacterQuestStatusEnum.None)
            {
                UpdateQuestLogClientRpc(progres.characterQuestId, 1, progres.status, clientId);
            }

            gameObject.GetComponent<NetworkObject>().Despawn();
        }

        [ClientRpc]
        private void UpdateInventoryClientRpc(InventoryItem item, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                UIManager.Instance.AddInventoryItem(item);
            }
        }

        [ClientRpc]
        private void HideTargetCanvasClientRpc()
        {
            UIManager.Instance.Target.SetActive(false);
        }

        [ClientRpc]
        public void UpdateLevelClientRpc(int level, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                UIManager.Instance.PlayerLevelText.text = $"Level: {level}";
                AudioManager.Instance.PlayOneShot(AudioTypeEnum.LevelUp, 0.4f);
            }
        }

        [ClientRpc]
        private void UpdateTargetCanvasClientRpc(float value)
        {
            Debug.Log("Updating target UI");

            Value = value;
            UIManager.Instance.TargetHealthPointsText.text = Value.ToString();
        }

        [ClientRpc]
        private void UpdateQuestLogClientRpc(int characterQuestId, int progres, CharacterQuestStatusEnum status, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                Debug.Log($"UpdateQuestLogClientRpc: {clientId}");

                var characterQuest = QuestManager.Instance.CharacterQuests
                    .Where(x => x.id == characterQuestId)
                    .Single();

                characterQuest.progress += progres;
                characterQuest.status = status;

                QuestManager.Instance.AddedProgresEvent.Invoke(characterQuest.questId, characterQuest.status);
            }
        }
    }
}