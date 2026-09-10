using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace Assets.Scripts.Areas.Character.Mono
{
    public abstract class AbstractWeapon : NetworkBehaviour
    {
        protected virtual AudioTypeEnum PrecastAudioType { get; }

        protected virtual AudioTypeEnum CastAudioType { get; }

        protected virtual AudioTypeEnum ImpactAudioType { get; }

        protected abstract float BaseDamage { get; }

        protected abstract float Speed { get; }

        protected virtual Quaternion RotationOffset => Quaternion.identity;

        private string _playerSessionId;
        private AudioSource _audioSource;
        private VisualEffect _visualEffect;
        private GameObject _target;
        private GameObject _caster;
        private bool _isCasting;
        private bool _hit;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _visualEffect = GetComponent<VisualEffect>();
        }

        public void StartCasting(GameObject target, GameObject caster, string playerSessionId)
        {
            _caster = caster;
            _target = target;
            _isCasting = true;
            _playerSessionId = playerSessionId;
            _hit = false;
            Debug.Log("StartCasting");
            StartCastingClientRpc();
        }

        public void SetPositionAndDirection(Vector3 position, Vector3 direction)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction) * RotationOffset);
        }

        [ClientRpc]
        private void StartCastingClientRpc()
        {
            Debug.Log("StartCastingClientRpc");

            if (PrecastAudioType != AudioTypeEnum.None && _audioSource != null)
            {
                AudioManager.Instance.TryPlayOneShot(_audioSource, PrecastAudioType, 0.7f);
            }

            if (_visualEffect != null)
            {
                _visualEffect.enabled = true;
            }
        }

        public void Cast()
        {
            CastClientRpc();
            _isCasting = false;
        }

        [ClientRpc]
        private void CastClientRpc()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            if (CastAudioType != AudioTypeEnum.None && _audioSource != null)
            {
                AudioManager.Instance.TryPlayOneShot(_audioSource, CastAudioType, 0.7f);
            }
        }

        public void Failed()
        {
            FailedClientRpc();
        }

        [ClientRpc]
        private void FailedClientRpc()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            if (_visualEffect != null)
            {
                _visualEffect.enabled = false;
            }

            if (_audioSource != null)
            {
                AudioManager.Instance.TryPlayOneShot(_audioSource, AudioTypeEnum.CastingFailed, 0.1f);
            }
            else
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.CastingFailed, 0.1f);
            }
        }

        private void Update()
        {
            if (!IsServer || _target == null)
            {
                return;
            }

            if (_isCasting)
            {
                var spawnPos = _caster.transform.position + Vector3.up * 1.0f;
                var targetPos = _target.transform.position;
                var direction = (targetPos - spawnPos).normalized;

                SetPositionAndDirection(spawnPos, direction);

                return;
            }

            transform.MoveTowardsTarget(_target, true, Speed);

            if (transform.IsCloseToTarget(_target))
            {
                OnHitTarget();
            }
        }

        // FIXME: disconnect error
        private void OnHitTarget()
        {
            if (!_hit)
            {
                _hit = true;

                var character = UserManager.Instance.Characters[OwnerClientId];

                var damage = character.ApplyWeaponDamage(BaseDamage);

                if (!character.AmmoType.IsArmorAmmo())
                {
                    _caster.GetComponent<Player>().ConsumeAmmo();
                }

                OnHitTargetClientRpc();

                AttackTargetSubscription.Instance.Invoke(_target.GetInstanceID().ToString(), new AttackTargetSubscriptionEvent
                {
                    ClientId = OwnerClientId,
                    Value = damage,
                    PlayerSessionId = _playerSessionId,
                    Player = _caster
                });
            }
        }

        [ClientRpc]
        private void OnHitTargetClientRpc()
        {
            if (ImpactAudioType != AudioTypeEnum.None)
            {
                AudioManager.Instance.TryPlayOneShot(ImpactAudioType, 1f);
            }

            gameObject.SetActive(false);
        }

        public override void OnNetworkDespawn()
        {
            _target = null;
            _isCasting = false;
            _caster = null;

            base.OnNetworkDespawn();

            gameObject.SetActive(false);
        }
    }
}
