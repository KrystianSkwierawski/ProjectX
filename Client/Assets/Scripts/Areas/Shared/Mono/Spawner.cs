using System;
using System.Collections.Generic;
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
        [SerializeField] private float _transformY;
        [Tooltip("Replenish removed objects. Disable to spawn only once per instance.")]
        [SerializeField] private bool _maintainPopulation = true;

        private ObjectPool<GameObject> _pool;
        private readonly List<GameObject> _objects = new();
        private bool _isSpawning;
        private bool _initialized;
        private bool _stopped;
        private int _instanceId;
        private Collider _collider;

        public void ConfigureInstance(int instanceId)
        {
            _instanceId = instanceId;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.enabled = false;

            var view = GetComponent<Renderer>();

            if (view != null)
            {
                view.enabled = false;
            }
        }

#if UNITY_SERVER && !UNITY_EDITOR
        private void Start()
        {
            _pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var result = Instantiate(_prefab);
                    _objects.Add(result);

                    var marker = result.GetComponent<DungeonWorldObject>();

                    if (marker != null)
                    {
                        marker.InstanceId = _instanceId;
                    }

                    var key = result.GetInstanceID().ToString();
                    ReleasePoolSubscription.Instance.Subscribe(key, _ =>
                    {
                        if (!_stopped && result != null && result.GetComponent<NetworkObject>().IsSpawned)
                        {
                            _pool.Release(result);
                        }
                    });

                    return result;
                },
                actionOnRelease: go =>
                {
                    go.GetComponent<NetworkObject>().Despawn(false);
                    go.SetActive(false);
                },
                defaultCapacity: Math.Max(1, _count));
        }

        private async void Update()
        {
            if (_stopped || _isSpawning || _pool == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            var count = !_initialized ? _count : _maintainPopulation ? _pool.CountInactive : 0;

            if (count <= 0)
            {
                return;
            }

            _isSpawning = true;
            var token = this.GetCancellationTokenOnDestroy();

            try
            {
                if (_initialized)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: token);
                }

                if (token.IsCancellationRequested || _stopped || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                {
                    return;
                }

                var box = (BoxCollider)_collider;

                for (var i = 0; i < count; i++)
                {
                    var local = box.center + new Vector3(
                        UnityEngine.Random.Range(-box.size.x / 2, box.size.x / 2), 0,
                        UnityEngine.Random.Range(-box.size.z / 2, box.size.z / 2));
                    var position = transform.TransformPoint(local);
                    position.y = _transformY;

                    var spawned = _pool.Get();
                    spawned.transform.position = position;
                    spawned.SetActive(true);
                    spawned.GetComponent<NetworkObject>().Spawn();
                }

                _initialized = true;
                Debug.Log($"Spawner populated. Name: {name}, InstanceId: {_instanceId}, Count: {count}, MaintainPopulation: {_maintainPopulation}.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            finally
            {
                _isSpawning = false;
            }
        }
#endif

        public void StopSpawning()
        {
            _stopped = true;

            foreach (var spawned in _objects)
            {
                if (spawned == null)
                {
                    continue;
                }

                ReleasePoolSubscription.Instance.Unsubscribe(spawned.GetInstanceID().ToString());
                var networkObject = spawned.GetComponent<NetworkObject>();

                if (networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
                else
                {
                    Destroy(spawned);
                }
            }

            _objects.Clear();
        }

        private void OnDestroy()
        {
            StopSpawning();
        }
    }
}
