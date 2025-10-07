using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class Health : NetworkBehaviour
{
    public float Value { get; private set; } = 100;

    private GameObject _targetCanvas;
    private GameObject _playerCanvas;

    private void Start()
    {
        if (IsClient)
        {
            _targetCanvas = GameObject.Find("TargetCanvas");
            _playerCanvas = GameObject.Find("PlayerCanvas");
        }
    }

    public bool DealDamage(float damage, string token)
    {
        Value -= damage;
        Debug.Log($"Object damaged. Damage: {damage}, CurrentValue: {Value}");

        if (Value <= 0)
        {
            Debug.Log("Object killed");
            HideTargetCanvasClientRpc();
            StartCoroutine(HandleKill(token));
            return true;
        }

        UpdateTargetCanvasClientRpc(Value);
        return true;
    }

    private IEnumerator HandleKill(string token)
    {
        yield return AddExperience(token);
        gameObject.GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void HideTargetCanvasClientRpc()
    {
        _targetCanvas.transform.Find("Target").gameObject.SetActive(false);
    }

    [ClientRpc]
    private void UpdateLevelClientRpc(int level)
    {
        // todo: only owner
        _playerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {level}";
        //GetComponent<AudioSource>().PlayOneShot(LevelUpSfx); // todo: play on owner player
    }

    [ClientRpc]
    private void UpdateTargetCanvasClientRpc(float value)
    {
        Debug.Log("Updating target UI");

        Value = value;
        _targetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>().text = Value.ToString();
    }

    private IEnumerator AddExperience(string token)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterExperiences", JsonUtility.ToJson(new AddCharacterExperienceCommand
        {
            amount = 1000,
            type = ExperienceTypeEnum.Combat,
            clientToken = token
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {ServerTokenManager.Instance.Token}");

        yield return request.SendWebRequest();

        Debug.Log($"AddExperience result: {request.result}");
        Debug.Log($"AddExperience text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<AddCharacterExperienceDto>(request.downloadHandler.text);

            if (result.leveledUp)
            {
                Debug.Log($"LevelUp! Level: {result.level}, SkillPoints: {result.skillPoints}, Experience: {result.experience}");
                // todo: notify player
                UpdateLevelClientRpc(result.level);
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
