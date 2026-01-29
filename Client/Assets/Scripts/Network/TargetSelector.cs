using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Assets.Scripts.UI;
using StarterAssets;
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
        private StarterAssetsInputs _input;

        private void Start()
        {
            if (IsOwner)
            {
                _input = GetComponent<StarterAssetsInputs>();
                PlayerUI.Instance.HideCastBar();
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

            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

            var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "Target";

            if (!hover)
            {
                CursorUI.Instance.ShowDefault();

                return;
            }

            CursorUI.Instance.ShowPointer();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (_currentlySelectedRenderer != null)
                {
                    UnselectServerRpc((int)SelectedTargetTransform.gameObject.GetComponent<NetworkObject>().NetworkObjectId);

                    _currentlySelectedRenderer.material.color = _originalSelectedColor;
                    SelectedTargetTransform = null;
                    TargetUI.Instance.Target.SetActive(false);
                }

                var newRenderer = hit.transform.GetComponent<Renderer>();
                _currentlySelectedRenderer = newRenderer;
                _originalSelectedColor = newRenderer.material.color;
                newRenderer.material.color = ColorUI.Green;
                SelectedTargetTransform = hit.transform;
                TargetUI.Instance.SetTarget("Bean", SelectedTargetTransform.GetComponent<Health>().Network.Value.ToString());
                SelectServerRpc((int)SelectedTargetTransform.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }

        [ServerRpc]
        private void SelectServerRpc(int networkObjectId)
        {
            UpdateTargetSelectorSubscription.Instance.Subscribe($"{networkObjectId}_{OwnerClientId}", (e) =>
            {
                UpdateTargetCanvasClientRpc(e.Value, e.Hide, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { OwnerClientId }
                    }
                });
            });
        }

        [ServerRpc]
        private void UnselectServerRpc(int networkObjectId)
        {
            UpdateTargetSelectorSubscription.Instance.Unsubscribe($"{networkObjectId}_{OwnerClientId}");
        }

        [ClientRpc]
        private void UpdateTargetCanvasClientRpc(float value, bool hide, ClientRpcParams rpcParams = default)
        {
            TargetUI.Instance.TargetHealthPointsText.text = value.ToString();

            if (hide)
            {
                TargetUI.Instance.Target.SetActive(false);
            }
        }

        private void StartCast()
        {
            _originalBarColor = PlayerUI.Instance.CastProgressBar.color;
            SetFireball();
        }

        private void SetFireball()
        {
            var spawnPos = transform.position + Vector3.up * 1.0f;
            var targetPos = SelectedTargetTransform.position;
            var direction = (targetPos - spawnPos).normalized;

            SpawnProjectileServerRpc(spawnPos, direction, NetworkManager.Singleton.LocalClientId, UserManager.Instance.Token);
        }

        [ServerRpc]
        public void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, ulong clientId, string token)
        {
            // TODO: ObjectPool
            var fireball = Instantiate(_fireballPrefab, position, Quaternion.LookRotation(direction));
            var networkObject = fireball.GetComponent<NetworkObject>();
            networkObject.SpawnWithOwnership(clientId);
            var spawnedFireball = fireball.GetComponent<Fireball>();
            spawnedFireball.PreCast(token);

            NotifyClientRpc(networkObject.NetworkObjectId, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
        }

        [ServerRpc]
        public void CastServerRpc(ulong objectId, ulong clientId, ulong selectedTargetTransformObjectId)
        {
            var fireball = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].GetComponent<Fireball>();
            var selectedTargetTransform = NetworkManager.Singleton.SpawnManager.SpawnedObjects[selectedTargetTransformObjectId];
            fireball.Cast(selectedTargetTransform);
        }

        [ClientRpc]
        void NotifyClientRpc(ulong objectId, ClientRpcParams rpcParams = default)
        {
            _isCasting = true;
            _castTimer = 0f;
            _objectId = objectId;
            PlayerUI.Instance.ShowCastBar(_castTimer);
        }

        private void HandleCastingInput()
        {
            if (SelectedTargetTransform != null && !_isCasting && !_isInterrupted &&
                _input.Move == Vector2.zero && !_input.Jump &&
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
                PlayerUI.Instance.HideCastBar();

                if (PlayerUI.Instance.CastProgressBar != null)
                {
                    PlayerUI.Instance.CastProgressBar.color = _originalBarColor;
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

            if (_input.Move != Vector2.zero || _input.Jump)
            {
                InterruptCast();
                return;
            }

            _castTimer += Time.deltaTime;
            PlayerUI.Instance.ShowCastBar(_castTimer / _castTime);

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
            PlayerUI.Instance.HideCastBar();
        }

        private void InterruptCast()
        {
            _isCasting = false;
            _isInterrupted = true;
            _interruptTimer = 0f;
            FailedServerRpc(_objectId, NetworkManager.Singleton.LocalClientId);

            PlayerUI.Instance.FailCastBar();
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