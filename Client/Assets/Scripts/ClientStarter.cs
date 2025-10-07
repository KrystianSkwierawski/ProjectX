using Unity.Netcode;
using UnityEngine;

public class ClientStarter : MonoBehaviour
{
    void Start()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client started");

        //var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        //Debug.Log(transport);
        ////transport.ConnectionData.Address = "127.0.0.1";
        ////transport.ConnectionData.Port = 7777;

        //NetworkManager.Singleton.StartClient();
        //Debug.Log("Client started");
    }
}
