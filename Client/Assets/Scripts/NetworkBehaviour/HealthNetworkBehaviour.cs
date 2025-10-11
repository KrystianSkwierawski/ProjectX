using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class HealthNetworkBehaviour : NetworkBehaviour
{
    [Inject] private readonly IExperienceService _experienceService;

    public NetworkVariable<float> Health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
        Health.Value -= damage;
        Debug.Log($"Object damaged. Damage: {damage}, CurrentValue: {Health}");

        if (Health.Value <= 0)
        {
            Debug.Log("Object killed");

            HideTargetCanvasClientRpc();

            var result = await _experienceService.AddExperienceAsync(token);

            if (result.leveledUp)
            {
                UpdateLevelClientRpc(result.level, clientId);
            }

            gameObject.GetComponent<NetworkObject>().Despawn();

            return true;
        }

        UpdateTargetCanvasClientRpc();
        return true;
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
    private void UpdateTargetCanvasClientRpc()
    {
        Debug.Log("Updating target UI");

        _targetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>().text = Health.Value.ToString();
    }
}
