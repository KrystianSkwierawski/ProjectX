using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class SpawnerController : MonoBehaviour, ITickable
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _beansCount = 2;
    [SerializeField] private Collider _collider;

    private bool _isSpawning;

    public async void Tick()
    {
#if UNITY_SERVER && !UNITY_EDITOR

        if (_isSpawning)
        {
            return;
        }

        var beans = GameObject.FindGameObjectsWithTag("Target");

        if (beans.Length < _beansCount)
        {
            _isSpawning = true;
            await RespawnAsync(_beansCount - beans.Length);
        }
#endif
    }

    private async UniTask RespawnAsync(int count)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(5));
        SpawnBeans(count);
    }

    private void SpawnBeans(int count)
    {
        var bounds = _collider.bounds;

        for (int i = 1; i <= count; i++)
        {
            var position = new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x), -3.5f, UnityEngine.Random.Range(bounds.min.z, bounds.max.z));

            var enemy = Instantiate(_enemyPrefab, position, new Quaternion(0f, 0f, 0f, 0f));
            var netObj = enemy.GetComponent<NetworkObject>();
            netObj.Spawn();
        }

        _isSpawning = false;

        Debug.Log($"{count} beans spawned");
    }
}
