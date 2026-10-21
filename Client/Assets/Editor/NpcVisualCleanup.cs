using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Assets.Scripts.Areas.Quest.Mono;
using Assets.Scripts.Areas.Shared.Mono;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectX.Editor
{
    [InitializeOnLoad]
    internal static class NpcVisualCleanup
    {
        private const string Request = "Temp/npc-visual-cleanup.request";
        private const string Folder = "Assets/Art/Characters/Npc";
        private static readonly string[] Paths = {
            "Assets/Prefabs/Quest/QuestNpc.prefab", "Assets/Prefabs/Merchant/MerchantNpc.prefab"
        };

        static NpcVisualCleanup()
        {
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            File.Delete(Request);
            try
            {
                Build();
                File.WriteAllText("Temp/npc-visual-cleanup.status", "complete");
            }
            catch (Exception exception)
            {
                File.WriteAllText("Temp/npc-visual-cleanup.status", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem("ProjectX/Character/Rebuild bird NPC visuals")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            foreach (string path in Paths)
            {
                Replace(path);
            }

            foreach (BirdNpcAuthoring.QuestBirdEnum species in Enum.GetValues(typeof(BirdNpcAuthoring.QuestBirdEnum)))
            {
                var visual = BirdNpcAuthoring.BuildQuest(species);
                PrefabUtility.SaveAsPrefabAsset(visual, Folder + "/" + species + "Visual.prefab");
                Object.DestroyImmediate(visual);
            }

            AssetDatabase.SaveAssets();
            AssignQuestBirds();
            ValidateScene();
            Preview();
            Debug.Log("NPC_BIRDS_VALIDATED: standalone prefabs, no AssetHunts dependencies, preserved scene IDs, quest markers, colliders and merchant offers.");
        }

        private static void Replace(string path)
        {
            var before = Identities(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(root))
                {
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                var remove = new List<GameObject>();
                foreach (Transform child in root.transform)
                {
                    if (child.name != "ExclamationMark" && child.name != "QuestionMark")
                    {
                        remove.Add(child.gameObject);
                    }
                }

                foreach (var child in remove)
                {
                    Object.DestroyImmediate(child);
                }

                foreach (var component in root.GetComponents<Component>())
                {
                    if (component is Renderer || component is MeshFilter || component is Animator)
                    {
                        Object.DestroyImmediate(component);
                    }
                }

                var bird = BirdNpcAuthoring.Build(root.GetComponent<MerchantNpc>() != null);
                bird.transform.SetParent(root.transform, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Unpacking a model variant can allocate new IDs. Keep the retained
            // objects' identities so scene overrides still address the same NPC.
            var after = Identities(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var remap = new Dictionary<long, long>();
            foreach (var entry in before)
            {
                if (after.TryGetValue(entry.Key, out long current) && current != entry.Value)
                {
                    remap.Add(current, entry.Value);
                }
            }

            if (remap.Count > 0)
            {
                string yaml = File.ReadAllText(path);
                yaml = Regex.Replace(yaml, @"(?<=fileID: )-?\d+|(?<=&)-?\d+", match =>
                    remap.TryGetValue(long.Parse(match.Value), out long id) ? id.ToString() : match.Value);
                File.WriteAllText(path, yaml);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Regular, "NPC is still a model variant: " + path);
            foreach (string dependency in AssetDatabase.GetDependencies(path, true))
            {
                Require(!dependency.Contains("AssetHunts"), "External NPC dependency: " + dependency);
            }

            var final = Identities(prefab);
            foreach (var entry in before)
            {
                if (final.TryGetValue(entry.Key, out long id))
                {
                    Require(id == entry.Value, "Retained NPC object changed identity: " + entry.Key);
                }
            }
        }

        private static Dictionary<string, long> Identities(GameObject root)
        {
            var result = new Dictionary<string, long>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(transform, root.transform);
                if (path.Length > 0 && !path.StartsWith("ExclamationMark", StringComparison.Ordinal)
                    && !path.StartsWith("QuestionMark", StringComparison.Ordinal)
                    && path != "HenVisual" && path != "MagpieVisual")
                {
                    continue;
                }

                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(transform.gameObject, out string _, out long objectId);
                result.Add(path + "|GameObject", objectId);
                foreach (var component in transform.GetComponents<Component>())
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out string _, out long id);
                    result.Add(path + "|" + component.GetType().FullName, id);
                }
            }

            return result;
        }

        private static void AssignQuestBirds()
        {
            // Serialized scene overrides leave every quest, transform and marker intact.
            // File order provides a stable cycle across the existing NPC instances.
            const string scenePath = "Assets/Scenes/EnvironmentScene.unity";
            string guid = AssetDatabase.AssetPathToGUID(Paths[0]);
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(Paths[0]).transform.Find("HenVisual");
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(visual.GetComponent<MeshFilter>(), out string _, out long meshId);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(visual.gameObject, out string _, out long objectId);
            string text = File.ReadAllText(scenePath);
            int count = 0;
            text = Regex.Replace(text, @"--- !u!1001 &[^\r\n]+[\s\S]*?(?=--- !u!|\z)", match =>
            {
                string block = match.Value;
                if (!block.Contains("m_SourcePrefab: {fileID: 100100000, guid: " + guid))
                {
                    return block;
                }

                var species = (BirdNpcAuthoring.QuestBirdEnum)(count++ % 4);
                string meshGuid = AssetDatabase.AssetPathToGUID(Folder + "/" + species + ".asset");
                Require(!string.IsNullOrEmpty(meshGuid), "Missing quest bird mesh: " + species);
                block = Regex.Replace(block, @"    - target: \{fileID: -?\d+, guid: " + guid
                    + @", type: 3\}\r?\n      propertyPath: m_Mesh\r?\n      value:[^\r\n]*\r?\n      objectReference:[^\r\n]*\r?\n", "");
                block = Regex.Replace(block, @"    - target: \{fileID: -?\d+, guid: " + guid
                    + @", type: 3\}\r?\n      propertyPath: m_Name\r?\n      value: (?:Hen|Stork|Pigeon|Swan)Visual\r?\n      objectReference:[^\r\n]*\r?\n", "");

                string overrides = $"    - target: {{fileID: {meshId}, guid: {guid}, type: 3}}\n      propertyPath: m_Mesh\n      value: \n      objectReference: {{fileID: 4300000, guid: {meshGuid}, type: 2}}\n"
                    + $"    - target: {{fileID: {objectId}, guid: {guid}, type: 3}}\n      propertyPath: m_Name\n      value: {species}Visual\n      objectReference: {{fileID: 0}}\n";
                return block.Replace("    m_RemovedComponents:", overrides + "    m_RemovedComponents:");
            });

            Require(count > 0, "No quest NPC instances found for bird assignment.");
            File.WriteAllText(scenePath, text);
            AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"QUEST_BIRDS_ASSIGNED: {count} instances, repeating Hen/Stork/Pigeon/Swan; quest data unchanged.");
        }

        private static void ValidateScene()
        {
            const string path = "Assets/Scenes/EnvironmentScene.unity";
            string sceneText = File.ReadAllText(path);
            foreach (string prefabPath in Paths)
            {
                string guid = AssetDatabase.AssetPathToGUID(prefabPath);
                string yaml = File.ReadAllText(prefabPath);
                foreach (Match match in Regex.Matches(sceneText, @"target: \{fileID: (-?\d+), guid: " + guid))
                {
                    Require(Regex.IsMatch(yaml, @"&" + match.Groups[1].Value + @"(?:\s|$)"), "Scene override lost its target: " + match.Value);
                }
            }

            var scene = EditorSceneManager.OpenPreviewScene(path);
            int quests = 0;
            int merchants = 0;
            var speciesCounts = new int[4];
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var npc in root.GetComponentsInChildren<QuestNpc>(true))
                    {
                        quests++;
                        Require(npc.CompareTag("QuestNpc") && npc.GetComponent<Collider>() != null, "Quest interaction collider/tag missing.");
                        Require(npc.transform.Find("ExclamationMark").GetComponent<MeshRenderer>() != null
                            && npc.transform.Find("QuestionMark").GetComponent<MeshRenderer>() != null, "Quest markers missing.");
                        int found = 0;
                        foreach (BirdNpcAuthoring.QuestBirdEnum species in Enum.GetValues(typeof(BirdNpcAuthoring.QuestBirdEnum)))
                        {
                            var visual = npc.transform.Find(species + "Visual");
                            if (visual == null)
                            {
                                continue;
                            }

                            Require(visual.GetComponent<MeshFilter>().sharedMesh == AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/" + species + ".asset"), "Wrong quest bird mesh.");
                            speciesCounts[(int)species]++;
                            found++;
                        }

                        Require(found == 1, "Quest NPC must have exactly one bird visual.");
                    }

                    foreach (var npc in root.GetComponentsInChildren<MerchantNpc>(true))
                    {
                        merchants++;
                        Require(npc.CompareTag("MerchantNpc") && npc.GetComponent<Collider>() != null, "Merchant interaction collider/tag missing.");
                        Require(npc.Items.Length == 47 && npc.transform.Find("MagpieVisual") != null, "Merchant offers or magpie missing.");
                    }
                }

                Require(quests > 0 && merchants > 0, "NPC scene instances missing.");
                foreach (int count in speciesCounts)
                {
                    Require(count >= quests / 4 && count <= (quests + 3) / 4, "Unbalanced quest bird distribution.");
                }

                Debug.Log("QUEST_BIRDS_VALIDATED: Hen/Stork/Pigeon/Swan=" + string.Join("/", speciesCounts));
                Debug.Log($"NPC_SCENE_VALIDATED: quest NPCs={quests}, merchants={merchants}, offers=47.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Preview()
        {
            var preview = new PreviewRenderUtility();
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    var species = (BirdNpcAuthoring.QuestBirdEnum)i;
                    var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + species + "Visual.prefab"));
                    model.transform.position = new Vector3(i * 1.4f - 2.1f, 0, 0);
                    model.transform.rotation = Quaternion.Euler(0, -25, 0);
                    preview.AddSingleGO(model);
                }

                preview.camera.transform.position = new Vector3(0, 2.1f, 6.4f);
                preview.camera.transform.LookAt(new Vector3(0, 1, 0));
                preview.camera.nearClipPlane = .01f;
                preview.camera.fieldOfView = 35;
                preview.camera.farClipPlane = 100;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(.16f, .19f, .23f);
                preview.lights[0].intensity = 1.3f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35, -35, 0);
                preview.lights[1].intensity = .7f;
                preview.ambientColor = Color.gray;

                preview.BeginStaticPreview(new Rect(0, 0, 1400, 700));
                preview.Render(true);
                var image = preview.EndStaticPreview();
                File.WriteAllBytes("Temp/npc-birds.png", image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally
            {
                preview.Cleanup();
            }
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
