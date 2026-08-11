using System;
using System.Collections.Generic;
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
        private const string _mainSceneName = "MainScene";

        private static readonly string[] _clientSceneNames =
        {
            _mainSceneName,
            "UIScene",
            "AudioScene",
            "EnvironmentScene",
            "TestScene"
        };

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
                Application.Quit(1);
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
                using (var loadingScope = LoadingScreenUI.Instance.Show("Loading game..."))
                {
                    await StartAuthenticatedClientAsync();
                }
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

            if (exception.ResponseCode == 429)
            {
                return "Too many login attempts. Please wait a moment and try again.";
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

        private async UniTask StartAuthenticatedClientAsync()
        {
            var loadedSceneNames = new List<string>(_clientSceneNames.Length);

            try
            {
                await QuestManager.Instance.LoadAsync();
                await QuestManager.Instance.LoadAsync(1);

                foreach (var sceneName in _clientSceneNames)
                {
                    await LoadSceneAdditivelyAsync(sceneName, loadedSceneNames);
                }

                if (!SceneManager.SetActiveScene(SceneManager.GetSceneByName(_mainSceneName)))
                {
                    throw new InvalidOperationException("MainScene could not be activated.");
                }

                await GameSessionManager.StartClientAsync();

                Debug.Log("Client started");

                await SceneManager.UnloadSceneAsync(gameObject.scene);
            }
            catch
            {
                await RollbackClientStartupAsync(loadedSceneNames);

                throw;
            }
        }

        private static async UniTask LoadSceneAdditivelyAsync(string sceneName, List<string> loadedSceneNames)
        {
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            loadedSceneNames.Add(sceneName);

            Debug.Log($"{sceneName} Loaded");
        }

        private async UniTask RollbackClientStartupAsync(IReadOnlyList<string> loadedSceneNames)
        {
            SceneManager.SetActiveScene(gameObject.scene);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            for (var i = loadedSceneNames.Count - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneByName(loadedSceneNames[i]);

                if (scene.IsValid() && scene.isLoaded)
                {
                    await SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private static async UniTask StartServerAsync()
        {
            var serverUserName = Environment.GetEnvironmentVariable("PROJECTX_SERVER_USERNAME");
            var serverPassword = Environment.GetEnvironmentVariable("PROJECTX_SERVER_PASSWORD");

            if (string.IsNullOrWhiteSpace(serverUserName) || string.IsNullOrWhiteSpace(serverPassword))
            {
                throw new InvalidOperationException("PROJECTX_SERVER_USERNAME and PROJECTX_SERVER_PASSWORD are required for the dedicated server.");
            }

            await UserManager.Instance.LoginAsync(serverUserName, serverPassword);

            await QuestManager.Instance.LoadAsync();

            await SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            Debug.Log("MainScene Loaded");

            await SceneManager.LoadSceneAsync("ServerScene", LoadSceneMode.Additive);
            Debug.Log("ServerScene Loaded");

            await SceneManager.LoadSceneAsync("EnvironmentScene", LoadSceneMode.Additive);
            Debug.Log("EnvironmentScene Loaded");

            await GameSessionManager.StartServerAsync();

            Debug.Log("Server started");
        }
    }
}
