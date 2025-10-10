using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BeanSpawner : MonoBehaviour
{
    public GameObject EnemyPrefab;
    public int BeansCount = 2;

    private bool _isSpawning;
    private Collider _collider;

    private void Start()
    {
        _collider = GetComponent<Collider>();
    }

    void Update()
    {
        if (_isSpawning || NetworkManager.Singleton.IsClient)
        {
            return;
        }

        var beans = GameObject.FindGameObjectsWithTag("Target");

        if (beans.Length < BeansCount)
        {
            _isSpawning = true;
            StartCoroutine(Respawn(BeansCount - beans.Length));
        }
    }

    private IEnumerator Respawn(int count)
    {
        yield return new WaitForSeconds(5);
        SpawnBeans(count);
    }

    private void SpawnBeans(int count)
    {
        var bounds = _collider.bounds;

        for (int i = 1; i <= count; i++)
        {
            var position = new Vector3(Random.Range(bounds.min.x, bounds.max.x), -3.5f, Random.Range(bounds.min.z, bounds.max.z));

            var enemy = Instantiate(EnemyPrefab, position, new Quaternion(0f, 0f, 0f, 0f));
            var netObj = enemy.GetComponent<NetworkObject>();
            netObj.Spawn();
        }

        _isSpawning = false;
        Debug.Log($"{count} beans spawned");
    }
}

