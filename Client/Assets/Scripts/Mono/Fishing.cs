using System;
using Assets.Scripts.Enums;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Fishing : NetworkBehaviour
{
    private GameObject _fishingRod;
    private bool _isCasting = false;
    private float _castTime = 5f;
    private float _castTimer = 0f;
    private bool _isInterrupted = false;
    private float _interruptDuration = 0.2f;
    private float _interruptTimer = 0f;
    private Color _originalBarColor;
    private StarterAssetsInputs _input;
    private AudioSource _castingAudioSource;
    private GameObject _bait;

    // FIXME: refactor!!!
    private void Start()
    {
        if (IsOwner)
        {
            _input = GetComponent<StarterAssetsInputs>();
            _castingAudioSource = GetComponent<AudioSource>();
            _castingAudioSource.volume = 0.5f;
            _castingAudioSource.loop = true;
            _castingAudioSource.clip = AudioManager.Instance.AudioClips[AudioTypeEnum.FishCasting];
            _fishingRod = transform.Find("FishingRod").gameObject;
            _fishingRod.SetActive(false);
            _bait = GameObject.Find("Bait");

            _bait.SetActive(false);
        }
    }

    [ServerRpc]
    private void AddItemServerRpc(string clientToken)
    {
        // TODO: validations
        Debug.Log("UpdateInventorySubscription");
        UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
        {
            ClientToken = clientToken,
            GameObjectName = nameof(CharacterInventoryTypeEnum.Fish)
        });
    }

    private async void Update()
    {
        if (IsOwner)
        {
            CheckFishOut();
            UpdateInterrupt();
            await HandleInputAsync();
            UpdateCasting();
        }
    }

    private void CheckFishOut()
    {
        var mouse = Mouse.current;

        // TODO: validations
        if (_isCasting && mouse.rightButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "Bait")
            {
                Debug.Log("clicked");
                StopCasting();
                AddItemServerRpc(TokenManager.Instance.Token);
            }
        }
    }

    private async UniTask HandleInputAsync()
    {
        if (!_isCasting && !_isInterrupted && _input.Move == Vector2.zero && !_input.Jump && Keyboard.current.fKey.wasPressedThisFrame)
        {
            _fishingRod.SetActive(true);

            _originalBarColor = UIManager.Instance.CastProgressBar.color;
            _isCasting = true;
            _castTimer = _castTime;
            UIManager.Instance.ShowCastBar(_castTimer / _castTime);
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.FishCast, 0.5f);
            _bait.SetActive(true);
            await UniTask.Delay(TimeSpan.FromSeconds(1.091565));
            _castingAudioSource.Play();
        }
    }

    private void UpdateCasting()
    {
        if (!_isCasting)
        {
            return;
        }

        if (_input.Move != Vector2.zero || _input.Jump)
        {
            InterruptCast();
            return;
        }

        // decrease remaining time
        _castTimer -= Time.deltaTime;
        var normalized = (_castTime > 0f) ? (_castTimer / _castTime) : 0f;
        UIManager.Instance.ShowCastBar(Mathf.Clamp01(normalized));

        if (_castTimer <= 0f)
        {
            StopCasting();
        }
    }

    private void StopCasting()
    {
        _isCasting = false;
        _castTimer = 0f;
        UIManager.Instance.HideCastBar();
        _fishingRod.SetActive(false);
        _bait.SetActive(false);
        _castingAudioSource.Stop();
        AudioManager.Instance.PlayOneShot(AudioTypeEnum.FishingBobber, 1f);
    }

    private void InterruptCast()
    {
        _isCasting = false;
        _isInterrupted = true;
        _interruptTimer = 0f;
        _fishingRod.SetActive(false);
        _bait.SetActive(false);

        UIManager.Instance.FailCastBar();
        _castingAudioSource.Stop();
        AudioManager.Instance.PlayOneShot(AudioTypeEnum.CastingFailed, 0.5f);
    }

    private void UpdateInterrupt()
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
            UIManager.Instance.HideCastBar();

            if (UIManager.Instance.CastProgressBar != null)
            {
                UIManager.Instance.CastProgressBar.color = _originalBarColor;
            }
        }
    }
}
