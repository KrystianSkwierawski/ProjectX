using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Network
{
    public class TargetSelector : NetworkBehaviour
    {
        [SerializeField] private float _maxCastDistance = 10.0f;
        [SerializeField] private GameObject _fireballPrefab;

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

        private void Start()
        {
            if (IsOwner)
            {
                UIManager.Instance.HideCastBar();
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
                        UIManager.Instance.Target.SetActive(false);
                    }

                    var newRenderer = hit.transform.GetComponent<Renderer>();
                    _currentlySelectedRenderer = newRenderer;
                    _originalSelectedColor = newRenderer.material.color;
                    newRenderer.material.color = Color.green;
                    SelectedTargetTransform = hit.transform;

                    UIManager.Instance.SetTarget("Bean", SelectedTargetTransform.GetComponent<Health>().Value.ToString());
                }
            }
        }

        private void StartCast()
        {
            _originalBarColor = UIManager.Instance.CastProgressBar.color;
            SetFireball();
        }

        private void SetFireball()
        {
            var spawnPos = transform.position + Vector3.up * 1.0f;
            var targetPos = SelectedTargetTransform.position;
            var direction = (targetPos - spawnPos).normalized;

            SpawnProjectileServerRpc(spawnPos, direction, NetworkManager.Singleton.LocalClientId, TokenManager.Instance.Token);
        }

        [ServerRpc]
        public void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, ulong clientId, string token)
        {
            var fireball = Instantiate(_fireballPrefab, position, Quaternion.LookRotation(direction));
            var netObj = fireball.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(clientId);
            var spawnedFireball = fireball.GetComponent<Fireball>();
            spawnedFireball.PreCast(token);

            NotifyClientRpc(netObj.NetworkObjectId, clientId);
        }

        [ServerRpc]
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
                UIManager.Instance.ShowCastBar(0f);
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
            UIManager.Instance.ShowCastBar(_castTimer / _castTime);

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
            UIManager.Instance.HideCastBar();
        }

        private void InterruptCast()
        {
            _isCasting = false;
            _isInterrupted = true;
            _interruptTimer = 0f;
            FailedServerRpc(_objectId, NetworkManager.Singleton.LocalClientId);

            UIManager.Instance.FailCastBar();
        }

        [ServerRpc]
        private void FailedServerRpc(ulong objectId, ulong clientId)
        {
            var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId];
            var fireball = obj.GetComponent<Fireball>();

            fireball.Failed();
        }

        [ServerRpc]
        private void DespawnFireballServerRpc(ulong objectId)
        {
            var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId];
            obj.Despawn();
        }

        private bool CheckMaxDistance()
        {
            float distance = Vector3.Distance(transform.position, SelectedTargetTransform.position);
            var result = distance <= _maxCastDistance;

            Debug.Log($"CheckMaxDistance -> IsValid: {result}, Distance: {distance}, MaxCastDistance: {_maxCastDistance}");

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
}