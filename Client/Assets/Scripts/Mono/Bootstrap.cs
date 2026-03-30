using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Mono
{
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
            if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
            {
                await UserManager.Instance.LoginAsync("user1@localhost", "User1!");
            }
            else
            {
                await UserManager.Instance.LoginAsync("user2@localhost", "User2!");
            }

            await QuestManager.Instance.LoadAsync();
            await QuestManager.Instance.LoadAsync(1);

            await SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            Debug.Log("MainScene Loaded");

            await SceneManager.LoadSceneAsync("UIScene", LoadSceneMode.Additive);
            Debug.Log("UIScene Loaded");

            await SceneManager.LoadSceneAsync("AudioScene", LoadSceneMode.Additive);
            Debug.Log("AudioScene Loaded");

            await SceneManager.LoadSceneAsync("EnvironmentScene", LoadSceneMode.Additive);
            Debug.Log("EnvironmentScene Loaded");

            await SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Additive);
            Debug.Log("TestScene Loaded");

            NetworkManager.Singleton.StartClient();

            Debug.Log("Client started");
        }

        private static async UniTask StartServer()
        {
            await UserManager.Instance.LoginAsync("server1@localhost", "Server1!");

            await QuestManager.Instance.LoadAsync();

            await SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            Debug.Log("MainScene Loaded");

            await SceneManager.LoadSceneAsync("ServerScene", LoadSceneMode.Additive);
            Debug.Log("ServerScene Loaded");

            await SceneManager.LoadSceneAsync("EnvironmentScene", LoadSceneMode.Additive);
            Debug.Log("EnvironmentScene Loaded");

            NetworkManager.Singleton.StartServer();

            Debug.Log("Server started");
        }
    }
}