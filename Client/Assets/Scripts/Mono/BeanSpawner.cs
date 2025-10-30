using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Mono 
{
    public class BeanSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private int _beansCount = 2;

        private bool _isSpawning;
        private Collider _collider;

#if UNITY_SERVER && !UNITY_EDITOR
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

            if (beans.Length < _beansCount)
            {
                _isSpawning = true;
                await RespawnAsync(_beansCount - beans.Length);
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

                var instance = Instantiate(_enemyPrefab, position, new Quaternion(0f, 0f, 0f, 0f));
                var obj = instance.GetComponent<NetworkObject>();
                obj.Spawn();
            }

            _isSpawning = false;
            Debug.Log($"{count} beans spawned");
        }
#endif
    }
}