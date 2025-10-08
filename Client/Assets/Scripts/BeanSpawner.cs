using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BeanSpawner : MonoBehaviour
{
    public GameObject EnemyPrefab;
    public int BeansCount = 2;
    private bool _isSpawning;

    void Update()
    {
#if UNITY_SERVER
        if (_isSpawning)
        {
            return;
        }

        var beans = GameObject.FindGameObjectsWithTag("Target");

        if (beans.Length < BeansCount)
        {
            _isSpawning = true;
            StartCoroutine(Respawn(BeansCount - beans.Length));
        }
#endif
    }

    private IEnumerator Respawn(int count)
    {
        yield return new WaitForSeconds(5);
        SpawnBeans(count);
    }

    private void SpawnBeans(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var offset = i + 1f;
            var position = new Vector3(offset, -3.5f, offset);

            var enemy = Instantiate(EnemyPrefab, position, new Quaternion(0f, 0f, 0f, 0f));
            var netObj = enemy.GetComponent<NetworkObject>();
            netObj.Spawn();
        }

        _isSpawning = false;
        Debug.Log($"{count} beans spawned");
    }
}
