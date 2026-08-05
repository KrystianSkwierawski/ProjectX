using System;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public sealed class LoadingSceneBootstrap : MonoBehaviour
    {
        private const string _bootstrapSceneName = "BootstrapScene";

        private async void Start()
        {
#if !UNITY_SERVER || UNITY_EDITOR
            using (var loadingScope = LoadingScreenUI.Instance.Show("Loading..."))
            {
                try
                {
                    await UniTask.WaitForSeconds(2);
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
    }
}
