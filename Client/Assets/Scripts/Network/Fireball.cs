using System;
using Assets.Scripts.Enums;
using Assets.Scripts.Mono;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace Assets.Scripts.Network
{
    public class Fireball : NetworkBehaviour
    {
        [SerializeField] private float _speed = 15f;

        private string _clientToken;
        private AudioSource _audioSource;
        private VisualEffect _visualEffect;
        private NetworkObject _target;
        private bool _hit;
        private Transform _targetTransform;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _visualEffect = GetComponent<VisualEffect>();
        }

        public void PreCast(string token)
        {
            _clientToken = token;
            PreCastClientRpc();
        }

        [ClientRpc]
        private void PreCastClientRpc()
        {
            AudioManager.Instance.PlayOneShot(_audioSource, AudioTypeEnum.FireballPrecast, 0.7f);
        }

        public void Cast(NetworkObject target)
        {
            CastClientRpc();
            _target = target;
            _targetTransform = target.transform;
        }

        [ClientRpc]
        private void CastClientRpc()
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            AudioManager.Instance.PlayOneShot(_audioSource, AudioTypeEnum.FireballCast, 0.7f);
        }

        public void Failed()
        {
            var delay = AudioManager.Instance.AudioClips[AudioTypeEnum.CastingFailed].length;

            Destroy(gameObject, delay);

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
            AudioManager.Instance.PlayOneShot(_audioSource, AudioTypeEnum.CastingFailed, 1f);
        }

        private async void Update()
        {
            if (!IsServer || _target == null)
            {
                return;
            }

            MoveTowardsTarget();

            if (IsCloseToTarget())
            {
                await OnHitTargetAsync();
            }
        }

        private void MoveTowardsTarget()
        {
            Vector3 direction = (_targetTransform.position - transform.position).normalized;
            transform.position += direction * _speed * Time.deltaTime;
        }

        private bool IsCloseToTarget()
        {
            return Vector3.Distance(transform.position, _targetTransform.position) < 0.5f;
        }

        private async UniTask OnHitTargetAsync()
        {
            if (!_hit)
            {
                _hit = true;
                await _target.GetComponent<Health>().DealDamageAsync(50f, _clientToken, OwnerClientId);

                OnHitTargetClientRpc();

                await DespawnAfterImpactAsync();
            }
        }

        private async UniTask DespawnAfterImpactAsync()
        {
            var delay = AudioManager.Instance.AudioClips[AudioTypeEnum.FireballImpact].length;

            await UniTask.Delay(TimeSpan.FromSeconds(delay));

            GetComponent<NetworkObject>()?.Despawn();
        }

        [ClientRpc]
        private void OnHitTargetClientRpc()
        {
            _visualEffect.enabled = false;
            AudioManager.Instance.PlayOneShot(_audioSource, AudioTypeEnum.FireballImpact, 1f);
        }
    }
}