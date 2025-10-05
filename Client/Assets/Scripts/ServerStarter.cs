using Unity.Netcode;
using UnityEngine;

public class ServerStarter : MonoBehaviour
{
    public GameObject EnemyPrefab;
    public int BeansCount = 2;

    void Awake()
    {
        NetworkManager.Singleton.OnServerStarted += SpawnBeans;
    }

    void Start()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server started");
    }

    private void SpawnBeans()
    {
        for (int i = 1; i <= BeansCount; i++)
        {
            var offset = i + 1f;
            var position = new Vector3(offset, -3.5f, offset);

            var enemy = Instantiate(EnemyPrefab, position, new Quaternion(0f, 0f, 0f, 0f));
            var netObj = enemy.GetComponent<NetworkObject>();
            netObj.Spawn();
        }

        Debug.Log($"{BeansCount} beans spawned");
    }
}
