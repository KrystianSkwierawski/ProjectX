using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectX.Editor
{
    [InitializeOnLoad]
    public static class ProjectXDevAutomation
    {
        private const string BootstrapScenePath = "Assets/Scenes/BootstrapScene.unity";
        private const string LoadingScenePath = "Assets/Scenes/LoadingScene.unity";
        private const string PlayClientRequestPath = "Temp/ProjectXAutomation/play-client.request";
        private const string BuildServerRequestPath = "Temp/ProjectXAutomation/build-server.request";
        private const string BuildServerStatusPath = "Temp/ProjectXAutomation/build-server.status";
        private const string DedicatedServerBuildProfilePath = "Assets/Settings/Build Profiles/DedicatedServer.asset";
        private const string PlayAfterExitEditorPref = "ProjectX.DevAutomation.PlayAfterExit";
        private const string BuildAfterExitEditorPref = "ProjectX.DevAutomation.BuildAfterExit";
        private const string BuildAfterExitPathEditorPref = "ProjectX.DevAutomation.BuildAfterExitPath";
        private const string RuntimeLogDirectoryPath = "Logs/Runtime";
        private static readonly object ClientRuntimeLogLock = new object();
        private static StreamWriter _clientRuntimeLogWriter;
        private static string _clientRuntimeLogPath;
        private static readonly string[] ClientRuntimeScenePaths =
        {
            LoadingScenePath,
            BootstrapScenePath,
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/UIScene.unity",
            "Assets/Scenes/AudioScene.unity",
            "Assets/Scenes/EnvironmentScene.unity",
            "Assets/Scenes/TestScene.unity"
        };
        private static readonly string[] ServerRuntimeScenePaths =
        {
            BootstrapScenePath,
            "Assets/Scenes/EnvironmentScene.unity",
            "Assets/Scenes/AudioScene.unity",
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/ServerScene.unity",
            "Assets/Scenes/TestScene.unity",
            "Assets/Scenes/UIScene.unity"
        };

        static ProjectXDevAutomation()
        {
            EditorApplication.update += WatchAutomationRequests;
            EditorApplication.delayCall += ResumeAutomationAfterExitIfNeeded;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.quitting += StopClientRuntimeLogging;
            AssemblyReloadEvents.beforeAssemblyReload += StopClientRuntimeLogging;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.delayCall += StartClientRuntimeLogging;
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                StartClientRuntimeLogging();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopClientRuntimeLogging();
            }
        }

        private static void StartClientRuntimeLogging()
        {
            lock (ClientRuntimeLogLock)
            {
                if (_clientRuntimeLogWriter != null)
                {
                    return;
                }

                var runtimeLogDirectory = GetProjectRelativeFullPath(RuntimeLogDirectoryPath);

                Directory.CreateDirectory(runtimeLogDirectory);

                var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff");

                _clientRuntimeLogPath = Path.Combine(runtimeLogDirectory, $"ProjectXClient-{timestamp}.log");
                _clientRuntimeLogWriter = new StreamWriter(_clientRuntimeLogPath, append: false)
                {
                    AutoFlush = true
                };
                _clientRuntimeLogWriter.WriteLine($"{DateTimeOffset.Now:O} [Session] Client Play Mode started.");
            }

            Application.logMessageReceivedThreaded -= WriteClientRuntimeLog;
            Application.logMessageReceivedThreaded += WriteClientRuntimeLog;

            Debug.Log($"Unity client runtime log: {_clientRuntimeLogPath}");
        }

        private static void StopClientRuntimeLogging()
        {
            Application.logMessageReceivedThreaded -= WriteClientRuntimeLog;

            lock (ClientRuntimeLogLock)
            {
                if (_clientRuntimeLogWriter == null)
                {
                    return;
                }

                _clientRuntimeLogWriter.WriteLine($"{DateTimeOffset.Now:O} [Session] Client Play Mode stopped.");
                _clientRuntimeLogWriter.Dispose();
                _clientRuntimeLogWriter = null;
                _clientRuntimeLogPath = null;
            }
        }

        private static void WriteClientRuntimeLog(string condition, string stackTrace, LogType type)
        {
            lock (ClientRuntimeLogLock)
            {
                if (_clientRuntimeLogWriter == null)
                {
                    return;
                }

                try
                {
                    _clientRuntimeLogWriter.WriteLine($"{DateTimeOffset.Now:O} [{type}] {condition}");

                    if (!string.IsNullOrWhiteSpace(stackTrace)
                        && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    {
                        _clientRuntimeLogWriter.WriteLine(stackTrace);
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

        [MenuItem("ProjectX/Open Bootstrap Scene", false, 50)]
        public static void OpenBootstrapSceneMenu()
        {
            OpenBootstrapSceneWithPrompt();
        }

        [MenuItem("ProjectX/Run", false, 10)]
        public static void RunFromUnityMenu()
        {
            RunAutomationFromUnity("-SkipServerBuild");
        }

        [MenuItem("ProjectX/Build And Run", false, 11)]
        public static void BuildAndRunFromUnityMenu()
        {
            RunAutomationFromUnity();
        }

        [MenuItem("ProjectX/Play Client From Loading Scene", false, 51)]
        public static void PlayClientFromLoadingScene()
        {
            QueuePlayClient();
        }

        [MenuItem("ProjectX/Build Dedicated Server", false, 52)]
        public static void BuildDedicatedServerMenu()
        {
            BuildDedicatedServer(GetDefaultServerBuildPath());
        }

        public static void BuildDedicatedServerFromCommandLine()
        {
            BuildDedicatedServer(GetCommandLineValue("-buildOutputPath", GetDefaultServerBuildPath()));
        }

        private static void WatchAutomationRequests()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            WatchBuildServerRequest();

            WatchPlayClientRequest();
        }

        private static void WatchBuildServerRequest()
        {
            var requestPath = GetProjectRelativeFullPath(BuildServerRequestPath);
            if (!File.Exists(requestPath))
            {
                return;
            }

            var outputPath = File.ReadAllText(requestPath).Trim();

            try
            {
                File.Delete(requestPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ProjectX automation could not delete server build request: {ex.Message}");
            }

            QueueBuildDedicatedServer(outputPath);
        }

        private static void WatchPlayClientRequest()
        {
            var requestPath = GetProjectRelativeFullPath(PlayClientRequestPath);
            if (!File.Exists(requestPath))
            {
                return;
            }

            try
            {
                File.Delete(requestPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ProjectX automation could not delete play request: {ex.Message}");
            }

            QueuePlayClient();
        }

        private static void ResumeAutomationAfterExitIfNeeded()
        {
            if (!EditorPrefs.GetBool(PlayAfterExitEditorPref, false) && !EditorPrefs.GetBool(BuildAfterExitEditorPref, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += ResumeAutomationAfterExitIfNeeded;
                return;
            }

            if (EditorPrefs.GetBool(BuildAfterExitEditorPref, false))
            {
                var outputPath = EditorPrefs.GetString(BuildAfterExitPathEditorPref, GetDefaultServerBuildPath());

                EditorPrefs.DeleteKey(BuildAfterExitEditorPref);
                EditorPrefs.DeleteKey(BuildAfterExitPathEditorPref);

                BuildDedicatedServerWithStatus(outputPath);
            }

            if (!EditorPrefs.GetBool(PlayAfterExitEditorPref, false))
            {
                return;
            }

            EditorPrefs.DeleteKey(PlayAfterExitEditorPref);

            StartPlayClientFromEditMode();
        }

        private static void QueueBuildDedicatedServer(string outputPath)
        {
            if (EditorApplication.isPlaying)
            {
                EditorPrefs.SetBool(BuildAfterExitEditorPref, true);
                EditorPrefs.SetString(BuildAfterExitPathEditorPref, outputPath);

                EditorApplication.ExitPlaymode();

                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorPrefs.SetBool(BuildAfterExitEditorPref, true);
                EditorPrefs.SetString(BuildAfterExitPathEditorPref, outputPath);
                return;
            }

            BuildDedicatedServerWithStatus(outputPath);
        }

        private static void QueuePlayClient()
        {
            if (EditorApplication.isPlaying)
            {
                EditorPrefs.SetBool(PlayAfterExitEditorPref, true);

                EditorApplication.ExitPlaymode();

                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorPrefs.SetBool(PlayAfterExitEditorPref, true);
                return;
            }

            StartPlayClientFromEditMode();
        }

        private static void StartPlayClientFromEditMode()
        {
            if (!OpenClientEntrySceneWithPrompt())
            {
                Debug.LogWarning("ProjectX automation cancelled before entering Play Mode.");
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.EnterPlaymode();
                }
            };
        }

        private static bool OpenBootstrapSceneWithPrompt()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            EnsureScenesInBuildSettings(ClientRuntimeScenePaths);

            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);

            EditorSceneManager.SetActiveScene(scene);

            return scene.IsValid() && scene.isLoaded;
        }

        private static bool OpenClientEntrySceneWithPrompt()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            EnsureScenesInBuildSettings(ClientRuntimeScenePaths);
            MoveSceneToFirstInBuildSettings(LoadingScenePath);

            var scene = EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);

            EditorSceneManager.SetActiveScene(scene);

            return scene.IsValid() && scene.isLoaded;
        }

        private static void BuildDedicatedServer(string outputPath)
        {
            outputPath = Path.GetFullPath(outputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Invalid build output path."));

            var scenePaths = GetEnabledScenePathsFromBuildProfile(DedicatedServerBuildProfilePath, ServerRuntimeScenePaths);

            MoveSceneToFirst(scenePaths, BootstrapScenePath);

            var options = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"ProjectX server build failed with result: {report.summary.result}");
            }

            Debug.Log($"ProjectX server built at: {outputPath}");
        }

        private static string[] GetEnabledScenePathsFromBuildProfile(string buildProfilePath, string[] fallbackScenePaths)
        {
            var buildProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(buildProfilePath);
            if (buildProfile == null)
            {
                Debug.LogWarning($"ProjectX automation could not load build profile at '{buildProfilePath}'. Falling back to local scene list.");
                return fallbackScenePaths;
            }

            var serializedProfile = new SerializedObject(buildProfile);
            var scenesProperty = serializedProfile.FindProperty("m_Scenes");
            if (scenesProperty == null || !scenesProperty.isArray || scenesProperty.arraySize == 0)
            {
                Debug.LogWarning($"ProjectX automation could not read scenes from '{buildProfilePath}'. Falling back to local scene list.");
                return fallbackScenePaths;
            }

            var scenePaths = new string[scenesProperty.arraySize];
            var sceneCount = 0;

            for (var i = 0; i < scenesProperty.arraySize; i++)
            {
                var sceneProperty = scenesProperty.GetArrayElementAtIndex(i);
                var enabledProperty = sceneProperty.FindPropertyRelative("m_enabled");
                var pathProperty = sceneProperty.FindPropertyRelative("m_path");

                if (pathProperty == null || string.IsNullOrWhiteSpace(pathProperty.stringValue))
                {
                    continue;
                }

                if (enabledProperty != null && !enabledProperty.boolValue)
                {
                    continue;
                }

                scenePaths[sceneCount++] = pathProperty.stringValue;
            }

            if (sceneCount == 0)
            {
                Debug.LogWarning($"ProjectX automation found no enabled scenes in '{buildProfilePath}'. Falling back to local scene list.");
                return fallbackScenePaths;
            }

            Array.Resize(ref scenePaths, sceneCount);
            return scenePaths;
        }

        private static void MoveSceneToFirst(string[] scenePaths, string requiredScenePath)
        {
            var index = Array.FindIndex(scenePaths, scenePath => string.Equals(scenePath, requiredScenePath, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new InvalidOperationException($"The required startup scene '{requiredScenePath}' is missing from the dedicated-server build profile.");
            }

            if (index == 0)
            {
                return;
            }

            var firstScene = scenePaths[index];
            Array.Copy(scenePaths, 0, scenePaths, 1, index);
            scenePaths[0] = firstScene;
        }

        private static void EnsureScenesInBuildSettings(string[] scenePaths)
        {
            var existingScenes = EditorBuildSettings.scenes;
            var changed = false;

            foreach (var scenePath in scenePaths)
            {
                var index = Array.FindIndex(
                    existingScenes,
                    scene => string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase));

                if (index >= 0)
                {
                    if (!existingScenes[index].enabled)
                    {
                        existingScenes[index].enabled = true;
                        changed = true;
                    }

                    continue;
                }

                Array.Resize(ref existingScenes, existingScenes.Length + 1);
                existingScenes[existingScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
                changed = true;
            }

            if (changed)
            {
                EditorBuildSettings.scenes = existingScenes;
            }
        }

        private static void MoveSceneToFirstInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            var index = Array.FindIndex(
                scenes,
                scene => string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase));

            if (index <= 0)
            {
                return;
            }

            var firstScene = scenes[index];
            Array.Copy(scenes, 0, scenes, 1, index);
            scenes[0] = firstScene;
            EditorBuildSettings.scenes = scenes;
        }

        private static void BuildDedicatedServerWithStatus(string outputPath)
        {
            try
            {
                BuildDedicatedServer(outputPath);

                WriteBuildServerStatus($"Succeeded|{Path.GetFullPath(outputPath)}");
            }
            catch (Exception ex)
            {
                WriteBuildServerStatus($"Failed|{ex.Message}");

                Debug.LogException(ex);

                throw;
            }
        }

        private static void WriteBuildServerStatus(string status)
        {
            var statusPath = GetProjectRelativeFullPath(BuildServerStatusPath);

            Directory.CreateDirectory(Path.GetDirectoryName(statusPath) ?? throw new InvalidOperationException("Invalid automation status path."));

            File.WriteAllText(statusPath, status);
        }

        private static string GetDefaultServerBuildPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Server", "ProjectXServer.exe"));
        }

        private static void RunAutomationFromUnity(string arguments = "")
        {
            var clientPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var runScriptPath = Path.Combine(clientPath, "Automation", "run.bat");

            if (!File.Exists(runScriptPath))
            {
                EditorUtility.DisplayDialog("ProjectX Automation", $"Could not find run script at:\n{runScriptPath}", "OK");
                return;
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = runScriptPath,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(runScriptPath),
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(startInfo);
            Debug.Log(string.IsNullOrWhiteSpace(arguments)
                ? $"ProjectX build and run started from Unity menu: {runScriptPath}"
                : $"ProjectX run started from Unity menu: {runScriptPath} {arguments}");
        }

        private static string GetProjectRelativeFullPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string GetCommandLineValue(string name, string defaultValue)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return defaultValue;
        }
    }
}
