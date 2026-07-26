using System.Collections.Generic;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Professions.Mono
{
    public class Herbalism : NetworkBehaviour
    {
        private readonly IDictionary<string, byte> _requiredLevels = new Dictionary<string, byte>
        {
            { "Chamomile(Clone)", 1 },
        };

        private const float _maxDistance = 2f;

        private Color _originalBarColor;
        private bool _isCasting = false;
        private float _castingTime = 2f;
        private float _castingTimer = 0f;
        private float _sfxTimer = 0f;
        private float _sfxTime = 2f;
        private bool _isInterrupted = false;
        private float _interruptDuration = 0.2f;
        private float _interruptTimer = 0f;
        private GameObject _target;
        private ThirdPersonController _thirdPersonController;

        private void Start()
        {
            if (IsOwner)
            {
                _thirdPersonController = GetComponent<ThirdPersonController>();
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                //CheckSfx();
                CheckInput();
                CheckMining();
                CheckInterrupt();
            }
        }

        private void CheckMining()
        {
            if (!_isCasting)
            {
                return;
            }

            if (_thirdPersonController.Input.Move != Vector2.zero || _thirdPersonController.Input.Jump)
            {
                InterruptCast();
                return;
            }

            _castingTimer += Time.deltaTime;
            PlayerUI.Instance.UpdateCastBar(_castingTimer / _castingTime);

            if (_castingTimer >= _castingTime)
            {
                ProcessServerRpc((NetworkObjectReference)_target.GetComponent<NetworkObject>(), UserManager.Instance.Token);
                StopHerbalism();
            }
        }

        private void CheckInput()
        {
            if (_isCasting)
            {
                return;
            }

            var mouse = Mouse.current;

            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

            var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "Herbalism";

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

            if (mouse.rightButton.wasPressedThisFrame && HasRequiredLevel(hit.transform.gameObject.name))
            {
                _target = hit.transform.gameObject;
                StartHerbalism();
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Herbalism, 0.5f);
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

        private void StartHerbalism()
        {
            _originalBarColor = PlayerUI.Instance.CastProgressBar.color;
            _isCasting = true;
            _castingTimer = 0f;
            _sfxTimer = 0f;

            PlayerUI.Instance.UpdateCastBar(_castingTimer / _castingTime);
            _thirdPersonController.LockCameraToTarget(_target.transform, -15f);
        }

        private void StopHerbalism()
        {
            _target = null;
            _isCasting = false;
            _castingTimer = 0f;
            _sfxTimer = 0f;
            PlayerUI.Instance.HideCastBar();
            _thirdPersonController.UnlockCamera();
        }

        private void InterruptCast()
        {
            _isCasting = false;
            _isInterrupted = true;
            _interruptTimer = 0f;

            PlayerUI.Instance.FailCastBar();
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.CastingFailed, 0.1f);
        }

        [ServerRpc]
        private void ProcessServerRpc(NetworkObjectReference networkObjectRef, string clientToken)
        {
            // TODO: validation
            if (networkObjectRef.TryGet(out NetworkObject networkObject) && HasRequiredLevel(networkObject.gameObject.name))
            {
                var gameObject = networkObject.gameObject;

                CheckLootSubscription.Instance.Invoke(OwnerClientId.ToString(), new CheckLootSubscriptionEvent
                {
                    GameObjectName = gameObject.name,
                });

                AddExperienceSubscription.Instance.Invoke(OwnerClientId.ToString(), new AddExperienceSubscriptionEvent
                {
                    Amount = 50,
                    Type = ExperienceTypeEnum.Herbalism,
                    ClientToken = clientToken
                });

                ReleasePoolSubscription.Instance.Invoke(gameObject.GetInstanceID().ToString(), new ReleasePoolSubscriptionEvent());
            }
        }

        private bool HasRequiredLevel(string objectName)
        {
            if (_requiredLevels.TryGetValue(objectName, out byte requiredLevel))
            {
                var level = UserManager.Instance.Characters[OwnerClientId].Levels[ExperienceTypeEnum.Herbalism];

                if (level < requiredLevel)
                {
                    var message = string.Format(TranslateManager.Instance.GetByKey(TranslateKeyEnum.ProfessionLevelRequired), requiredLevel, level);

                    if (IsOwner)
                    {
                        LogUI.Instance.ShowAsync(message, color: ColorUI.Red).Forget();

                        AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.CastingFailed, 0.1f);
                    }

                    Debug.Log(message);

                    return false;
                }
            }

            return true;
        }
    }
}
