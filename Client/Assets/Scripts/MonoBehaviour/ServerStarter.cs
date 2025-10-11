using Unity.Netcode;
using UnityEngine;

public class ServerStarter : MonoBehaviour
{
#if UNITY_SERVER && !UNITY_EDITOR
    private void Start()
    {
        NetworkManager.Singleton.StartServer();

        Debug.Log("Server started");
    }
#endif
}
