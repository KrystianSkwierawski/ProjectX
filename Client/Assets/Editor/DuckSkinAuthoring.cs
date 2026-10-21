using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectX.Editor
{
    internal static class DuckSkinAuthoring
    {
        private const string Folder = "Assets/Art/Characters/Duck";

        public static void Build(Transform visual)
        {
            var body = visual.Find("Body");
            var neck = body.Find("Neck");
            var head = neck.Find("Head");
            var leftWing = body.Find("LeftWing");
            var rightWing = body.Find("RightWing");
            var leftFoot = visual.Find("LeftFoot");
            var rightFoot = visual.Find("RightFoot");
            var bones = new[] { body, neck, head, leftWing, leftWing.Find("Tip"), rightWing,
                rightWing.Find("Tip"), leftFoot, leftFoot.Find("Toes"), rightFoot, rightFoot.Find("Toes") };
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var triangles = new List<int>();
            var palette = new List<Material>();

            // One connected surface spans belly, shoulders, neck, cheeks and crown.
            var torso = ContinuousTorso();
            var torsoMaterial = body.Find("BodyMesh").GetComponent<MeshRenderer>().sharedMaterial;
            Append(torso, Matrix4x4.identity, "Torso", 0, torsoMaterial,
                vertices, normals, uv, weights, triangles, palette);
            UnityEngine.Object.DestroyImmediate(torso);

            var parts = visual.GetComponentsInChildren<MeshFilter>();
            foreach (var part in parts)
            {
                if (part.name == "BodyMesh" || part.name == "HeadMesh")
                {
                    continue;
                }

                int bone = Array.IndexOf(bones, part.transform.parent);
                if (bone < 0)
                {
                    throw new InvalidOperationException("Unbound duck surface: " + part.name);
                }

                var matrix = visual.worldToLocalMatrix * part.transform.localToWorldMatrix;
                Append(part.sharedMesh, matrix, part.name, bone, part.GetComponent<MeshRenderer>().sharedMaterial,
                    vertices, normals, uv, weights, triangles, palette);
            }

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/DuckSkin.asset");
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, Folder + "/DuckSkin.asset");
            }

            mesh.Clear();
            mesh.name = "DuckSkin";
            mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.boneWeights = weights.ToArray();
            var bindposes = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bindposes[i] = bones[i].worldToLocalMatrix * visual.localToWorldMatrix;
            }

            mesh.bindposes = bindposes;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            EditorUtility.SetDirty(mesh);

            var skin = new GameObject("DuckMesh");
            skin.transform.SetParent(visual, false);
            var renderer = skin.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.sharedMaterial = AtlasMaterial(palette);
            renderer.bones = bones;
            renderer.rootBone = body;
            renderer.quality = SkinQuality.Bone4;
            renderer.localBounds = new Bounds(new Vector3(0, .9f, 0), new Vector3(2.4f, 2.4f, 2f));
            renderer.updateWhenOffscreen = false;

            foreach (var part in parts)
            {
                UnityEngine.Object.DestroyImmediate(part.sharedMesh);
                UnityEngine.Object.DestroyImmediate(part.gameObject);
            }

            Debug.Log($"[DuckSkin] Authored {vertices.Count} vertices, {triangles.Count / 3} triangles, {bones.Length} bones, one renderer/material.");
        }

        private static void Append(Mesh source, Matrix4x4 matrix, string name, int bone, Material material,
            List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<BoneWeight> weights,
            List<int> triangles, List<Material> palette)
        {
            int slot = palette.IndexOf(material);
            if (slot < 0)
            {
                slot = palette.Count;
                palette.Add(material);
            }

            int offset = vertices.Count;
            var points = source.vertices;
            var sourceNormals = source.normals;
            var sourceUv = source.uv;
            var normalMatrix = matrix.inverse.transpose;
            for (int i = 0; i < points.Length; i++)
            {
                var point = matrix.MultiplyPoint3x4(points[i]);
                if (name.EndsWith("Eye", StringComparison.Ordinal) || name.EndsWith("Glint", StringComparison.Ordinal))
                {
                    // Conform the complete eye surface to the actual torso/head loft,
                    // rather than embedding an ellipsoid in an approximate head sphere.
                    var profile = ProfileAtHeight(point.y);
                    float radial = Mathf.Clamp(point.x / profile.y, -.999f, .999f);
                    float surface = profile.w + profile.z * Mathf.Sqrt(1 - radial * radial);
                    float depth = name.EndsWith("Eye", StringComparison.Ordinal) ? .002f + (points[i].z + .5f) * .035f
                        : .036f + (points[i].z + .5f) * .012f;
                    point.z = surface + depth;
                }

                vertices.Add(point);
                normals.Add(normalMatrix.MultiplyVector(sourceNormals[i]).normalized);
                uv.Add(new Vector2((slot % 4 + .03125f + sourceUv[i].x * .9375f) / 4,
                    (slot / 4 + .03125f + sourceUv[i].y * .9375f) / 2));

                var weight = new BoneWeight { boneIndex0 = bone, weight0 = 1 };
                if (name == "Torso" || name.EndsWith("Eye", StringComparison.Ordinal) || name.EndsWith("Glint", StringComparison.Ordinal))
                {
                    weight = point.y < 1.12f
                        ? Pair(0, 1, Smooth(.86f, 1.12f, point.y))
                        : Pair(1, 2, Smooth(1.12f, 1.36f, point.y));
                }
                else if (bone == 3 || bone == 5)
                {
                    // Shoulder blends into the body, elbow and feather tips bend separately.
                    float shoulder = 1 - Smooth(.71f, .94f, point.y);
                    float tip = 1 - Smooth(.40f, .66f, point.y);
                    weight = new BoneWeight { boneIndex0 = 0, weight0 = 1 - shoulder,
                        boneIndex1 = bone, weight1 = shoulder * (1 - tip),
                        boneIndex2 = bone + 1, weight2 = shoulder * tip };
                }
                else if (bone == 7 || bone == 9)
                {
                    weight = name == "Ankle"
                        ? Pair(bone, 0, Smooth(.19f, .35f, point.y))
                        : Pair(bone, bone + 1, Smooth(.14f, .31f, point.z));
                }

                weights.Add(weight);
            }

            foreach (int triangle in source.triangles)
            {
                triangles.Add(offset + triangle);
            }
        }

        private static BoneWeight Pair(int first, int second, float blend)
        {
            return new BoneWeight { boneIndex0 = first, boneIndex1 = second, weight0 = 1 - blend, weight1 = blend };
        }

        private static float Smooth(float from, float to, float value)
        {
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, value));
        }

        private static readonly Vector4[] TorsoProfile = {
            new Vector4(.20f, 0, 0, -.02f), new Vector4(.29f, .204f, .207f, -.025f),
            new Vector4(.48f, .34f, .324f, -.025f), new Vector4(.72f, .3825f, .351f, -.025f),
            new Vector4(.93f, .3145f, .297f, 0), new Vector4(1.08f, .24f, .24f, .04f),
            new Vector4(1.19f, .298f, .279f, .08f), new Vector4(1.36f, .372f, .344f, .12f),
            new Vector4(1.54f, .316f, .298f, .11f), new Vector4(1.67f, .186f, .177f, .09f),
            new Vector4(1.72f, 0, 0, .08f)
        };

        private static Vector4 ProfileAtHeight(float height)
        {
            int section = 0;
            while (section < TorsoProfile.Length - 2 && TorsoProfile[section + 1].x < height)
            {
                section++;
            }

            float low = 0;
            float high = 1;
            for (int i = 0; i < 20; i++)
            {
                float t = (low + high) * .5f;
                if (ProfilePoint(section, t).x < height)
                {
                    low = t;
                }
                else
                {
                    high = t;
                }
            }

            return ProfilePoint(section, (low + high) * .5f);
        }

        private static Vector4 ProfilePoint(int section, float t)
        {
            var a = TorsoProfile[Mathf.Max(0, section - 1)];
            var b = TorsoProfile[section];
            var c = TorsoProfile[section + 1];
            var d = TorsoProfile[Mathf.Min(TorsoProfile.Length - 1, section + 2)];
            return .5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t
                + (-a + 3 * b - 3 * c + d) * t * t * t);
        }

        private static Mesh ContinuousTorso()
        {
            var profile = TorsoProfile;
            const int segments = 64;
            const int steps = 8;
            int rings = (profile.Length - 1) * steps;
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var uv = new Vector2[vertices.Length];
            var indices = new List<int>();
            for (int ring = 0; ring <= rings; ring++)
            {
                int section = Mathf.Min(ring / steps, profile.Length - 2);
                float t = (float)(ring - section * steps) / steps;
                var a = profile[Mathf.Max(0, section - 1)];
                var b = profile[section];
                var c = profile[section + 1];
                var d = profile[Mathf.Min(profile.Length - 1, section + 2)];
                var p = .5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t
                    + (-a + 3 * b - 3 * c + d) * t * t * t);
                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = (float)segment / segments;
                    float angle = u * 2 * Mathf.PI;
                    int vertex = ring * (segments + 1) + segment;
                    vertices[vertex] = new Vector3(Mathf.Cos(angle) * p.y, p.x, Mathf.Sin(angle) * p.z + p.w);
                    uv[vertex] = new Vector2(u, 1 - Mathf.InverseLerp(.20f, 1.72f, p.x));
                    if (ring == rings || segment == segments)
                    {
                        continue;
                    }

                    int next = vertex + segments + 1;
                    if (ring > 0)
                    {
                        indices.Add(vertex);
                        indices.Add(next);
                        indices.Add(vertex + 1);
                    }

                    if (ring < rings - 1)
                    {
                        indices.Add(vertex + 1);
                        indices.Add(next);
                        indices.Add(next + 1);
                    }
                }
            }

            var mesh = new Mesh { vertices = vertices, uv = uv, triangles = indices.ToArray() };
            mesh.RecalculateNormals();
            var normals = mesh.normals;
            for (int ring = 0; ring <= rings; ring++)
            {
                int first = ring * (segments + 1);
                var normal = (normals[first] + normals[first + segments]).normalized;
                normals[first] = normal;
                normals[first + segments] = normal;
            }

            for (int segment = 0; segment <= segments; segment++)
            {
                normals[segment] = Vector3.down;
                normals[rings * (segments + 1) + segment] = Vector3.up;
            }

            mesh.normals = normals;
            return mesh;
        }

        private static Material AtlasMaterial(List<Material> palette)
        {
            const int tile = 256;
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/DuckAtlas.asset");
            if (atlas == null)
            {
                atlas = new Texture2D(1024, 512, TextureFormat.RGBA32, true);
                AssetDatabase.CreateAsset(atlas, Folder + "/DuckAtlas.asset");
            }

            for (int slot = 0; slot < palette.Count; slot++)
            {
                var color = palette[slot].GetColor("_BaseColor");
                var texture = palette[slot].GetTexture("_BaseMap") as Texture2D;
                for (int y = 0; y < tile; y++)
                {
                    for (int x = 0; x < tile; x++)
                    {
                        var sample = texture == null ? Color.white : texture.GetPixelBilinear(
                            Mathf.Clamp01((x - 8f) / 240), Mathf.Clamp01((y - 8f) / 240));
                        atlas.SetPixel(slot % 4 * tile + x, slot / 4 * tile + y, color * sample);
                    }
                }
            }

            atlas.Apply();
            atlas.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(atlas);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/DuckSkin.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Folder + "/DuckSkin.mat");
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", atlas);
            material.SetFloat("_Smoothness", .24f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
