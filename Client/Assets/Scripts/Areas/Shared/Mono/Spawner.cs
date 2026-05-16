using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _count = 2;
        [SerializeField] private float _transformY; // FIXME: auto calc y

        private ObjectPool<GameObject> _pool;

        private bool _isSpawning;
        private Collider _collider;


#if UNITY_SERVER && !UNITY_EDITOR
        private void Start()
        {
            _collider = GetComponent<Collider>();

            _pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var result = Instantiate(_prefab);

                    var instanceId = result.GetInstanceID().ToString();

                    ReleasePoolSubscription.Instance.Subscribe(instanceId, (e) =>
                    {
                        Debug.Log($"Releasing to pool. GameObjectName: {result.name}, InstanceId: {instanceId}");
                        _pool.Release(result);
                    });

                    return result;
                },
                actionOnGet: (GameObject gameObject) => gameObject.GetComponent<NetworkObject>().Spawn(),
                actionOnRelease: (GameObject gameObject) => gameObject.GetComponent<NetworkObject>().Despawn(false),
                defaultCapacity: _count
            );
        }

        private async void Update()
        {
            if (_isSpawning)
            {
                return;
            }

            var init = _pool.CountAll == 0;
            var inactive = _pool.CountInactive;

            if (init || inactive > 0)
            {
                _isSpawning = true;
                await RespawnAsync(init ? _count : inactive);
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
                var bean = _pool.Get();

                var position = new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x), _transformY, UnityEngine.Random.Range(bounds.min.z, bounds.max.z));

                bean.transform.position = position;
            }

            _isSpawning = false;
            Debug.Log($"{count} {_prefab.name} spawned");
        }
#endif
    }
}