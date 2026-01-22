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

        // TODO: task.whenall?
        private static async UniTask StartClient()
        {
    
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

            await SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            Debug.Log("MainScene Loaded");

            await SceneManager.LoadSceneAsync("UIScene", LoadSceneMode.Additive);
            Debug.Log("UIScene Loaded");

            await SceneManager.LoadSceneAsync("AudioScene", LoadSceneMode.Additive);
            Debug.Log("AudioScene Loaded");

#if UNITY_EDITOR
            await SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Additive);
            Debug.Log("TestScene Loaded");
#endif

            await SceneManager.LoadSceneAsync("NPCScene", LoadSceneMode.Additive);
            Debug.Log("NPCScene Loaded");

            NetworkManager.Singleton.StartClient();

            Debug.Log("Client started");
        }

        private static async UniTask StartServer()
        {
            await TokenManager.Instance.LoginAsync("server1@localhost", "Server1!");

            await QuestManager.Instance.LoadQuestsAsync();

            await SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            Debug.Log("MainScene Loaded");

            await SceneManager.LoadSceneAsync("ServerScene", LoadSceneMode.Additive);
            Debug.Log("ServerScene Loaded");

            NetworkManager.Singleton.StartServer();

            Debug.Log("Server started");
        }
    }
}