using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class MonsterPatrolNetworkBehaviour : NetworkBehaviour
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
        transform.position += direction * MoveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _currentTarget) < PointTolerance)
        {
            await WaitBeforeNextPointAsync();
        }
    }

    private async UniTask WaitBeforeNextPointAsync()
    {
        _isWaiting = true;
        float delay = new System.Random().Next(1, 5);
        await UniTask.Delay(System.TimeSpan.FromSeconds(delay));    
        PickNewPatrolPoint();
        _isWaiting = false;
    }

    private void PickNewPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * PatrolRadius;
        _currentTarget = _startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
}
