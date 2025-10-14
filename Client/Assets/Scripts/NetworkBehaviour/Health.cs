using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class Health : NetworkBehaviour
{
    public float Value { get; private set; } = 100;

    private GameObject _targetCanvas;
    public AudioClip LevelUpSfx;

    private void Start()
    {
        if (IsClient)
        {
            _targetCanvas = GameObject.Find("TargetCanvas");
        }
    }

    public async UniTask<bool> DealDamageAsync(float damage, string token, ulong clientId)
    {
        Value -= damage;
        Debug.Log($"Object damaged. Damage: {damage}, CurrentValue: {Value}");

        if (Value <= 0)
        {
            Debug.Log("Object killed");

            HideTargetCanvasClientRpc();

            await HandleKillAsync(token, clientId);

            return true;
        }

        UpdateTargetCanvasClientRpc(Value);
        return true;
    }

    private async UniTask HandleKillAsync(string token, ulong clientId)
    {
        await AddExperienceAsync(token, clientId);
        gameObject.GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void HideTargetCanvasClientRpc()
    {
        _targetCanvas.transform.Find("Target").gameObject.SetActive(false);
    }

    [ClientRpc]
    private void UpdateLevelClientRpc(int level, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            GameObject.Find("PlayerCanvas").transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {level}";
            GameObject.Find("PlayerArmature").GetComponent<AudioSource>().PlayOneShot(LevelUpSfx, 0.4f);
        }
    }

    [ClientRpc]
    private void UpdateTargetCanvasClientRpc(float value)
    {
        Debug.Log("Updating target UI");

        Value = value;
        _targetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>().text = Value.ToString();
    }

    private async UniTask AddExperienceAsync(string token, ulong clientId)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterExperiences", JsonUtility.ToJson(new AddCharacterExperienceCommand
        {
            amount = 1000,
            type = ExperienceTypeEnum.Combat,
            clientToken = token
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {ServerTokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"AddExperience result: {request.result}");
        Debug.Log($"AddExperience text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<AddCharacterExperienceDto>(request.downloadHandler.text);

            if (result.leveledUp)
            {
                Debug.Log($"LevelUp! Level: {result.level}, SkillPoints: {result.skillPoints}, Experience: {result.experience}");
                // todo: notify player
                UpdateLevelClientRpc(result.level, clientId);
            }
        }
    }

    [Serializable]
    private class AddCharacterExperienceCommand
    {
        public int amount;

        public ExperienceTypeEnum type;

        public string clientToken;
    }

    [Serializable]
    private class AddCharacterExperienceDto
    {
        public byte level;

        public byte skillPoints;

        public int experience;

        public bool leveledUp;
    }

    private enum ExperienceTypeEnum : byte
    {
        None,

        Combat,

        Crafting,

        Gathering,

        Exploration,

        Questing,

        Trading,

        Survival,

        Technology,

        Healing,

        Building,

        Farming,

        Fishing,

        Cooking,

        Alchemy,

        Enchanting,
    }
}
