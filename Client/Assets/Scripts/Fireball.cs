using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class Fireball : NetworkBehaviour
{
    public AudioClip PreCastSfx;
    public AudioClip CastSfx;
    public AudioClip ImpactSfx;
    public AudioClip FailedSfx;
    public float Speed = 15f;

    private AudioSource _audioSource;
    private VisualEffect _visualEffect;
    private NetworkObject _target;
    private bool _damageDealt;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _visualEffect = GetComponent<VisualEffect>();
    }

    public void PreCast()
    {
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

    private void Update()
    {
        if (!IsServer || _target == null)
        {
            return;
        }

        MoveTowardsTarget();

        if (IsCloseToTarget())
        {
            OnHitTarget();
        }
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

    private void OnHitTarget()
    {
        if (!_damageDealt)
        {
            _damageDealt = _target.GetComponent<Health>().DealDamage(50f);
        }

        Destroy(gameObject, ImpactSfx.length); // despawn
        OnHitTargetClientRpc();
        _target = null;
    }

    [ClientRpc]
    private void OnHitTargetClientRpc()
    {
        _visualEffect.enabled = false;
        _audioSource.PlayOneShot(ImpactSfx, 1f);
    }
}