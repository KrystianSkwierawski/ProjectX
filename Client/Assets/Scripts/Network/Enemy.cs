using Assets.Scripts.Enums;
using Assets.Scripts.Extensions;
using Assets.Scripts.Mono;
using Assets.Scripts.Subscriptions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : NetworkBehaviour
{
    private GameObject _target;
    private ulong? _clientId;
    private float _attackTime = 2f;
    private float _attackTimer = 0f;
    private Vector3 _initPosition;
    private NavMeshAgent _agent;
    private bool _isReturning = false;
    private float _destinationUpdateThreshold = 0.5f;

    private bool _isPatrolling = false;
    private float _patrolRadius = 5f;
    private float _patrolWaitTime = 2f;
    private float _patrolWaitTimer = 0f;
    private Vector3 _currentPatrolPoint;

    private float _maxChasePathLength = 30f;

    private void Start()
    {
        if (IsServer)
        {
            var key = gameObject.GetInstanceID().ToString();

            _initPosition = transform.position;

            _agent = GetComponent<NavMeshAgent>();

            EnemyAggroSubscription.Instance.Subscribe(key, (e) =>
            {
                if (e.Target == null)
                {
                    LoseAggro();

                    return;
                }

                if (_target == null)
                {
                    _target = e.Target;
                    _clientId = e.ClientId;
                    _isReturning = false;
                    _isPatrolling = false;

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

            _agent.enabled = true;
        }
    }

    [ClientRpc]
    private void AggroClientRpc(ClientRpcParams rpcParams = default)
    {
        AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.MonsterAggro, 0.5f);
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (!_agent.enabled)
        {
            // TODO: update transfrom directly?
            Debug.Log("NavMeshAgent disabled");

            return;
        }

        if (_target == null)
        {
            if (_isReturning && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _isReturning = false;
                StartPatrolling();
            }

            if (_isPatrolling)
            {
                HandlePatrolling();
            }

            return;
        }

        if (IsAgentPathTooLong(_target.transform.position) || IsAgentPathTooLong(_initPosition))
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

            AttackPlayerSubscription.Instance.Invoke(_clientId.Value.ToString(), new PlayerAttackSubscriptionEvent
            {
                Value = 50,
            });
        }
    }

    private void LoseAggro()
    {
        Debug.Log("Lose aggro");

        // TODO: slow regeneration
        SetHealthSubscription.Instance.Invoke(gameObject.GetInstanceID().ToString(), new SetHealthSubscriptionEvent
        {
            Value = 100
        });

        _attackTimer = 0f;
        _target = null;
        _clientId = null;

        _isReturning = true;
        _isPatrolling = false;
        _agent.isStopped = false;
        _agent.SetDestination(_initPosition);
    }

    private void StartPatrolling()
    {
        _isPatrolling = true;
        _patrolWaitTimer = 0f;
        _agent.isStopped = false;
        _currentPatrolPoint = GetRandomPointAround(_initPosition, _patrolRadius);
        _agent.SetDestination(_currentPatrolPoint);
    }

    private void HandlePatrolling()
    {
        if (_agent.pathPending)
        {
            return;
        }

        if (_agent.remainingDistance <= _agent.stoppingDistance)
        {
            _patrolWaitTimer += Time.deltaTime;

            if (_patrolWaitTimer >= _patrolWaitTime)
            {
                _patrolWaitTimer = 0f;
                _currentPatrolPoint = GetRandomPointAround(_initPosition, _patrolRadius);
                _agent.SetDestination(_currentPatrolPoint);
            }
        }
    }

    private Vector3 GetRandomPointAround(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPos = center + Random.insideUnitSphere * radius;

            if (NavMesh.SamplePosition(randomPos, out var hit, 1.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return center;
    }

    private bool IsAgentPathTooLong(Vector3 position)
    {
        var path = new NavMeshPath();

        bool pathCalculated = _agent.CalculatePath(position, path);

        // if no path could be calculated or path is not complete, consider it too long / unreachable
        if (!pathCalculated || path.status != NavMeshPathStatus.PathComplete)
        {
            return true;
        }

        var length = 0f;
        var previous = _agent.transform.position;

        for (int i = 0; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(previous, path.corners[i]);
            previous = path.corners[i];
        }

        return length > _maxChasePathLength;
    }
}