using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class Health : NetworkBehaviour
{
    [SerializeField] private AudioClip _levelUpSfx;

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

    private async UniTask HandleKillAsync(string token, ulong clientId)
    {
        var experience = await ExperienceManager.Instance.AddExperienceAsync(ExperienceTypeEnum.Combat, token, clientId);
        var progres = await QuestManager.Instance.CheckCharacterQuestProgresAsync(1, gameObject.name, 1, token);

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
            GameObject.Find("PlayerArmature").GetComponent<AudioSource>().PlayOneShot(_levelUpSfx, 0.4f);
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
