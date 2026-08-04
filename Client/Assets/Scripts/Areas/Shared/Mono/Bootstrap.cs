using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Quest;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public sealed class Bootstrap : MonoBehaviour
    {
        private bool _isLoginInProgress;

        private async void Start()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            try
            {
                await StartServerAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
#else
            InitializeClientLogin();
#endif
        }

        private void InitializeClientLogin()
        {
            LoginUI.Instance.LoginRequested += HandleLoginRequested;
            PrefillDevelopmentCredentials();
        }

        private void HandleLoginRequested(string email, string password)
        {
            if (_isLoginInProgress)
            {
                return;
            }

            LoginAndStartClientAsync(email, password).Forget();
        }

        private async UniTask LoginAndStartClientAsync(string email, string password)
        {
            _isLoginInProgress = true;
            LoginUI.Instance.SetLoading(true);

            try
            {
                await UserManager.Instance.LoginAsync(email, password, this.GetCancellationTokenOnDestroy());
            }
            catch (ApiRequestException exception)
            {
                Debug.LogWarning($"Login request failed. Result: {exception.Result}, HTTP: {exception.ResponseCode}, Error: {exception.Message}");

                ShowLoginError(GetLoginErrorMessage(exception));

                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                ShowLoginError("An unexpected error occurred. Please try again.");

                return;
            }

            try
            {
                await StartAuthenticatedClientAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                ShowLoginError("Login succeeded, but the game could not be started. Please try again.");
            }
        }

        private void ShowLoginError(string message)
        {
            _isLoginInProgress = false;
            LoginUI.Instance.ShowRequestError(message);
        }

        private static string GetLoginErrorMessage(ApiRequestException exception)
        {
            if (exception.Result == UnityWebRequest.Result.ConnectionError)
            {
                return "Could not connect to the server. Check that the API is running.";
            }

            if (exception.ResponseCode == 401 || exception.ResponseCode == 403)
            {
                return "Incorrect email or password.";
            }

            if (exception.ResponseCode == 400)
            {
                return "Check the email and password and try again.";
            }

            if (exception.Result == UnityWebRequest.Result.DataProcessingError)
            {
                return "The server response could not be read. Please try again.";
            }

            if (exception.ResponseCode >= 500)
            {
                return "The server is temporarily unavailable. Please try again.";
            }

            return "Login failed. Please try again.";
        }

        #region Temporary development credentials

        private void PrefillDevelopmentCredentials()
        {
#if UNITY_EDITOR
            if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
            {
                LoginUI.Instance.PrefillDevelopmentCredentials("user1@localhost", "User1!");
            }
            else
            {
                LoginUI.Instance.PrefillDevelopmentCredentials("user2@localhost", "User2!");
            }
#elif DEVELOPMENT_BUILD
            LoginUI.Instance.PrefillDevelopmentCredentials("user1@localhost", "User1!");
#endif
        }

        #endregion

        private static async UniTask StartAuthenticatedClientAsync()
        {
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

            if (!NetworkManager.Singleton.StartClient())
            {
                throw new InvalidOperationException("The network client could not be started.");
            }

            Debug.Log("Client started");
        }

        private static async UniTask StartServerAsync()
        {
            await UserManager.Instance.LoginAsync("server1@localhost", "Server1!");

            await QuestManager.Instance.LoadAsync();

            await SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            Debug.Log("MainScene Loaded");

            await SceneManager.LoadSceneAsync("ServerScene", LoadSceneMode.Additive);
            Debug.Log("ServerScene Loaded");

            await SceneManager.LoadSceneAsync("EnvironmentScene", LoadSceneMode.Additive);
            Debug.Log("EnvironmentScene Loaded");

            if (!NetworkManager.Singleton.StartServer())
            {
                throw new InvalidOperationException("The network server could not be started.");
            }

            Debug.Log("Server started");
        }
    }
}
