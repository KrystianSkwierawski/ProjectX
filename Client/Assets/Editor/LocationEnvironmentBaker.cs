using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectX.Editor
{
    public static class LocationEnvironmentBaker
    {
        [MenuItem("ProjectX/Bake Dungeon Navigation")]
        public static void BakeDungeon()
        {
            Bake("Dungeon");
        }

        [MenuItem("ProjectX/Bake Template Navigation")]
        public static void BakeTemplate()
        {
            Bake("Template");
        }

        private static void Bake(string location)
        {
            var prefabPath = $"Assets/Prefabs/Locations/Resources/{location}EnvironmentPrefab.prefab";
            var dataPath = $"Assets/Resources/{location}NavMesh.asset";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                var surface = root.GetComponent<NavMeshSurface>();
                surface.BuildNavMesh();
                var generated = surface.navMeshData;

                if (generated == null)
                {
                    throw new InvalidOperationException($"{location} NavMesh bake failed.");
                }

                var saved = AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath);

                if (saved == null)
                {
                    AssetDatabase.CreateAsset(generated, dataPath);
                    saved = generated;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, saved);
                    UnityEngine.Object.DestroyImmediate(generated);
                    EditorUtility.SetDirty(saved);
                }

                surface.navMeshData = saved;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"Location navigation baked. Prefab: {prefabPath}, Data: {dataPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
