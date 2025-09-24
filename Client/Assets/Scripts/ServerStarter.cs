using Unity.Netcode;
using UnityEngine;

public class ServerStarter : MonoBehaviour
{
    void Start()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server started");
    }
}
