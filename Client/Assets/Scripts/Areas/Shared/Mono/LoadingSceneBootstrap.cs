using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public sealed class LoadingSceneBootstrap : MonoBehaviour
    {
        private const string _bootstrapSceneName = "BootstrapScene";

        private static readonly string[] _authenticatedSceneNames =
        {
            "MainScene",
            "UIScene",
            "AudioScene",
            "EnvironmentScene",
            "TestScene"
        };

        private bool _isReturningToLogin;

        private void Awake()
        {
            UserManager.Instance.SessionInvalidated += HandleSessionInvalidated;
        }

        private async void Start()
        {
#if !UNITY_SERVER || UNITY_EDITOR
            using (var loadingScope = LoadingScreenUI.Instance.Show("Loading..."))
            {
                try
                {
                    await LoadBootstrapSceneAsync();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
#endif
        }

        private static async UniTask LoadBootstrapSceneAsync()
        {
            var bootstrapScene = SceneManager.GetSceneByName(_bootstrapSceneName);

            if (!bootstrapScene.IsValid() || !bootstrapScene.isLoaded)
            {
                await SceneManager.LoadSceneAsync(_bootstrapSceneName, LoadSceneMode.Additive);
                bootstrapScene = SceneManager.GetSceneByName(_bootstrapSceneName);
            }

            if (!SceneManager.SetActiveScene(bootstrapScene))
            {
                throw new InvalidOperationException("BootstrapScene could not be activated.");
            }
        }

        private void HandleSessionInvalidated(string message)
        {
            ReturnToLoginAsync(message).Forget();
        }

        private async UniTask ReturnToLoginAsync(string message)
        {
            if (_isReturningToLogin)
            {
                return;
            }

            _isReturningToLogin = true;

            try
            {
                using (LoadingScreenUI.Instance.Show("Signing out..."))
                {
                    var networkManager = NetworkManager.Singleton;

                    if (networkManager != null && networkManager.IsListening)
                    {
                        networkManager.Shutdown();
                    }

                    var loadingScene = gameObject.scene;

                    if (loadingScene.IsValid() && loadingScene.isLoaded)
                    {
                        SceneManager.SetActiveScene(loadingScene);
                    }

                    for (var i = _authenticatedSceneNames.Length - 1; i >= 0; i--)
                    {
                        var scene = SceneManager.GetSceneByName(_authenticatedSceneNames[i]);

                        if (scene.IsValid() && scene.isLoaded)
                        {
                            await SceneManager.UnloadSceneAsync(scene);
                        }
                    }

                    await LoadBootstrapSceneAsync();
                }

                LoginUI.Instance.ShowRequestError(message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isReturningToLogin = false;
            }
        }

        private void OnDestroy()
        {
            UserManager.Instance.SessionInvalidated -= HandleSessionInvalidated;
        }
    }
}
