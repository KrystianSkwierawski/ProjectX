using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using VContainer.Unity;

public class FireballController : NetworkBehaviour, ITickable
{
    [SerializeField] private AudioClip PreCastSfx;
    [SerializeField] private AudioClip CastSfx;
    [SerializeField] private AudioClip ImpactSfx;
    [SerializeField] private AudioClip FailedSfx;
    [SerializeField] private float Speed = 15f;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private VisualEffect _visualEffect;

    private string _clientToken;
    private NetworkObject _target;
    private bool _hit;

    protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
    {
        base.OnNetworkPreSpawn(ref networkManager);

        FindFirstObjectByType<MainLifetimeScope>().Container.InjectGameObject(gameObject);
    }

    public async void Tick()
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

    public void PreCast(string token)
    {
        _clientToken = token;
        PreCastClientRpc();
    }

    [ClientRpc]
    private void PreCastClientRpc()
    {
        _audioSource.PlayOneShot(PreCastSfx, 0.7f);
    }

    public void Cast(NetworkObject target)
    {
        CastClientRpc();
        _target = target;
    }

    [ClientRpc]
    private void CastClientRpc()
    {
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        _audioSource.PlayOneShot(CastSfx, 0.7f);
    }

    public void Failed()
    {
        Destroy(gameObject, FailedSfx.length);
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
        _audioSource.PlayOneShot(FailedSfx, 1f);
    }

    private void MoveTowardsTarget()
    {
        // todo: set in Cast()
        var targetTransform = _target.GetComponent<Transform>();

        Vector3 direction = (targetTransform.position - transform.position).normalized;
        transform.position += direction * Speed * Time.deltaTime;
    }

    private bool IsCloseToTarget()
    {
        // todo: set in Cast()
        var targetTransform = _target.GetComponent<Transform>();

        return Vector3.Distance(transform.position, targetTransform.position) < 0.5f;
    }

    private async UniTask OnHitTargetAsync()
    {
        if (!_hit)
        {
            _hit = await _target.GetComponent<TargetController>().DealDamageAsync(50f, _clientToken, OwnerClientId);

            OnHitTargetClientRpc();

            await DespawnAfterImpactAsync();
        }
    }

    private async UniTask DespawnAfterImpactAsync()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(ImpactSfx.length));

        GetComponent<NetworkObject>()?.Despawn();
    }

    [ClientRpc]
    private void OnHitTargetClientRpc()
    {
        _visualEffect.enabled = false;
        _audioSource.PlayOneShot(ImpactSfx, 1f);
    }
}