using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

#if UNITY_SERVER && !UNITY_EDITOR
        private static readonly object _serverRuntimeLogLock = new object();
        private static StreamWriter _serverRuntimeLogWriter;
#endif

        private static readonly string[] _clientSceneNames =
        {
            _mainSceneName,
            "UIScene",
            "AudioScene",
            "EnvironmentScene",
            "TestScene"
        };

        private bool _isLoginInProgress;

#if UNITY_SERVER && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartServerRuntimeLogging()
        {
            var runtimeLogPath = Environment.GetEnvironmentVariable("PROJECTX_RUNTIME_LOG_PATH");

            if (string.IsNullOrWhiteSpace(runtimeLogPath))
            {
                return;
            }

            runtimeLogPath = Path.GetFullPath(runtimeLogPath);

            Directory.CreateDirectory(Path.GetDirectoryName(runtimeLogPath)
                ?? throw new InvalidOperationException("Invalid server runtime log path."));

            lock (_serverRuntimeLogLock)
            {
                _serverRuntimeLogWriter = new StreamWriter(runtimeLogPath, append: false)
                {
                    AutoFlush = true
                };
                _serverRuntimeLogWriter.WriteLine($"{DateTimeOffset.Now:O} [Session] Dedicated server started.");
            }

            Application.logMessageReceivedThreaded += WriteServerRuntimeLog;
            Application.quitting += StopServerRuntimeLogging;

            Debug.Log($"Unity server runtime log: {runtimeLogPath}");
        }

        private static void StopServerRuntimeLogging()
        {
            Application.logMessageReceivedThreaded -= WriteServerRuntimeLog;
            Application.quitting -= StopServerRuntimeLogging;

            lock (_serverRuntimeLogLock)
            {
                if (_serverRuntimeLogWriter == null)
                {
                    return;
                }

                _serverRuntimeLogWriter.WriteLine($"{DateTimeOffset.Now:O} [Session] Dedicated server stopped.");
                _serverRuntimeLogWriter.Dispose();
                _serverRuntimeLogWriter = null;
            }
        }

        private static void WriteServerRuntimeLog(string condition, string stackTrace, LogType type)
        {
            lock (_serverRuntimeLogLock)
            {
                if (_serverRuntimeLogWriter == null)
                {
                    return;
                }

                try
                {
                    _serverRuntimeLogWriter.WriteLine($"{DateTimeOffset.Now:O} [{type}] {condition}");

                    if (!string.IsNullOrWhiteSpace(stackTrace)
                        && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    {
                        _serverRuntimeLogWriter.WriteLine(stackTrace);
                    }
                }
                catch (IOException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
#endif

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
                await UserManager.Instance.LoadCharactersAsync();

                #region Temporary automatic character selection

                var character = UserManager.Instance.AvailableCharacters.FirstOrDefault()
                    ?? throw new InvalidOperationException("The authenticated user has no playable characters.");

                UserManager.Instance.SelectCharacter(character.Id);

                #endregion

                await QuestManager.Instance.LoadAsync();

                await QuestManager.Instance.LoadCharacterQuestsAsync(UserManager.Instance.SelectedCharacterId);

                foreach (var sceneName in _clientSceneNames)
                {
                    await LoadSceneAdditivelyAsync(sceneName, loadedSceneNames);
                }

                if (!SceneManager.SetActiveScene(SceneManager.GetSceneByName(_mainSceneName)))
                {
                    throw new InvalidOperationException("MainScene could not be activated.");
                }

                await GameSessionManager.StartClientAsync(UserManager.Instance.SelectedCharacterId);

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
