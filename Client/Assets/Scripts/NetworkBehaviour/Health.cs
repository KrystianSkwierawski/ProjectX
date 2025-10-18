using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class Health : NetworkBehaviour
{
    [SerializeField] private AudioClip _levelUpSfx;

    public float Value { get; private set; } = 100;

    private GameObject _targetCanvas;

    private void Start()
    {
        if (IsClient)
        {
            _targetCanvas = GameObject.Find("TargetCanvas");
        }
    }

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
        await AddExperienceAsync(token, clientId);

        // todo check quest
        if (gameObject.name == "bean")
        {
            var progres = await QuestManager.Instance.AddCharacterQuestProgresAsync(1, 1, token);

            UpdateQuestLogClientRpc(1, 1, progres.status, clientId);
        }

        gameObject.GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void HideTargetCanvasClientRpc()
    {
        _targetCanvas.transform.Find("Target").gameObject.SetActive(false);
    }

    [ClientRpc]
    private void UpdateQuestLogClientRpc(int characterQuestId, int progres, CharacterQuestStatusEnum status, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            var characterQuest = QuestManager.Instance.CharacterQuests
                .Where(x => x.id == characterQuestId)
                .Single();

            characterQuest.progress += progres;
            characterQuest.status = status;

            QuestManager.Instance.AddedProgresEvent.Invoke();
        }
    }

    [ClientRpc]
    private void UpdateLevelClientRpc(int level, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            GameObject.Find("PlayerCanvas").transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {level}";
            GameObject.Find("PlayerArmature").GetComponent<AudioSource>().PlayOneShot(_levelUpSfx, 0.4f);
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

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"AddExperience result: {request.result}");
        Debug.Log($"AddExperience text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<AddCharacterExperienceDto>(request.downloadHandler.text);

            if (result.leveledUp)
            {
                Debug.Log($"LevelUp! Level: {result.level}, SkillPoints: {result.skillPoints}, Experience: {result.experience}");

                UpdateLevelClientRpc(result.level, clientId);
            }
        }
    }
}
