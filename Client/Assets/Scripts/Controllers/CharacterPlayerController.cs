using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class CharacterPlayerController : NetworkBehaviour, IStartable, ITickable
{
    [SerializeField] private float MaxCastDistance = 20.0f;
    [SerializeField] private GameObject FireballPrefab;

    private Transform SelectedTargetTransform;
    private static Renderer _currentlySelectedRenderer = null;
    private static Color _originalSelectedColor;
    private bool _isCasting = false;
    private float _castTime = 1.5f;
    private float _castTimer = 0f;
    private ulong _objectId = 0;
    private bool _isInterrupted = false;
    private float _interruptDuration = 0.2f;
    private float _interruptTimer = 0f;
    private Color _originalBarColor;
    private Image _castProgressBar;
    private float _timer;
    private const float SaveInterval = 5f;

    [Inject] private readonly ICharacterService _characterService;
    [Inject] private readonly ITokenManagerService _tokenManagerService;
    [Inject, Key("PlayerCanvas")] private readonly GameObject _playerCanvas;
    [Inject, Key("TargetCanvas")] private readonly GameObject _targetCanvas;
    [Inject, Key("ProgressBarCanvas")] private readonly GameObject _progressBarCanvas;

    public async void Start()
    {
        if (IsOwner)
        {
            _castProgressBar = _progressBarCanvas.transform.Find("ProgressBar").GetComponent<Image>();

            // todo: this should be hidden by default 
            HideCastBar();

            await UniTask.WhenAll(LoadInfoAsync(), LoadTransformAsync());
        }
    }

    public void Tick()
    {
        if (IsOwner)
        {
            CheckSaveTransform();
            HandleSelectionInput();
            UpdateInterrupt();
            HandleCastingInput();
            UpdateCasting();
        }
    }

    private void CheckSaveTransform()
    {   
        _timer += Time.deltaTime;

        if (_timer >= SaveInterval)
        {
            SaveTransformServerRpc(_tokenManagerService.Token);
            _timer = 0f;
        }
    }

    private async UniTask LoadInfoAsync()
    {
        var result = await _characterService.GetInfoAsync();

        _playerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>().text = result.name;
        _playerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>().text = result.health.ToString();
        _playerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {result.level}";
    }

    private async UniTask LoadTransformAsync()
    {
        var result = await _characterService.GetTransformAsync(_tokenManagerService.Token);

        transform.position = new Vector3(result.positionX, result.positionY, result.positionZ);
        transform.rotation = Quaternion.Euler(0, result.rotationY, 0);
    }

    [ServerRpc]
    private void SaveTransformServerRpc(string token)
    {
        _characterService.SaveTransformAsync(transform.position, transform.rotation.eulerAngles.y, token);
    }

    private void HandleSelectionInput()
    {
        var mouse = Mouse.current;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "Target")
            {
                if (_currentlySelectedRenderer != null)
                {
                    _currentlySelectedRenderer.material.color = _originalSelectedColor;
                    _targetCanvas.transform.Find("Target").gameObject.SetActive(false);
                }

                var newRenderer = hit.transform.GetComponent<Renderer>();
                _currentlySelectedRenderer = newRenderer;
                _originalSelectedColor = newRenderer.material.color;
                newRenderer.material.color = Color.green;
                SelectedTargetTransform = hit.transform;

                _targetCanvas.transform.Find("Target").gameObject.SetActive(true);
                _targetCanvas.transform.Find("Target/Name").GetComponent<TextMeshProUGUI>().text = "Bean";
                _targetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>().text = SelectedTargetTransform.GetComponent<TargetController>().Value.ToString();
            }
        }
    }

    private void StartCast()
    {
        _originalBarColor = _castProgressBar.color;
        SetFireball();
    }

    private void SetFireball()
    {
        var spawnPos = transform.position + Vector3.up * 1.0f;
        var targetPos = SelectedTargetTransform.position;
        var direction = (targetPos - spawnPos).normalized;

        SpawnProjectileServerRpc(spawnPos, direction, NetworkManager.Singleton.LocalClientId, _tokenManagerService.Token);
    }

    [ServerRpc]
    public void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, ulong clientId, string token)
    {
        var fireball = Instantiate(FireballPrefab, position, Quaternion.LookRotation(direction));
        var netObj = fireball.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId);
        var spawnedFireball = fireball.GetComponent<FireballController>();
        spawnedFireball.PreCast(token);

        NotifyClientRpc(netObj.NetworkObjectId, clientId);
    }

    [ServerRpc]
    public void CastServerRpc(ulong objectId, ulong clientId, ulong selectedTargetTransformObjectId)
    {
        var fireball = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].GetComponent<FireballController>();
        var selectedTargetTransform = NetworkManager.Singleton.SpawnManager.SpawnedObjects[selectedTargetTransformObjectId];
        fireball.Cast(selectedTargetTransform);
    }

    [ClientRpc]
    void NotifyClientRpc(ulong objectId, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            _isCasting = true;
            _castTimer = 0f;
            _objectId = objectId;
            ShowCastBar(0f);
        }
    }

    private void ShowCastBar(float progress)
    {
        if (_castProgressBar != null)
        {
            _progressBarCanvas.SetActive(true);
            _castProgressBar.fillAmount = Mathf.Clamp01(progress);
        }
    }

    private void HideCastBar()
    {
        if (_castProgressBar != null)
        {
            _progressBarCanvas.SetActive(false);
        }
    }

    private void HandleCastingInput()
    {
        if (SelectedTargetTransform != null && !_isCasting && !_isInterrupted &&
            Keyboard.current.digit1Key.wasPressedThisFrame &&
            CheckMaxDistance() && CheckLineOfSight() && CheckAngle())
        {
            StartCast();
        }
    }

    private void UpdateInterrupt()
    {
        if (!_isInterrupted)
            return;

        _interruptTimer += Time.deltaTime;
        if (_interruptTimer >= _interruptDuration)
        {
            _isInterrupted = false;
            _interruptTimer = 0f;
            HideCastBar();
            if (_castProgressBar != null)
                _castProgressBar.color = _originalBarColor;
        }
    }

    private void UpdateCasting()
    {
        if (!_isCasting)
            return;

        if (SelectedTargetTransform == null)
        {
            StopCasting();
            DespawnFireballServerRpc(_objectId);

            return;
        }

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed ||
            Keyboard.current.spaceKey.isPressed)
        {
            InterruptCast();
            return;
        }

        _castTimer += Time.deltaTime;
        ShowCastBar(_castTimer / _castTime);

        if (_castTimer >= _castTime)
        {
            StopCasting();

            var selectedTargetTransformObjectId = SelectedTargetTransform.GetComponent<NetworkObject>().NetworkObjectId;
            CastServerRpc(_objectId, NetworkManager.Singleton.LocalClientId, selectedTargetTransformObjectId);
        }
    }

    private void StopCasting()
    {
        _isCasting = false;
        _castTimer = 0f;
        HideCastBar();
    }

    private void InterruptCast()
    {
        _isCasting = false;
        _isInterrupted = true;
        _interruptTimer = 0f;
        FailedServerRpc(_objectId, NetworkManager.Singleton.LocalClientId);

        if (_castProgressBar != null)
        {
            _castProgressBar.color = Color.red;
            _castProgressBar.fillAmount = 1f;
            _progressBarCanvas.SetActive(true);
        }
    }

    [ServerRpc]
    private void FailedServerRpc(ulong objectId, ulong clientId)
    {
        var netObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId];
        var _fireball = netObj.GetComponent<FireballController>();

        _fireball.Failed();
    }

    [ServerRpc]
    private void DespawnFireballServerRpc(ulong objectId)
    {
        var netObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId];
        netObj.Despawn();
    }

    private bool CheckMaxDistance()
    {
        float distance = Vector3.Distance(transform.position, SelectedTargetTransform.position);
        var result = distance <= MaxCastDistance;

        Debug.Log($"CheckMaxDistance -> IsValid: {result}, Distance: {distance}, MaxCastDistance: {MaxCastDistance}");

        return result;
    }

    private bool CheckLineOfSight()
    {
        var origin = transform.position + Vector3.up * 1.0f;
        var direction = (SelectedTargetTransform.position - origin).normalized;
        var distance = Vector3.Distance(origin, SelectedTargetTransform.position);

        var result = Physics.Raycast(origin, direction, out RaycastHit hit, distance) && hit.transform == SelectedTargetTransform;

        Debug.Log($"CheckLineOfSight -> IsValid: {result}");

        return result;
    }

    private bool CheckAngle()
    {
        var toTarget = (SelectedTargetTransform.position - transform.position).normalized;
        var playerForward = transform.forward;
        var angle = Vector3.Angle(playerForward, toTarget);
        var result = angle < 90f;

        Debug.Log($"CheckAngle -> IsValid: {result}, Angle: {angle}");

        return result;
    }
}