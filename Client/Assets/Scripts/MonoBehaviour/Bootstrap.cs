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
        await SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
        Debug.Log("MainScene Loaded");

        await SceneManager.LoadSceneAsync("UIScene", LoadSceneMode.Additive);
        Debug.Log("UIScene Loaded");

        await SceneManager.LoadSceneAsync("AudioScene", LoadSceneMode.Additive);
        Debug.Log("AudioScene Loaded");

        UIManager.Instance.Init();

        if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
        {
            await TokenManager.Instance.LoginAsync("user1@localhost", "User1!");
        }
        else
        {
            await TokenManager.Instance.LoginAsync("user2@localhost", "User2!");
        }

        await QuestManager.Instance.LoadQuestsAsync();
        await QuestManager.Instance.LoadCharacterQuestsAsync();

        await SceneManager.LoadSceneAsync("EnvironmentScene", LoadSceneMode.Additive);
        Debug.Log("EnvironmentScene Loaded");

        await SceneManager.LoadSceneAsync("NpcScene", LoadSceneMode.Additive);
        Debug.Log("NpcScene Loaded");

        NetworkManager.Singleton.StartClient();

        Debug.Log("Client started");
    }

    private static async UniTask StartServer()
    {
        await SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
        Debug.Log("MainScene Loaded");

        await SceneManager.LoadSceneAsync("ServerScene", LoadSceneMode.Additive);
        Debug.Log("ServerScene Loaded");

        await TokenManager.Instance.LoginAsync("server1@localhost", "Server1!");

        NetworkManager.Singleton.StartServer();

        Debug.Log("Server started");
    }
}
