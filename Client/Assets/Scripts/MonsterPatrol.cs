using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MonsterPatrol : NetworkBehaviour
{
    public float PatrolRadius = 5f;
    public float MoveSpeed = 2f;
    public float PointTolerance = 0.2f;

    private Vector3 _startPosition;
    private Vector3 _currentTarget;
    private bool _isWaiting = false;

    private void Start()
    {
        if (IsServer)
        {
            _startPosition = transform.position;
            PickNewPatrolPoint();
        }
    }

    private void Update()
    {
        if (IsServer && !_isWaiting)
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        Vector3 direction = (_currentTarget - transform.position).normalized;
        transform.position += direction * MoveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _currentTarget) < PointTolerance)
        {
            StartCoroutine(WaitBeforeNextPoint());
        }
    }

    private IEnumerator WaitBeforeNextPoint()
    {
        _isWaiting = true;
        float delay = new System.Random().Next(1, 5);
        yield return new WaitForSeconds(delay);
        PickNewPatrolPoint();
        _isWaiting = false;
    }

    private void PickNewPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * PatrolRadius;
        _currentTarget = _startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
}
