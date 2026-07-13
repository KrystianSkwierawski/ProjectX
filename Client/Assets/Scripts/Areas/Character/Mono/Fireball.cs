using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Fireball : NetworkBehaviour
    {
        [SerializeField] private float _speed = 15f;

        private string _clientToken;
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

        public void StartCasting(GameObject target, GameObject caster, string token)
        {
            _caster = caster;
            _target = target;
            _isCasting = true;
            _clientToken = token;
            _hit = false;
            Debug.Log("StartCasting");
            StartCastingClientRpc();
        }

        [ClientRpc]
        private void StartCastingClientRpc()
        {
            Debug.Log("StartCastingClientRpc");
            AudioManager.Instance.TryPlayOneShot(_audioSource, AudioTypeEnum.FireballPrecast, 0.7f);
            _visualEffect.enabled = true;
        }

        public void Cast()
        {
            CastClientRpc();
            _isCasting = false;
        }

        [ClientRpc]
        private void CastClientRpc()
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            AudioManager.Instance.TryPlayOneShot(_audioSource, AudioTypeEnum.FireballCast, 0.7f);
        }

        public void Failed()
        {
            FailedClientRpc();
        }

        [ClientRpc]
        private void FailedClientRpc()
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            _visualEffect.enabled = false;
            AudioManager.Instance.TryPlayOneShot(_audioSource, AudioTypeEnum.CastingFailed, 0.1f);
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

                gameObject.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(direction));

                return;
            }

            transform.MoveTowardsTarget(_target, true, _speed);

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
                var damage = CharacterStatsCalculator.ApplyStrength(50f, character.Strength);

                AttackTargetSubscription.Instance.Invoke(_target.GetInstanceID().ToString(), new AttackTargetSubscriptionEvent
                {
                    ClientId = OwnerClientId,
                    Value = damage,
                    ClientToken = _clientToken,
                    Player = _caster
                });

                OnHitTargetClientRpc();
            }
        }

        [ClientRpc]
        private void OnHitTargetClientRpc()
        {
            _visualEffect.enabled = false;
            AudioManager.Instance.TryPlayOneShot(_audioSource, AudioTypeEnum.FireballImpact, 1f);
        }

        public override void OnNetworkDespawn()
        {
            _target = null;
            _isCasting = false;
            _caster = null;

            base.OnNetworkDespawn();
        }
    }
}
