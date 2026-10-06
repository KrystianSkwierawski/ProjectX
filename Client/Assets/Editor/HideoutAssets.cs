using System;
using System.Linq;
using Assets.Scripts.Areas.Hideout;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectX.Editor
{
    public static class HideoutAssets
    {
        private const string FarmPath = "Assets/Prefabs/Locations/Farm.prefab";
        private const string RoomPath = "Assets/Prefabs/Locations/Resources/HideoutEnvironmentPrefab.prefab";
        private const string ScenePath = "Assets/Scenes/HideoutScene.unity";

        [MenuItem("ProjectX/Create Hideout Blockout")]
        public static void Create()
        {
            CreateFarm();
            CreateRoom();
            WirePlayer();
            WireEntrance();

            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(x => x.path != ScenePath).Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Validate();
        }

        private static void CreateFarm()
        {
            var root = new GameObject("Farm");
            var building = root.AddComponent<HideoutBuilding>();
            var farm = new GameObject("Content");
            farm.transform.SetParent(root.transform, false);
            var soil = Cube("Floor", farm.transform, new Vector3(0, 0.025f, 0), new Vector3(3, 0.05f, 3));
            soil.GetComponent<Renderer>().sharedMaterial = GetFloorMaterial();

            var station = Cube("BuildStation", root.transform, new Vector3(0, 0.025f, 0), new Vector3(3, 0.05f, 3));
            station.GetComponent<Renderer>().sharedMaterial = soil.GetComponent<Renderer>().sharedMaterial;
            ConfigureFarmSlots(building);

            var labelObject = new GameObject("Countdown");
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.AddComponent<TextMeshPro>();
            labelObject.transform.localPosition = new Vector3(0, 3, 0);
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Futura PT Heavy SDF.asset");
            var countdownMaterialPath = "Assets/Materials/HideoutCountdown.mat";
            var countdownMaterial = AssetDatabase.LoadAssetAtPath<Material>(countdownMaterialPath);

            if (countdownMaterial == null)
            {
                countdownMaterial = new Material(label.font.material);
                countdownMaterial.SetColor("_OutlineColor", Color.black);
                countdownMaterial.SetFloat("_OutlineWidth", 0.2f);
                countdownMaterial.EnableKeyword("OUTLINE_ON");
                AssetDatabase.CreateAsset(countdownMaterial, countdownMaterialPath);
            }

            label.fontSharedMaterial = countdownMaterial;
            label.fontSize = 5;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1, 1, 1, 0.85f);
            label.rectTransform.sizeDelta = new Vector2(5, 1);
            label.text = "00:30";
            labelObject.SetActive(false);

            var ghostPath = "Assets/Resources/HideoutGhost.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(ghostPath) == null)
            {
                AssetDatabase.CopyAsset("Assets/Materials/DungeonPortal.mat", ghostPath);
            }

            var ghost = AssetDatabase.LoadAssetAtPath<Material>(ghostPath);
            ghost.SetColor("_BaseColor", new Color(0.75f, 0.9f, 1, 0.35f));
            EditorUtility.SetDirty(ghost);
            var serialized = new SerializedObject(building);
            serialized.FindProperty("_buildingId").enumValueIndex = (int)HideoutBuildingEnum.Farm;
            var crops = serialized.FindProperty("_crops");
            var names = new[] { "Chamomile", "Strawberry", "Mint", "Lavender", "Calendula", "Raspberry" };
            crops.arraySize = names.Length;

            for (var i = 0; i < names.Length; i++)
            {
                var crop = crops.GetArrayElementAtIndex(i);
                crop.FindPropertyRelative("Seed").intValue = (int)Enum.Parse<InventoryItemEnum>(names[i] + "Seed");
                crop.FindPropertyRelative("Product").intValue = (int)Enum.Parse<InventoryItemEnum>(names[i]);
                crop.FindPropertyRelative("Spawner").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Spawning/" + names[i] + "Spawner.prefab").GetComponent<Spawner>();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, FarmPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        public static void UpdateFarmSlots()
        {
            var root = PrefabUtility.LoadPrefabContents(FarmPath);

            ConfigureFarmSlots(root.GetComponent<HideoutBuilding>());
            PrefabUtility.SaveAsPrefabAsset(root, FarmPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureFarmSlots(HideoutBuilding building)
        {
            var content = building.transform.Find("Content");

            foreach (var old in content.GetComponentsInChildren<Spawner>(true))
            {
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            var serialized = new SerializedObject(building);
            var slots = serialized.FindProperty("_slots");
            slots.arraySize = 6;

            for (var i = 0; i < slots.arraySize; i++)
            {
                var obj = new GameObject($"SlotSpawner{i + 1}");
                obj.SetActive(false);
                obj.transform.SetParent(content, false);
                obj.transform.localPosition = new Vector3((i % 2 - 0.5f) * 1.2f, 0.065f, (i / 2 - 1) * 0.85f);
                obj.AddComponent<BoxCollider>().enabled = false;

                var spawner = obj.AddComponent<Spawner>();
                var settings = new SerializedObject(spawner);
                settings.FindProperty("_count").intValue = 1;
                settings.FindProperty("_maintainPopulation").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
                slots.GetArrayElementAtIndex(i).objectReferenceValue = spawner;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material GetFloorMaterial()
        {
            const string path = "Assets/Materials/FarmFloor.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                AssetDatabase.CopyAsset("Assets/Materials/DungeonGrey.mat", path);
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            material.SetColor("_BaseColor", new Color(0.2f, 0.55f, 0.15f, 1));
            EditorUtility.SetDirty(material);

            return material;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;

            return cube;
        }

        private static void CreateRoom()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Locations/Resources/TemplateEnvironmentPrefab.prefab");
            root.name = "HideoutEnvironmentPrefab";
            var farm = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FarmPath));
            farm.transform.SetParent(root.transform, false);
            farm.transform.localPosition = Vector3.zero;
            var surface = root.GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
            var dataPath = "Assets/Resources/HideoutNavMesh.asset";
            var saved = AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath);

            if (saved == null)
            {
                AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
            }
            else
            {
                EditorUtility.CopySerialized(surface.navMeshData, saved);
                surface.navMeshData = saved;
            }

            PrefabUtility.SaveAsPrefabAsset(root, RoomPath);
            PrefabUtility.UnloadPrefabContents(root);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath), scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void WirePlayer()
        {
            var path = "Assets/Prefabs/Character/NestedParentArmature_Unpack Variant.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var player = root.GetComponentInChildren<DungeonTravel>(true).gameObject;

            if (player.GetComponent<CharacterHideout>() == null)
            {
                player.AddComponent<CharacterHideout>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void WireEntrance()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/EnvironmentScene.unity", OpenSceneMode.Single);

            if (!scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<DungeonPortal>(true))
                .Any(x => x.Destination == LocationEnum.HideoutScene))
            {
                var portal = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/DungeonPortal.prefab"), scene);
                portal.name = "HideoutPortal";
                portal.transform.position = new Vector3(8, 1.4f, 4);
                var serialized = new SerializedObject(portal.GetComponent<DungeonPortal>());
                serialized.FindProperty("_destination").enumValueIndex = (int)LocationEnum.HideoutScene;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene);
        }

        public static void Validate()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath);
            root.GetComponent<LocationEnvironment>().ValidateNavigation();
            var building = root.GetComponentInChildren<HideoutBuilding>(true);
            var spawners = building.GetComponentsInChildren<Spawner>(true);
            var crops = new SerializedObject(building).FindProperty("_crops");

            if (spawners.Length != 6)
            {
                throw new InvalidOperationException("Farm must contain six persistent slot spawners.");
            }

            if (crops.arraySize != 6)
            {
                throw new InvalidOperationException("Farm requires six seed/product/spawner definitions.");
            }

            for (var i = 0; i < crops.arraySize; i++)
            {
                if (crops.GetArrayElementAtIndex(i).FindPropertyRelative("Spawner").objectReferenceValue == null)
                {
                    throw new InvalidOperationException("Farm has a missing crop spawner reference.");
                }
            }

            if (building.BuildingId != HideoutBuildingEnum.Farm)
            {
                throw new InvalidOperationException("Farm prefab must select Farm in its building enum.");
            }

            if (spawners.Any(x => x.gameObject.activeSelf))
            {
                throw new InvalidOperationException("Hideout must contain six initially inactive farm spawners.");
            }

            if (!root.GetComponentsInChildren<DungeonPortal>().Any(x => x.Destination == LocationEnum.EnvironmentScene))
            {
                throw new InvalidOperationException("Hideout return portal missing.");
            }

            Debug.Log("Hideout prefab, spawner, return portal and baked navigation validation passed.");

            var preview = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FarmPath));

            try
            {
                var view = preview.GetComponent<HideoutBuilding>();
                typeof(HideoutBuilding).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(view, null);

                var now = DateTimeOffset.UtcNow;
                var definition = new HideoutBuildingDto { Id = view.BuildingId };
                view.Apply(definition, now, false);

                if (!view.CanBuild || preview.transform.Find("Content").gameObject.activeSelf)
                {
                    throw new InvalidOperationException("Unbuilt hideout projection failed.");
                }

                var station = preview.transform.Find("BuildStation").gameObject;
                var ghost = Resources.Load<Material>("HideoutGhost");

                if (!station.activeInHierarchy || !station.GetComponent<Collider>().enabled
                    || station.GetComponent<Renderer>().sharedMaterial != ghost)
                {
                    throw new InvalidOperationException("Unbuilt hideout must have a clickable ghost preview.");
                }

                definition.BuildEndsAt = now.AddSeconds(20);
                view.Apply(definition, now, false);
                var countdown = preview.GetComponentInChildren<TMP_Text>(true);

                if (view.CanBuild || !countdown.gameObject.activeSelf || countdown.text != "00:00:20"
                    || countdown.transform.localPosition.y < 2 || countdown.fontSharedMaterial.GetFloat("_OutlineWidth") <= 0)
                {
                    throw new InvalidOperationException("Construction/countdown projection failed.");
                }

                view.Apply(definition, now.AddSeconds(21), true);

                if (countdown.gameObject.activeSelf || preview.GetComponentInChildren<Spawner>(true).gameObject.activeInHierarchy)
                {
                    throw new InvalidOperationException("Empty completed farm must not spawn a free crop.");
                }

                Debug.Log("Hideout unbuilt, restored countdown, outline and completed projection checks passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }
    }
}
