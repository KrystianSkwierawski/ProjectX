using Unity.Netcode;
using UnityEngine;

public class ClientStarter : MonoBehaviour
{
    private void Start()
    {
        NetworkManager.Singleton.StartClient();

        Debug.Log("Client started");
    }
}
