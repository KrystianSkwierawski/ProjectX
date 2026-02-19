using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mining : NetworkBehaviour
{
    private const float _maxDistance = 2f;

    private GameObject _picaxe;

    private StarterAssetsInputs _input;

    private readonly NetworkVariable<bool> _active =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private Color _originalBarColor;
    private bool _isMining = false;
    private float _miningTime = 3f;
    private float _miningTimer = 0f;
    private float _sfxTimer = 0f;
    private float _sfxTime = 1f;
    private bool _isInterrupted = false;
    private float _interruptDuration = 0.2f;
    private float _interruptTimer = 0f;
    private GameObject _target;
    private ThirdPersonController _thirdPersonController;

    public override void OnNetworkSpawn()
    {
        SetActive(_active.Value);
        _active.OnValueChanged += OnSetActiveChanged;
        base.OnNetworkSpawn();
    }

    private void Awake()
    {
        _picaxe = transform.Find("Picaxe").gameObject;
    }

    private void Start()
    {
        if (IsOwner)
        {
            _input = GetComponent<StarterAssetsInputs>();
            _thirdPersonController = GetComponent<ThirdPersonController>();
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            CheckMiningSfx();
            CheckInput();
            CheckMining();
            CheckInterrupt();
        }
    }

    private void CheckMining()
    {
        if (!_isMining)
        {
            return;
        }

        if (_input.Move != Vector2.zero || _input.Jump)
        {
            InterruptCast();
            return;
        }

        _miningTimer += Time.deltaTime;
        PlayerUI.Instance.ShowCastBar(_miningTimer / _miningTime);

        if (_miningTimer >= _miningTime)
        {
            ProcessServerRpc((NetworkObjectReference)_target.GetComponent<NetworkObject>(), UserManager.Instance.Token);
            StopMining();
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MinedOre, 0.3f);
        }
    }

    private void CheckMiningSfx()
    {
        if (!_isMining)
        {
            return;
        }

        _sfxTimer += Time.deltaTime;

        if (_sfxTimer >= _sfxTime)
        {
            _sfxTimer -= _sfxTime;
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Mining, 0.5f);
        }
    }

    private void CheckInput()
    {
        if (_isMining)
        {
            return;
        }

        var mouse = Mouse.current;

        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

        var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "Rock";

        if (!hover)
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        if (Vector3.Distance(transform.position, hit.transform.position) > _maxDistance)
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        CursorUI.Instance.ShowPointer();

        if (mouse.rightButton.wasPressedThisFrame)
        {
            _target = hit.transform.gameObject;
            StartMining();
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Mining, 0.5f);
        }
    }

    private void CheckInterrupt()
    {
        if (!_isInterrupted)
        {
            return;
        }

        _interruptTimer += Time.deltaTime;

        if (_interruptTimer >= _interruptDuration)
        {
            _isInterrupted = false;
            _interruptTimer = 0f;
            PlayerUI.Instance.HideCastBar();
            PlayerUI.Instance.CastProgressBar.color = _originalBarColor;
        }
    }

    private void StartMining()
    {
        SetActiveServerRpc(true);

        _originalBarColor = PlayerUI.Instance.CastProgressBar.color;
        _isMining = true;
        _miningTimer = 0f;
        _sfxTimer = 0f;

        PlayerUI.Instance.ShowCastBar(_miningTimer / _miningTime);
        _thirdPersonController.LockCameraToTarget(_target.transform);
    }

    private void StopMining()
    {
        SetActiveServerRpc(false);

        _target = null;
        _isMining = false;
        _miningTimer = 0f;
        _sfxTimer = 0f;
        PlayerUI.Instance.HideCastBar();
        _thirdPersonController.UnlockCamera();
    }

    private void InterruptCast()
    {
        _isMining = false;
        _isInterrupted = true;
        _interruptTimer = 0f;

        PlayerUI.Instance.FailCastBar();
        AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.CastingFailed, 0.1f);

        SetActiveServerRpc(false);
    }

    [ServerRpc]
    public void SetActiveServerRpc(bool value)
    {
        _active.Value = value;
    }

    [ServerRpc]
    private void ProcessServerRpc(NetworkObjectReference networkObjectRef, string clientToken)
    {
        // TODO: validation
        if (networkObjectRef.TryGet(out NetworkObject networkObject))
        {
            var gameObject = networkObject.gameObject;

            CheckLootSubscription.Instance.Invoke(OwnerClientId.ToString(), new CheckLootSubscriptionEvent
            {
                GameObjectName = gameObject.name,
            });

            UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
            {
                characterId = 1,
                amount = 50,
                type = ExperienceTypeEnum.Mining
            }, clientToken).Forget();

            ReleasePoolSubscription.Instance.Invoke(gameObject.GetInstanceID().ToString(), new ReleasePoolSubscriptionEvent());
        }
    }

    private void SetActive(bool value)
    {
        _picaxe.SetActive(value);
    }

    private void OnSetActiveChanged(bool prev, bool next)
    {
        SetActive(next);
    }

    public override void OnNetworkDespawn()
    {
        _active.OnValueChanged -= OnSetActiveChanged;
        base.OnNetworkDespawn();
    }
}