using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.UI;
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

        private static Renderer _currentlySelectedRenderer = null;
        private static Color _originalSelectedColor;

        private bool _isCasting = false;

        private float _castTime = 1.5f;
        private float _castTimer = 0f;

        private GameObject _selectedTarget;

        private ObjectPool<GameObject> _fireballPool;
        private ObjectPool<GameObject> _arrowPool;
        private ObjectPool<GameObject> _swordPool;

        private GameObject _currentWeapon;
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
                    createFunc: () => Instantiate(_fireballPrefab),
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
                    createFunc: () => Instantiate(_arrowPrefab),
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
                    createFunc: () => Instantiate(_swordPrefab),
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

        private void Update()
        {
            if (IsOwner && UserManager.Instance.Characters.ContainsKey(OwnerClientId))
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

                var newRenderer = hit.transform.GetComponent<Renderer>();
                _currentlySelectedRenderer = newRenderer;
                _originalSelectedColor = newRenderer.material.color;
                newRenderer.material.color = ColorUI.Green;
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
            StopCasting();
            _thirdPersonController.UnlockCamera();

            _currentlySelectedRenderer.material.color = _originalSelectedColor;
            _selectedTarget = null;
            TargetUI.Instance.Target.SetActive(false);
        }

        [ServerRpc]
        private void SelectServerRpc(NetworkObjectReference selectedTargetObjectRef)
        {
            if (_selectedTarget != null)
            {
                UpdateTargetSelectorSubscription.Instance.Unsubscribe($"{_selectedTarget.GetInstanceID()}_{OwnerClientId}");
            }

            if (selectedTargetObjectRef.TryGet(out var selectedTargetTransformObject))
            {
                _selectedTarget = selectedTargetTransformObject.gameObject;

                UpdateTargetSelectorSubscription.Instance.Subscribe($"{_selectedTarget.GetInstanceID()}_{OwnerClientId}", (e) =>
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
            DespawnWeapon();
            _selectedTarget = null;
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
            if (_currentWeapon != null)
            {
                _currentWeapon.GetComponent<AbstractWeapon>().Cast();
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
                StopCasting();

                return;
            }

            if (_isCasting && weaponCategory == WeaponCategoryEnum.Wand && (_thirdPersonController.Input.Move != Vector2.zero || _thirdPersonController.Input.Jump))
            {
                _onlyView = true;
                _thirdPersonController.UnlockCamera();
                StopCasting();
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

        private void StopCasting()
        {
            _isCasting = false;
            _castTimer = 0f;
            PlayerUI.Instance.HideCastBar();
        }

        private void DespawnWeapon()
        {
            if (_currentWeapon != null)
            {
                switch (UserManager.Instance.Characters[OwnerClientId].WeaponType.GetWeaponCategory())
                {
                    case WeaponCategoryEnum.Wand:
                        _fireballPool.Release(_currentWeapon);
                        break;
                    case WeaponCategoryEnum.Bow:
                        _arrowPool.Release(_currentWeapon);
                        break;
                    case WeaponCategoryEnum.Sword:
                        _swordPool.Release(_currentWeapon);
                        break;
                }

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
