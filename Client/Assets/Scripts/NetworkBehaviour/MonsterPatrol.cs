using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class MonsterPatrol : NetworkBehaviour
{
    [SerializeField] private float _patrolRadius = 5f;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _pointTolerance = 0.2f;

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

    private async void Update()
    {
        if (IsServer && !_isWaiting)
        {
            await PatrolAsync();
        }
    }

    private async UniTask PatrolAsync()
    {
        Vector3 direction = (_currentTarget - transform.position).normalized;
        transform.position += direction * _moveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _currentTarget) < _pointTolerance)
        {
            await WaitBeforeNextPointAsync();
        }
    }

    private async UniTask WaitBeforeNextPointAsync()
    {
        _isWaiting = true;

        float delay = new System.Random().Next(5, 10);

        await UniTask.Delay(System.TimeSpan.FromSeconds(delay));

        PickNewPatrolPoint();

        _isWaiting = false;

    }

    private void PickNewPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * _patrolRadius;
        _currentTarget = _startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
}
