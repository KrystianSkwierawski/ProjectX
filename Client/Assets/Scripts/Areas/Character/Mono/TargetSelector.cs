using System;
using System.Collections.Generic;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Shared.Mono;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class TargetSelector : NetworkBehaviour
    {
        [SerializeField] private GameObject _fireballPrefab;
        [SerializeField] private GameObject _arrowPrefab;
        [SerializeField] private GameObject _swordPrefab;

        private Renderer _currentlySelectedRenderer;
        private Color _originalSelectedColor;
        private string _selectedTargetSubscriptionKey;

        private bool _isCasting = false;

        private float _castTime = 1.5f;
        private float _castTimer = 0f;

        private GameObject _selectedTarget;

        private ObjectPool<GameObject> _fireballPool;
        private ObjectPool<GameObject> _arrowPool;
        private ObjectPool<GameObject> _swordPool;

        private GameObject _currentWeapon;
        private readonly List<GameObject> _weapons = new();
        private bool _onlyView;

        private ThirdPersonController _thirdPersonController;

        private void Start()
        {
            if (IsOwner)
            {
                PlayerUI.Instance.HideCastBar();
                _thirdPersonController = GetComponent<ThirdPersonController>();
            }

            if (IsServer)
            {
                _fireballPool = new ObjectPool<GameObject>(
                    createFunc: () => CreateWeapon(_fireballPrefab, weapon => _fireballPool.Release(weapon)),
                    actionOnGet: (GameObject gameObject) =>
                    {
                        gameObject.SetActive(true);

                        var spawnPos = transform.position + Vector3.up * 1.0f;
                        var targetPos = _selectedTarget.transform.position;
                        var direction = (targetPos - spawnPos).normalized;

                        gameObject.GetComponent<AbstractWeapon>().SetPositionAndDirection(spawnPos, direction);

                        var networkObject = gameObject.GetComponent<NetworkObject>();
                        networkObject.SpawnWithOwnership(OwnerClientId);
                    },
                    actionOnRelease: (GameObject gameObject) =>
                    {
                        gameObject.GetComponent<NetworkObject>().Despawn(false);
                        gameObject.SetActive(false);
                    }
                );

                _arrowPool = new ObjectPool<GameObject>(
                    createFunc: () => CreateWeapon(_arrowPrefab, weapon => _arrowPool.Release(weapon)),
                    actionOnGet: (GameObject gameObject) =>
                    {
                        gameObject.SetActive(true);

                        var spawnPos = transform.position + Vector3.up * 1.0f;
                        var targetPos = _selectedTarget.transform.position;
                        var direction = (targetPos - spawnPos).normalized;

                        gameObject.GetComponent<AbstractWeapon>().SetPositionAndDirection(spawnPos, direction);

                        var networkObject = gameObject.GetComponent<NetworkObject>();
                        networkObject.SpawnWithOwnership(OwnerClientId);
                    },
                    actionOnRelease: (GameObject gameObject) =>
                    {
                        gameObject.GetComponent<NetworkObject>().Despawn(false);
                        gameObject.SetActive(false);
                    }
                );

                _swordPool = new ObjectPool<GameObject>(
                    createFunc: () => CreateWeapon(_swordPrefab, weapon => _swordPool.Release(weapon)),
                    actionOnGet: (GameObject gameObject) =>
                    {
                        gameObject.SetActive(true);

                        var spawnPos = transform.position + Vector3.up * 1.0f;
                        var targetPos = _selectedTarget.transform.position;
                        var direction = (targetPos - spawnPos).normalized;

                        gameObject.GetComponent<AbstractWeapon>().SetPositionAndDirection(spawnPos, direction);

                        var networkObject = gameObject.GetComponent<NetworkObject>();
                        networkObject.SpawnWithOwnership(OwnerClientId);
                    },
                    actionOnRelease: (GameObject gameObject) =>
                    {
                        gameObject.GetComponent<NetworkObject>().Despawn(false);
                        gameObject.SetActive(false);
                    }
                );
            }
        }

        private GameObject CreateWeapon(GameObject prefab, Action<GameObject> release)
        {
            var weapon = Instantiate(prefab);
            _weapons.Add(weapon);
            weapon.GetComponent<AbstractWeapon>().SetReleaseHandler(finished =>
            {
                if (_currentWeapon == finished)
                {
                    _currentWeapon = null;
                }

                release(finished);
            });

            return weapon;
        }

        private void Update()
        {
            if (IsOwner && UserManager.Instance.Characters.ContainsKey(OwnerClientId)
                && !GetComponent<DungeonTravel>().IsTransitioning)
            {
                CheckCurrentTarget();

                HandleSelectionInput();

                CheckCasting();

                UpdateCasting();
            }
        }

        private void CheckCurrentTarget()
        {
            if (_isCasting && _selectedTarget != null && !IsValidTarget(_selectedTarget.transform))
            {
                HandleUnselect();
                UnselectServerRpc();
            }
        }

        private void HandleSelectionInput()
        {
            var mouse = Mouse.current;

            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

            var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "Target";

            if (!hover)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            if (!IsValidTarget(hit.transform))
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            CursorUI.Instance.ShowPointer();

            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                _onlyView = mouse.leftButton.wasPressedThisFrame || (!_onlyView && mouse.rightButton.wasPressedThisFrame && hit.transform.gameObject == _selectedTarget);

                if (_currentlySelectedRenderer != null)
                {
                    HandleUnselect();
                    UnselectServerRpc();
                }

                _currentlySelectedRenderer = hit.transform.GetComponent<Renderer>();

                if (_currentlySelectedRenderer != null)
                {
                    _originalSelectedColor = _currentlySelectedRenderer.material.color;
                    _currentlySelectedRenderer.material.color = ColorUI.Green;
                }

                _selectedTarget = hit.transform.gameObject;

                if (!_onlyView)
                {
                    _thirdPersonController.LockCameraToTarget(_selectedTarget.transform);
                }

                TargetUI.Instance.SetTarget("Bean", _selectedTarget.GetComponent<Health>().Network.Value.ToString());
                SelectServerRpc((NetworkObjectReference)_selectedTarget.GetComponent<NetworkObject>());
            }
        }

        public void HandleUnselect()
        {
            StopCasting(failed: true);
            _thirdPersonController.UnlockCamera();

            if (_currentlySelectedRenderer != null)
            {
                _currentlySelectedRenderer.material.color = _originalSelectedColor;
            }

            _currentlySelectedRenderer = null;
            _selectedTarget = null;
            TargetUI.Instance.Target.SetActive(false);
        }

        [ServerRpc]
        private void SelectServerRpc(NetworkObjectReference selectedTargetObjectRef)
        {
            ClearTargetSubscription();
            _selectedTarget = null;

            if (DungeonTravel.CanInteract(OwnerClientId)
                && selectedTargetObjectRef.TryGet(out var selectedTargetTransformObject)
                && DungeonWorldObject.SameInstance(gameObject, selectedTargetTransformObject.gameObject))
            {
                _selectedTarget = selectedTargetTransformObject.gameObject;
                _selectedTargetSubscriptionKey = $"{_selectedTarget.GetInstanceID()}_{OwnerClientId}";

                UpdateTargetSelectorSubscription.Instance.Subscribe(_selectedTargetSubscriptionKey, (e) =>
                {
                    UpdateTargetCanvasClientRpc(e.Value, e.Killed, OwnerClientId.ToClientRpcParams());

                    if (e.Killed)
                    {
                        UnselectTarget();
                    }
                });
            }
        }

        [ServerRpc]
        public void UnselectServerRpc()
        {
            UnselectTarget();
        }

        private void UnselectTarget()
        {
            ClearTargetSubscription();
            DespawnWeapon();
            _selectedTarget = null;
        }

        public void ResetForTravel()
        {
            if (IsServer)
            {
                UnselectTarget();
            }
        }

        private void ClearTargetSubscription()
        {
            if (string.IsNullOrEmpty(_selectedTargetSubscriptionKey))
            {
                return;
            }

            UpdateTargetSelectorSubscription.Instance.Unsubscribe(_selectedTargetSubscriptionKey);
            _selectedTargetSubscriptionKey = null;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                ClearTargetSubscription();

                foreach (var weapon in _weapons)
                {
                    if (weapon == null)
                    {
                        continue;
                    }

                    var networkObject = weapon.GetComponent<NetworkObject>();

                    if (networkObject.IsSpawned)
                    {
                        networkObject.Despawn();
                    }
                    else
                    {
                        Destroy(weapon);
                    }
                }

                _weapons.Clear();
                _currentWeapon = null;
            }

            if (_currentlySelectedRenderer != null)
            {
                _currentlySelectedRenderer.material.color = _originalSelectedColor;
            }

            _currentlySelectedRenderer = null;
            _selectedTarget = null;

            base.OnNetworkDespawn();
        }

        [ClientRpc]
        private void UpdateTargetCanvasClientRpc(float value, bool killed, ClientRpcParams rpcParams = default)
        {
            TargetUI.Instance.TargetHealthPointsText.text = value.ToString();

            if (killed)
            {
                HandleUnselect();
            }
        }

        private void StartCasting()
        {
            SpawnProjectileServerRpc();
        }

        [ServerRpc]
        public void SpawnProjectileServerRpc()
        {
            if (_currentWeapon != null)
            {
                return;
            }

            if (_selectedTarget == null
                || !DungeonTravel.CanInteract(OwnerClientId)
                || !DungeonWorldObject.SameInstance(gameObject, _selectedTarget)
                || !_selectedTarget.TryGetComponent(out NetworkObject selectedTargetNetworkObject)
                || !selectedTargetNetworkObject.IsSpawned)
            {
                UnselectTarget();

                return;
            }

            var character = UserManager.Instance.Characters[OwnerClientId];

            _currentWeapon = character.WeaponType.GetWeaponCategory() switch
            {
                WeaponCategoryEnum.Wand => _fireballPool.Get(),
                WeaponCategoryEnum.Bow => _arrowPool.Get(),
                WeaponCategoryEnum.Sword => _swordPool.Get(),
                _ => null
            };

            if (_currentWeapon == null)
            {
                Debug.LogError($"No weapon found");

                return;
            }

            _currentWeapon.GetComponent<AbstractWeapon>().StartCasting(_selectedTarget, gameObject, UserManager.Instance.GetPlayerSessionId(OwnerClientId));

            NotifyWeaponSpawnedClientRpc(OwnerClientId.ToClientRpcParams());
        }

        [ServerRpc]
        public void CastServerRpc()
        {
            if (_currentWeapon != null && DungeonTravel.CanInteract(OwnerClientId)
                && DungeonWorldObject.SameInstance(gameObject, _selectedTarget))
            {
                _currentWeapon.GetComponent<AbstractWeapon>().Cast();
                _currentWeapon = null;
            }
        }

        [ClientRpc]
        void NotifyWeaponSpawnedClientRpc(ClientRpcParams rpcParams = default)
        {
            _isCasting = true;
            _castTimer = 0f;
            PlayerUI.Instance.UpdateCastBar(_castTimer);
        }

        [ServerRpc]
        private void DespawnDespawnServerRpc()
        {
            DespawnWeapon();
        }

        private void CheckCasting()
        {
            var weaponCategory = UserManager.Instance.Characters[OwnerClientId].WeaponType.GetWeaponCategory();

            if (_selectedTarget == null || weaponCategory == WeaponCategoryEnum.None)
            {
                StopCasting(failed: true);

                return;
            }

            if (_isCasting && weaponCategory == WeaponCategoryEnum.Wand && (_thirdPersonController.Input.Move != Vector2.zero || _thirdPersonController.Input.Jump))
            {
                _onlyView = true;
                _thirdPersonController.UnlockCamera();
                StopCasting(failed: true);
                DespawnDespawnServerRpc();

                return;
            }

            if (_isCasting || _onlyView)
            {
                return;
            }

            if (IsValidTarget(_selectedTarget.transform))
            {
                StartCasting();
            }
        }

        private void UpdateCasting()
        {
            if (!_isCasting || _selectedTarget == null || _onlyView)
            {
                return;
            }

            var castingTime = UserManager.Instance.Characters[OwnerClientId].WeaponType.GetWeaponCategory() == WeaponCategoryEnum.Wand ? (_castTime * 2f) : _castTime;

            _castTimer += Time.deltaTime;

            PlayerUI.Instance.UpdateCastBar(_castTimer / castingTime);

            if (_castTimer >= castingTime)
            {
                StopCasting();

                CastServerRpc();
            }
        }

        private void StopCasting(bool failed = false)
        {
            if (!_isCasting)
            {
                return;
            }

            _isCasting = false;
            _castTimer = 0f;

            if (failed)
            {
                PlayerUI.Instance.FailCastBar(0.5f);
                AudioManager.Instance.TryPlayOneShot(Assets.Scripts.Areas.Shared.Enums.AudioTypeEnum.CastingFailed, 0.1f);
            }
            else
            {
                PlayerUI.Instance.HideCastBar();
            }
        }

        private void DespawnWeapon()
        {
            if (_currentWeapon != null)
            {
                _currentWeapon.GetComponent<AbstractWeapon>().Finish();

                _currentWeapon = null;
            }
        }

        private bool CheckMaxDistance(Transform selectedTransform)
        {
            float distance = Vector3.Distance(transform.position, selectedTransform.position);

            var maxCastDistance = UserManager.Instance.Characters[OwnerClientId].WeaponType.GetWeaponCategory() switch
            {
                WeaponCategoryEnum.Wand => 12f,
                WeaponCategoryEnum.Bow => 18f,
                WeaponCategoryEnum.Sword => 3f,
                _ => 0f
            };

            var result = distance <= maxCastDistance;

            Debug.Log($"CheckMaxDistance -> IsValid: {result}, Distance: {distance}, MaxCastDistance: {maxCastDistance}");

            return result;
        }

        private bool CheckLineOfSight(Transform selectedTransform)
        {
            var origin = transform.position + Vector3.up * 1.0f;
            var direction = (selectedTransform.position - origin).normalized;
            var distance = Vector3.Distance(origin, selectedTransform.position);

            var result = Physics.Raycast(origin, direction, out RaycastHit hit, distance) && hit.transform == selectedTransform;

            Debug.Log($"CheckLineOfSight -> IsValid: {result}");

            return result;
        }

        private bool CheckAngle(Transform selectedTransform)
        {
            var toTarget = (selectedTransform.position - transform.position).normalized;
            var playerForward = transform.forward;
            var angle = Vector3.Angle(playerForward, toTarget);
            var result = angle < 90f;

            Debug.Log($"CheckAngle -> IsValid: {result}, Angle: {angle}");

            return result;
        }

        private bool IsValidTarget(Transform selectedTransform)
        {
            return CheckMaxDistance(selectedTransform) &&
                //CheckLineOfSight(selectedTransform) &&
                CheckAngle(selectedTransform);
        }
    }
}
