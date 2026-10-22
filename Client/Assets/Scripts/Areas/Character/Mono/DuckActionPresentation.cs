using System;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Character.Mono
{
    public enum DuckActionEnum : byte
    {
        None = 0, SwordWindup = 1, SwordStrike = 2, BowDraw = 3, BowRelease = 4,
        WandCharge = 5, WandRelease = 6, Mining = 7, Fishing = 8, Herbalism = 9,
        Crafting = 10, Lumberjack = 11, Cooking = 12, Alchemy = 13
    }

    public struct DuckActionState : INetworkSerializable, IEquatable<DuckActionState>
    {
        public DuckActionEnum Action;
        public double Started;
        public float Duration;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Action);
            serializer.SerializeValue(ref Started);
            serializer.SerializeValue(ref Duration);
        }

        public bool Equals(DuckActionState other) => Action == other.Action && Started == other.Started && Duration == other.Duration;
    }

    // Presentation only: combat/profession coordinators remain responsible for all outcomes.
    // Timestamped server state also reconstructs the current pose for late observers.
    [DefaultExecutionOrder(100)]
    public sealed class DuckActionPresentation : NetworkBehaviour
    {
        private static readonly int[] ActionHashes = CreateActionHashes();

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _visual;
        [SerializeField] private Transform _leftGrip;
        [SerializeField] private Transform _rightGrip;
        [SerializeField] private GameObject[] _props = new GameObject[9];
        [SerializeField] private LineRenderer _bowString;
        [SerializeField] private GameObject _nockedArrow;

        private readonly NetworkVariable<DuckActionState> _state = new NetworkVariable<DuckActionState>();
        private readonly NetworkVariable<WeaponCategoryEnum> _weapon = new NetworkVariable<WeaponCategoryEnum>();
        private int _layer;
        private float _weight;
        private Health _health;
        private WeaponCategoryEnum _castingWeapon;
        private DuckActionState? _pendingCombat;

        public override void OnNetworkSpawn()
        {
            _layer = _animator.GetLayerIndex("Actions");
            _health = GetComponent<Health>();
            _weight = 0;
            _pendingCombat = null;
            if (IsServer)
            {
                _weapon.Value = WeaponCategoryEnum.None;
                _castingWeapon = WeaponCategoryEnum.None;
                SetServer(DuckActionEnum.None, 0);
            }

            _state.OnValueChanged += OnActionChanged;
            Apply(_state.Value, NetworkManager.ServerTime.Time, 1);
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnActionChanged;
            _animator.SetLayerWeight(_layer, 0);
            foreach (var prop in _props)
            {
                if (prop != null)
                {
                    prop.SetActive(false);
                }
            }

            _weight = 0;
            _pendingCombat = null;
        }

        private void OnActionChanged(DuckActionState previous, DuckActionState next)
        {
            Debug.Log($"[DuckAction] actor={NetworkObjectId}, action={next.Action}, duration={next.Duration:F2}, server={IsServer}.", this);
        }

        public void BeginCombat(WeaponCategoryEnum category, float duration)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            _castingWeapon = category;
            _weapon.Value = category;
            var action = category == WeaponCategoryEnum.Bow ? DuckActionEnum.BowDraw
                : category == WeaponCategoryEnum.Wand ? DuckActionEnum.WandCharge : DuckActionEnum.SwordWindup;

            // Auto-attack can begin its next cast immediately after release. Keep the
            // visible follow-through without delaying the authoritative combat clock.
            var current = _state.Value;
            double now = NetworkManager.ServerTime.Time;
            double remaining = current.Started + current.Duration - now;
            if (remaining > 0 && (current.Action == DuckActionEnum.SwordStrike
                || current.Action == DuckActionEnum.BowRelease || current.Action == DuckActionEnum.WandRelease))
            {
                _pendingCombat = new DuckActionState
                {
                    Action = action,
                    Started = now + remaining,
                    Duration = Mathf.Max(.01f, duration - (float)remaining)
                };

                return;
            }

            SetServer(action, duration);
        }

        public void ReleaseCombat()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            var action = _castingWeapon == WeaponCategoryEnum.Bow ? DuckActionEnum.BowRelease
                : _castingWeapon == WeaponCategoryEnum.Wand ? DuckActionEnum.WandRelease : DuckActionEnum.SwordStrike;
            SetServer(action, .4f);
        }

        public void CancelCombat()
        {
            if (IsServer && IsSpawned && _state.Value.Action >= DuckActionEnum.SwordWindup && _state.Value.Action <= DuckActionEnum.WandRelease)
            {
                SetServer(DuckActionEnum.None, 0);
            }
        }

        public void SetProfession(DuckActionEnum action, bool active)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            if (active)
            {
                SetServer(action, action == DuckActionEnum.Fishing ? 0 : 4);
            }
            else if (_state.Value.Action == action)
            {
                SetServer(DuckActionEnum.None, 0);
            }
        }

        public void RequestWork(DuckActionEnum action, bool active, GameObject target)
        {
            if (IsSpawned && IsOwner)
            {
                WorkServerRpc(action, active, target != null ? target.transform.position : Vector3.zero);
            }
        }

        [ServerRpc]
        private void WorkServerRpc(DuckActionEnum action, bool active, Vector3 position)
        {
            if (action != DuckActionEnum.Herbalism && action != DuckActionEnum.Crafting
                && action != DuckActionEnum.Cooking && action != DuckActionEnum.Alchemy)
            {
                return;
            }

            if (!active)
            {
                if (_state.Value.Action == action)
                {
                    SetServer(DuckActionEnum.None, 0);
                }

                return;
            }

            if (!DungeonTravel.CanInteract(OwnerClientId) || !float.IsFinite(position.sqrMagnitude)
                || Vector3.Distance(transform.position, position) > 3.5f)
            {
                Debug.LogWarning($"[DuckAction] Rejected work presentation actor={NetworkObjectId}, action={action}: unavailable or out of range.", this);
                return;
            }

            string tag = action == DuckActionEnum.Herbalism ? "Herbalism" : "Crafting";
            foreach (var collider in Physics.OverlapSphere(position, .35f))
            {
                if (collider.CompareTag(tag) && DungeonWorldObject.SameInstance(gameObject, collider.gameObject))
                {
                    SetServer(action, action == DuckActionEnum.Herbalism ? 2.2f : 4.2f);
                    return;
                }
            }

            Debug.LogWarning($"[DuckAction] Rejected work presentation actor={NetworkObjectId}, action={action}: no matching work target.", this);
        }

        private void SetServer(DuckActionEnum action, float duration)
        {
            _pendingCombat = null;
            _state.Value = new DuckActionState { Action = action, Started = NetworkManager.ServerTime.Time, Duration = duration };
        }

        private void Update()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            if (UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character))
            {
                var category = character.WeaponType.GetWeaponCategory();
                if (_weapon.Value != category)
                {
                    _weapon.Value = category;
                    CancelCombat();
                }
            }

            if (_pendingCombat.HasValue && NetworkManager.ServerTime.Time >= _pendingCombat.Value.Started)
            {
                _state.Value = _pendingCombat.Value;
                _pendingCombat = null;
            }

            var state = _state.Value;
            if (state.Action != DuckActionEnum.None && (!DungeonTravel.CanInteract(OwnerClientId)
                || _health != null && _health.Network.Value <= 0
                || state.Duration > 0 && NetworkManager.ServerTime.Time >= state.Started + state.Duration +
                    (state.Action == DuckActionEnum.SwordWindup || state.Action == DuckActionEnum.BowDraw || state.Action == DuckActionEnum.WandCharge ? 1 : 0)))
            {
                SetServer(DuckActionEnum.None, 0);
            }
        }

        private void LateUpdate()
        {
            if (IsSpawned)
            {
                Apply(_state.Value, NetworkManager.ServerTime.Time, Time.deltaTime);
            }
        }

        private void Apply(DuckActionState state, double now, float delta)
        {
            bool active = state.Action != DuckActionEnum.None;
            _weight = Mathf.MoveTowards(_weight, active ? 1 : 0, delta * 12);
            _animator.SetLayerWeight(_layer, _weight);
            float phase = Mathf.Max(0, (float)(now - state.Started));
            if (active)
            {
                bool loop = state.Action >= DuckActionEnum.Mining;
                float normalized = loop ? phase / CycleLength(state.Action) : Mathf.Clamp01(phase / Mathf.Max(.01f, state.Duration));
                _animator.Play(ActionHashes[(int)state.Action], _layer, normalized);
            }

            int selected = PropIndex(state.Action, _weapon.Value);
            bool showProp = _visual.gameObject.activeInHierarchy && (_health == null || _health.Network.Value > 0);
            for (int i = 0; i < _props.Length; i++)
            {
                var prop = _props[i];
                if (prop == null)
                {
                    continue;
                }

                bool visible = i == selected && showProp;
                if (visible)
                {
                    var grip = i == 0 ? _leftGrip : _rightGrip;
                    prop.transform.SetPositionAndRotation(grip.position,
                        i == 0 ? transform.rotation * Quaternion.Euler(0, 0, 25) : grip.rotation);
                }

                if (prop.activeSelf != visible)
                {
                    prop.SetActive(visible);
                }
            }

            if (_bowString != null)
            {
                float draw = state.Action == DuckActionEnum.BowDraw ? Mathf.Clamp01(phase / Mathf.Max(.01f, state.Duration))
                    : state.Action == DuckActionEnum.BowRelease ? 1 - Mathf.Clamp01(phase / .1f) : 0;
                var nock = state.Action == DuckActionEnum.BowDraw
                    ? _bowString.transform.InverseTransformPoint(_rightGrip.position)
                    : new Vector3(0, 0, -.19f - .20f * draw);
                _bowString.SetPosition(1, nock);
                _nockedArrow.transform.localPosition = nock + new Vector3(0, 0, .24f);
                _nockedArrow.SetActive(state.Action == DuckActionEnum.BowDraw);
            }
        }

        public static float CycleLength(DuckActionEnum action) => action == DuckActionEnum.Fishing ? 2f
            : action == DuckActionEnum.Herbalism ? 1.2f : 1f;

        private static int[] CreateActionHashes()
        {
            var values = (DuckActionEnum[])Enum.GetValues(typeof(DuckActionEnum));
            var hashes = new int[values.Length];
            foreach (var value in values)
            {
                hashes[(int)value] = Animator.StringToHash(value.ToString());
            }

            return hashes;
        }

        public static int PropIndex(DuckActionEnum action, WeaponCategoryEnum weapon)
        {
            switch (action)
            {
                case DuckActionEnum.BowDraw: case DuckActionEnum.BowRelease: return 0;
                case DuckActionEnum.SwordWindup: case DuckActionEnum.SwordStrike: return 1;
                case DuckActionEnum.WandCharge: case DuckActionEnum.WandRelease: return 2;
                case DuckActionEnum.Mining: return 3;
                case DuckActionEnum.Fishing: return 4;
                case DuckActionEnum.Herbalism: return 5;
                case DuckActionEnum.Crafting: return 6;
                case DuckActionEnum.Lumberjack: return 7;
                case DuckActionEnum.Cooking: case DuckActionEnum.Alchemy: return 8;
                default: return weapon == WeaponCategoryEnum.Bow ? 0 : weapon == WeaponCategoryEnum.Sword ? 1
                    : weapon == WeaponCategoryEnum.Wand ? 2 : -1;
            }
        }
    }
}
