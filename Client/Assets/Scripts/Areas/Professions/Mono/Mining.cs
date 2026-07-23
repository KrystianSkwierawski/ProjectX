using System.Collections.Generic;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Mono;
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
    public class Mining : NetworkBehaviour
    {
        private readonly IDictionary<string, byte> _requiredLevels = new Dictionary<string, byte>
        {
            { "CopperRock(Clone)", 1 },
            { "BlackRock(Clone)", 2 },
            { "WhiteRock(Clone)", 3 },
            { "PurpleRock(Clone)", 4 },
        };

        private const float _maxDistance = 2f;

        private GameObject _picaxe;

        private readonly NetworkVariable<bool> _active =
            new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        private Color _originalBarColor;
        private bool _isCasting = false;
        private float _castingTime = 3f;
        private float _castingTimer = 0f;
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
                _thirdPersonController = GetComponent<ThirdPersonController>();
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                CheckSfx();
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
                StopMining();
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MinedOre, 0.1f);
            }
        }

        private void CheckSfx()
        {
            if (!_isCasting)
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
            if (_isCasting)
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

            if (mouse.rightButton.wasPressedThisFrame && HasRequiredLevel(hit.transform.gameObject.name))
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
            _isCasting = true;
            _castingTimer = 0f;
            _sfxTimer = 0f;

            PlayerUI.Instance.UpdateCastBar(_castingTimer / _castingTime);
            _thirdPersonController.LockCameraToTarget(_target.transform, 0f);
        }

        private void StopMining()
        {
            SetActiveServerRpc(false);

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
            // TODO: validation position, distance, etc
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
                    Type = ExperienceTypeEnum.Mining,
                    ClientToken = clientToken
                });

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

        private bool HasRequiredLevel(string objectName)
        {
            if (_requiredLevels.TryGetValue(objectName, out byte requiredLevel))
            {
                var level = UserManager.Instance.Characters[OwnerClientId].Levels[ExperienceTypeEnum.Mining];

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
