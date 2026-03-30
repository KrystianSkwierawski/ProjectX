using Assets.Scripts.Enums;
using Assets.Scripts.Extensions;
using Assets.Scripts.Mono;
using Assets.Scripts.Network;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAggro : NetworkBehaviour
{
    private GameObject _target;
    private ulong? _clientId;
    private float _attackTime = 2f;
    private float _attackTimer = 0f;
    private MonsterPatrol _patrol;
    private Vector3 _initPosition;
    private NavMeshAgent _agent;
    private bool _isReturning = false;
    private float _destinationUpdateThreshold = 0.5f;

    private void Start()
    {
        if (IsServer)
        {
            var key = gameObject.GetInstanceID().ToString();

            _patrol = GetComponent<MonsterPatrol>();

            _agent = GetComponent<NavMeshAgent>();

            _agent.isStopped = false;
            _agent.updatePosition = true;

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
                    _isReturning = false;

                    _agent.isStopped = false;
                    _agent.SetDestination(_target.transform.position);

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
            if (_isReturning && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _agent.isStopped = true;
                _isReturning = false;
                _patrol.IsWaiting = false;
            }

            return;
        }

        if (transform.IsFarToTarget(_target))
        {
            LoseAggro();

            return;
        }

        if (transform.IsCloseToTarget(_target, 2f))
        {
            if (!_agent.isStopped)
            {
                _agent.isStopped = true;
            }

            CheckAttack();

            return;
        }

        if (!_agent.hasPath || Vector3.Distance(_agent.destination, _target.transform.position) > _destinationUpdateThreshold)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_target.transform.position);
        }
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

        _isReturning = true;
        _patrol.IsWaiting = true;
        _agent.isStopped = false;
        _agent.SetDestination(_initPosition);
    }
}