using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Target : NetworkBehaviour
{
    public float MaxCastDistance = 10.0f;
    public GameObject FireballPrefab;
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
    private GameObject _progressBarCanvas;
    private GameObject _targetCanvas;

    private void Start()
    {
        if (IsOwner)
        {
            _progressBarCanvas = GameObject.Find("ProgressBarCanvas");
            _castProgressBar = GameObject.Find("ProgressBar").GetComponent<Image>();
            _targetCanvas = GameObject.Find("TargetCanvas");

            HideCastBar();
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            HandleSelectionInput();
            UpdateInterrupt();
            HandleCastingInput();
            UpdateCasting();
        }
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
                _targetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>().text = SelectedTargetTransform.GetComponent<Health>().Value.ToString();
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

        SpawnProjectileServerRpc(spawnPos, direction, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = true)]
    public void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, ulong clientId)
    {
        var fireball = Instantiate(FireballPrefab, position, Quaternion.LookRotation(direction));
        var netObj = fireball.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId);
        var spawnedFireball = fireball.GetComponent<Fireball>();
        spawnedFireball.PreCast();

        NotifyClientRpc(netObj.NetworkObjectId, clientId);
    }

    [ServerRpc(RequireOwnership = true)]
    public void CastServerRpc(ulong objectId, ulong clientId, ulong selectedTargetTransformObjectId)
    {
        var fireball = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].GetComponent<Fireball>();

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
            _isCasting = false;
            _castTimer = 0f;
            HideCastBar();
            var selectedTargetTransformObjectId = SelectedTargetTransform.GetComponent<NetworkObject>().NetworkObjectId;
            CastServerRpc(_objectId, NetworkManager.Singleton.LocalClientId, selectedTargetTransformObjectId);
        }
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
        var _fireball = netObj.GetComponent<Fireball>();

        _fireball.Failed();
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
