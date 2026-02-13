using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Assets.Scripts.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using StarterAssets;

namespace Assets.Scripts.Network
{
    public class TargetSelector : NetworkBehaviour
    {
        [SerializeField] private float _maxCastDistance = 10.0f;
        [SerializeField] private GameObject _fireballPrefab;

        private static Renderer _currentlySelectedRenderer = null;
        private static Color _originalSelectedColor;
        private bool _isCasting = false;
        private float _castTime = 1.5f;
        private float _castTimer = 0f;

        private Color _originalBarColor;
        private GameObject _selectedTarget;
        private ObjectPool<GameObject> _fireballPool;
        private GameObject _currentFireball;
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
                        var spawnPos = transform.position + Vector3.up * 1.0f;
                        var targetPos = _selectedTarget.transform.position;
                        var direction = (targetPos - spawnPos).normalized;

                        gameObject.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(direction));

                        var networkObject = gameObject.GetComponent<NetworkObject>();
                        networkObject.SpawnWithOwnership(OwnerClientId);
                    },
                    actionOnRelease: (GameObject gameObject) => gameObject.GetComponent<NetworkObject>().Despawn(false)
                );
            }
        }

        private void Update()
        {
            if (IsOwner)
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
                _thirdPersonController.LockCameraToTarget(_selectedTarget.transform);

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
                    UpdateTargetCanvasClientRpc(e.Value, e.Killed, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    });

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
            DespawnFireball();
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
            _originalBarColor = PlayerUI.Instance.CastProgressBar.color;
            SpawnProjectileServerRpc(UserManager.Instance.Token);
        }

        [ServerRpc]
        public void SpawnProjectileServerRpc(string token)
        {
            _currentFireball = _fireballPool.Get();

            _currentFireball.GetComponent<Fireball>().StartCasting(_selectedTarget, gameObject, token);

            NotifyFireballSpawnedClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            });
        }

        [ServerRpc]
        public void CastServerRpc()
        {
            if (_currentFireball != null)
            {
                _currentFireball.GetComponent<Fireball>().Cast();
            }
        }

        [ClientRpc]
        void NotifyFireballSpawnedClientRpc(ClientRpcParams rpcParams = default)
        {
            _isCasting = true;
            _castTimer = 0f;
            PlayerUI.Instance.ShowCastBar(_castTimer);
        }

        [ServerRpc]
        private void DespawnFireballServerRpc()
        {
            DespawnFireball();
        }

        private void CheckCasting()
        {
            if (_isCasting || _selectedTarget == null || _onlyView)
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

            _castTimer += Time.deltaTime;
            PlayerUI.Instance.ShowCastBar(_castTimer / _castTime);

            if (_castTimer >= _castTime)
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

        private void DespawnFireball()
        {
            if (_currentFireball != null)
            {
                _fireballPool.Release(_currentFireball);
                _currentFireball = null;
            }
        }

        private bool CheckMaxDistance(Transform selectedTransform)
        {
            float distance = Vector3.Distance(transform.position, selectedTransform.position);
            var result = distance <= _maxCastDistance;

            Debug.Log($"CheckMaxDistance -> IsValid: {result}, Distance: {distance}, MaxCastDistance: {_maxCastDistance}");

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
                CheckLineOfSight(selectedTransform) &&
                CheckAngle(selectedTransform);
        }
    }
}