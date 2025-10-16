using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private async void Start()
    {
#if UNITY_EDITOR
        await StartClient();
#elif UNITY_SERVER && !UNITY_EDITOR
        await StartServer();
#endif
    }

    private static async UniTask StartClient()
    {
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        Debug.Log("MainScene Loaded");

        SceneManager.LoadScene("EnvironmentScene", LoadSceneMode.Additive);
        Debug.Log("EnvironmentScene Loaded");

        SceneManager.LoadScene("UIScene", LoadSceneMode.Additive);
        Debug.Log("UIScene Loaded");

        SceneManager.LoadScene("NPCScene", LoadSceneMode.Additive);
        Debug.Log("NPCScene Loaded");

        if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
        {
            await TokenManager.Instance.LoginAsync("user1@localhost", "User1!");
        }
        else
        {
            await TokenManager.Instance.LoginAsync("user2@localhost", "User2!");
        }

        NetworkManager.Singleton.StartClient();

        Debug.Log("Client started");
    }

    private static async UniTask StartServer()
    {
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        Debug.Log("MainScene Loaded");

        SceneManager.LoadScene("ServerScene", LoadSceneMode.Additive);
        Debug.Log("ServerScene Loaded");

        await TokenManager.Instance.LoginAsync("server1@localhost", "Server1!");

        NetworkManager.Singleton.StartServer();

        Debug.Log("Server started");
    }
}
