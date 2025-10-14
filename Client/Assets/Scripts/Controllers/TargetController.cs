using System;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TargetController : NetworkBehaviour, IStartable, ITickable
{
    public AudioClip LevelUpSfx;
    public float Value { get; private set; } = 100; // todo: network variable


    public float PatrolRadius = 5f;
    public float MoveSpeed = 2f;
    public float PointTolerance = 0.2f;
    private Vector3 _startPosition;
    private Vector3 _currentTarget;
    private bool _isWaiting = false;

    [Inject] private readonly IExperienceService _experienceService;
    [Inject, Key("PlayerCanvas")] private readonly GameObject _playerCanvas;
    [Inject, Key("TargetCanvas")] private readonly GameObject _targetCanvas;

    protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
    {
        base.OnNetworkPreSpawn(ref networkManager);

        FindFirstObjectByType<MainLifetimeScope>().Container.InjectGameObject(gameObject);
    }

    public void Start()
    {
        if (IsClient)
        {
            _startPosition = transform.position;
        }
    }

    public async void Tick()
    {
        if (IsServer && !_isWaiting)
        {
            await PatrolAsync();
        }
    }

    public async UniTask<bool> DealDamageAsync(float value, string token, ulong clientId)
    {
        Value -= value;

        Debug.Log($"ODealDamageAsync -> Value: {value}, CurrentValue: {Value}");

        if (Value <= 0)
        {
            Debug.Log("Object killed");

            HideTargetCanvasClientRpc();

            await HandleKillAsync(token, clientId);

            return true;
        }

        if (_experienceService is null)
        {
            Debug.LogError("Experience service on server is null");
        }

        if (_targetCanvas is null)
        {
            Debug.LogError("_targetCanvas on server is null");
        }

        UpdateTargetCanvasClientRpc(Value);

        return true;
    }

    private async UniTask HandleKillAsync(string token, ulong clientId)
    {
        var result = await _experienceService.Add(token, clientId);

        if (result.leveledUp)
        {
            UpdateLevelClientRpc(result.level, clientId);
        }

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
            _playerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {level}";
            GameObject.Find("PlayerArmature").GetComponent<AudioSource>().PlayOneShot(LevelUpSfx, 0.4f);
        }
    }

    [ClientRpc]
    private void UpdateTargetCanvasClientRpc(float value)
    {
        Debug.Log("Updating target UI");

        Value = value;

        if (_experienceService is null)
        {
            Debug.LogError("Experience service on client is null");
        }

        if (_targetCanvas is null)
        {
            Debug.LogError("_targetCanvas on client is null");
        }

        _targetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>().text = Value.ToString();
    }

    private async UniTask PatrolAsync()
    {
        Vector3 direction = (_currentTarget - transform.position).normalized;
        transform.position += direction * MoveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _currentTarget) < PointTolerance)
        {
            _isWaiting = true;

            var delay = new System.Random().Next(1, 5);

            await UniTask.Delay(TimeSpan.FromSeconds(delay));

            PickNewPatrolPoint();

            _isWaiting = false;
        }
    }

    private void PickNewPatrolPoint()
    {
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * PatrolRadius;
        _currentTarget = _startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
}
