using Unity.Netcode;
using UnityEngine;

public class ClientServerStarter : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client started");
#elif UNITY_SERVER && !UNITY_EDITOR
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server started");
#endif
    }
}
