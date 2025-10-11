using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class SpawnerMonoBehaviour : MonoBehaviour
{

    public GameObject EnemyPrefab;
    public int BeansCount = 2;

    private bool _isSpawning;
    private Collider _collider;


    private void Start()
    {
        _collider = GetComponent<Collider>();
    }

    private async void Update()
    {
        if (_isSpawning)
        {
            return;
        }

        var beans = GameObject.FindGameObjectsWithTag("Target");


        if (beans.Length < BeansCount)
        {
            Debug.Log($"Current beans: {beans.Length}");
            _isSpawning = true;
            await RespawnAsync(BeansCount - beans.Length);
        }
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

            var enemy = Instantiate(EnemyPrefab, position, new Quaternion(0f, 0f, 0f, 0f));
            var netObj = enemy.GetComponent<NetworkObject>();
            netObj.Spawn();
        }

        _isSpawning = false;
        Debug.Log($"{count} beans spawned");
    }
}

