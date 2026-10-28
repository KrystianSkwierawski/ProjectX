using System;
using System.Reflection;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectX.Editor
{
    [InitializeOnLoad]
    public static class ClientDisconnectValidation
    {
        private const string PendingKey = "ProjectX.ClientDisconnectValidation";

        static ClientDisconnectValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
                {
                    SessionState.SetBool(PendingKey, false);
                    ValidateAsync().Forget();
                }
            };
        }

        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static async UniTask ValidateAsync()
        {
            try
            {
                var network = new GameObject("DisconnectProbeNetwork").AddComponent<NetworkManager>();
                var monitor = network.gameObject.AddComponent<ClientConnectionMonitor>();
                var notifications = 0;
                string message = null;
                UserManager.Instance.SessionInvalidated += value => { notifications++; message = value; };

                SetConnected(network, true);
                UserManager.Instance.SetCharacterLanguage(LanguageEnum.pl);
                UserManager.Instance.Characters[network.LocalClientId] = new CharacterDto();
                monitor.Initialize(network);
                monitor.Initialize(network);

                EmitDisconnected(network, network.LocalClientId + 1);
                await UniTask.Yield();
                Require(notifications == 0, "Another client must not end the local session");

                EmitDisconnected(network, network.LocalClientId);
                EmitTransportFailure(network);
                Require(notifications == 0, "Teardown must leave the NGO callback first");
                await UniTask.DelayFrame(2);
                Require(notifications == 1, "Disconnect and transport failure must notify once");
                Require(UserManager.Instance.Characters.Count == 0 && UserManager.Instance.SelectedCharacterId == 0
                    && UserManager.Instance.Token == null, "Disconnected session state must be cleared");
                Require(message == "Utracono połączenie z serwerem gry. Zaloguj się ponownie.", "Localized reason must survive session clearing");

                SetConnected(network, true);
                monitor.Initialize(network);
                EmitTransportFailure(network);
                await UniTask.DelayFrame(2);
                Require(notifications == 2, "Transport failure alone must end a later session");

                SetConnected(network, true);
                monitor.Initialize(network);
                EmitDisconnected(network, network.LocalClientId);
                UserManager.Instance.InvalidateSession("Explicit logout");
                await UniTask.DelayFrame(2);
                Require(notifications == 3 && message == "Explicit logout", "Logout must cancel a queued disconnect");

                SetConnected(network, false);
                monitor.Initialize(network);
                await UniTask.DelayFrame(2);
                Require(notifications == 4, "Loss during final startup transition must return to login");

                SetConnected(network, true);
                monitor.Initialize(network);
                typeof(ClientConnectionMonitor).GetMethod("OnApplicationQuit", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(monitor, null);
                EmitDisconnected(network, network.LocalClientId);
                await UniTask.DelayFrame(2);
                Require(notifications == 4, "Application quit must not return to login");

                monitor.Initialize(network);
                EmitDisconnected(network, network.LocalClientId);
                UnityEngine.Object.Destroy(monitor);
                await UniTask.DelayFrame(2);
                Require(notifications == 4, "Destroyed monitor must cancel pending work");

                SetConnected(network, false);
                UnityEngine.Object.Destroy(network.gameObject);
                await ValidateSceneReturnAsync();
                await ValidateStartupRollbackAsync();

                Debug.Log("CLIENT_DISCONNECT_VALIDATED: NGO event subscriptions, deduplication, deferred cleanup, localization, reinitialization, startup gap, logout, quit and destroy cancellation.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static async UniTask ValidateSceneReturnAsync()
        {
            await SceneManager.LoadSceneAsync("LoadingScene", LoadSceneMode.Single);
            await UniTask.WaitUntil(() => LoginUI.Instance != null).Timeout(TimeSpan.FromSeconds(20));

            for (var attempt = 0; attempt < 2; attempt++)
            {
                await SceneManager.UnloadSceneAsync("BootstrapScene");
                var main = SceneManager.CreateScene("MainScene");
                SceneManager.CreateScene("UIScene");
                SceneManager.CreateScene("EnvironmentScene");
                SceneManager.SetActiveScene(main);
                var network = new GameObject("SceneReturnNetwork").AddComponent<NetworkManager>();
                var monitor = network.gameObject.AddComponent<ClientConnectionMonitor>();

                UserManager.Instance.SetCharacterLanguage(LanguageEnum.pl);
                monitor.Initialize(network);

                await UniTask.WaitUntil(() => LoginUI.Instance != null
                    && !SceneManager.GetSceneByName("MainScene").isLoaded
                    && !SceneManager.GetSceneByName("UIScene").isLoaded
                    && !SceneManager.GetSceneByName("EnvironmentScene").isLoaded)
                    .Timeout(TimeSpan.FromSeconds(20));

                Require(SceneManager.GetSceneByName("LoadingScene").isLoaded, "Persistent loading scene must survive logout");
                Require(SceneManager.GetActiveScene().name == "BootstrapScene", "Login scene must become active");
                Require(NetworkManager.Singleton == null, "Old persistent NGO manager must not survive into the next login");
                await UniTask.DelayFrame(2);
            }

            Debug.Log("CLIENT_DISCONNECT_SCENES_VALIDATED: two returns through actual LoadingScene/BootstrapScene with authenticated-scene teardown.");
        }

        private static async UniTask ValidateStartupRollbackAsync()
        {
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<Bootstrap>();
            var rollback = typeof(Bootstrap).GetMethod("RollbackClientStartupAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            await ((UniTask)rollback.Invoke(bootstrap, new object[] { Array.Empty<string>() }));

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var main = SceneManager.CreateScene("MainScene");
                SceneManager.CreateScene("UIScene");
                SceneManager.SetActiveScene(main);
                var network = new GameObject("FailedStartupNetwork").AddComponent<NetworkManager>();

                Require(NetworkManager.Singleton == network, "Retry must get a fresh singleton");

                await ((UniTask)rollback.Invoke(bootstrap, new object[] { new[] { "MainScene", "UIScene" } }))
                    .Timeout(TimeSpan.FromSeconds(20));

                Require(network == null && NetworkManager.Singleton == null, "Failed startup must destroy persistent manager");
                Require(!SceneManager.GetSceneByName("MainScene").isLoaded
                    && !SceneManager.GetSceneByName("UIScene").isLoaded, "Failed startup must unload its scenes");
                Require(LoginUI.Instance != null && SceneManager.GetActiveScene().name == "BootstrapScene", "Rollback must preserve login");
            }

            Debug.Log("CLIENT_STARTUP_ROLLBACK_VALIDATED: pre-network failure and two consecutive failed startups dispose NGO and preserve login.");
        }

        private static void SetConnected(NetworkManager network, bool connected)
        {
            typeof(NetworkManager).GetProperty(nameof(NetworkManager.IsConnectedClient)).SetValue(network, connected);
        }

        private static object ConnectionManager(NetworkManager network) => typeof(NetworkManager)
            .GetField("ConnectionManager", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(network);

        private static void EmitDisconnected(NetworkManager network, ulong clientId)
        {
            var connection = ConnectionManager(network);
            var callback = connection.GetType().GetField("OnClientDisconnectCallback", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(connection) as Action<ulong>;

            callback?.Invoke(clientId);
        }

        private static void EmitTransportFailure(NetworkManager network)
        {
            var connection = ConnectionManager(network);
            var callback = connection.GetType().GetField("OnTransportFailure", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(connection) as Action;

            callback?.Invoke();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
