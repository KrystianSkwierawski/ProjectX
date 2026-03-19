using Assets.Scripts.Enums;
using Assets.Scripts.Extensions;
using Assets.Scripts.Mono;
using Assets.Scripts.Network;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Unity.Netcode;
using UnityEngine;

public class MonsterAggro : NetworkBehaviour
{
    private GameObject _target;
    private ulong? _clientId;
    private float _attackTime = 2f;
    private float _attackTimer = 0f;
    private MonsterPatrol _patrol;
    private Vector3 _initPosition;

    private void Start()
    {
        if (IsServer)
        {
            var key = gameObject.GetInstanceID().ToString();

            _patrol = GetComponent<MonsterPatrol>();

            MonsterAggroSubscription.Instance.Subscribe(key, (e) =>
            {
                if (e.Target == null)
                {
                    LoseAggro();

                    return;
                }

                if (_target == null)
                {
                    _patrol.IsWaiting = true;
                    _initPosition = transform.position;
                    _target = e.Target;
                    _clientId = e.ClientId;

                    AggroClientRpc(new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { e.ClientId }
                        }
                    });
                }
            });
        }
    }

    [ClientRpc]
    private void AggroClientRpc(ClientRpcParams rpcParams = default)
    {
        AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MonsterAggro, 0.5f);
    }

    private void Update()
    {
        if (_target == null)
        {
            return;
        }

        if (transform.IsFarToTarget(_target))
        {
            LoseAggro();

            return;
        }

        if (transform.IsCloseToTarget(_target, 2f))
        {
            CheckAttack();

            return;
        }

        transform.MoveTowardsTarget(_target, false);
    }

    private void CheckAttack()
    {
        _attackTimer += Time.deltaTime;

        if (_attackTimer >= _attackTime)
        {
            _attackTimer = 0f;

            Debug.Log("Attack player");
            AttackPlayerSubscription.Instance.Invoke(_clientId.Value.ToString(), new PlayerAttackSubscriptionEvent
            {
                Value = 50,
            });
        }
    }

    private void LoseAggro()
    {
        Debug.Log("Lose aggro");
        _attackTimer = 0f;
        _target = null;
        _clientId = null;
        _patrol.IsWaiting = false;
        transform.position = _initPosition; // TODO: walk to position
    }
}
