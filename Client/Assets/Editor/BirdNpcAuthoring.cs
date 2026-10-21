using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectX.Editor
{
    internal static class BirdNpcAuthoring
    {
        private const string Folder = "Assets/Art/Characters/Npc";
        internal enum QuestBirdEnum { Hen, Stork, Pigeon, Swan }
        private static readonly Color[] Palette = {
            new Color(.97f, .91f, .75f), new Color(.76f, .53f, .29f),
            new Color(.055f, .07f, .095f), new Color(.95f, .97f, .98f),
            new Color(.68f, .055f, .065f), new Color(.95f, .49f, .08f),
            new Color(.045f, .20f, .28f), new Color(.055f, .31f, .28f),
            new Color(.48f, .52f, .58f), new Color(.24f, .28f, .34f)
        };

        public static GameObject Build(bool magpie)
        {
            Directory.CreateDirectory(Folder);
            string name = magpie ? "Magpie" : "Hen";
            var geometry = new Geometry();
            geometry.Body(magpie);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * .19f;
                int footColor = magpie ? 2 : 5;
                geometry.Ellipsoid(new Vector3(x, .28f, .03f), new Vector3(.046f, .20f, .05f), Vector3.zero, footColor);
                for (int toe = -1; toe <= 1; toe++)
                {
                    geometry.Taper(new Vector3(x, .105f, .025f), new Vector3(x + toe * .085f, .07f, .26f - Mathf.Abs(toe) * .045f), .037f, .024f, footColor);
                }

                geometry.Taper(new Vector3(x, .105f, .025f), new Vector3(x - side * .045f, .075f, -.10f), .03f, .023f, footColor);
                geometry.Ellipsoid(new Vector3(side * .34f, .87f, -.065f), new Vector3(.13f, .36f, .25f), new Vector3(12, 0, side * -12), magpie ? 2 : 1);
                for (int feather = 0; feather < 5; feather++)
                {
                    geometry.Ellipsoid(new Vector3(side * (.39f + feather * .004f), .76f + feather * .045f, -.18f + feather * .07f),
                        new Vector3(.055f, .23f - feather * .012f, .065f), new Vector3(18 + feather * 6, 0, side * -14), magpie ? (feather < 2 ? 6 : 3) : 0);
                }

                var eyeRotation = Quaternion.Euler(0, side * 32, 0);
                var eye = new Vector3(side * .205f, 1.55f, .365f);
                if (magpie)
                {
                    geometry.Ellipsoid(eye, new Vector3(.078f, .09f, .027f), new Vector3(0, side * 32, 0), 3);
                }

                geometry.Ellipsoid(eye + eyeRotation * new Vector3(0, 0, magpie ? .022f : 0),
                    new Vector3(magpie ? .054f : .066f, magpie ? .063f : .078f, .026f), new Vector3(0, side * 32, 0), 2);
                geometry.Ellipsoid(eye + eyeRotation * new Vector3(-.014f, .022f, magpie ? .047f : .027f),
                    new Vector3(.016f, .02f, .008f), new Vector3(0, side * 32, 0), 3);
            }

            geometry.Taper(new Vector3(0, 1.44f, .34f), new Vector3(0, 1.405f, magpie ? .68f : .59f), magpie ? .09f : .115f, .066f, magpie ? 2 : 5);
            if (magpie)
            {
                for (int feather = -1; feather <= 1; feather++)
                {
                    geometry.Ellipsoid(new Vector3(feather * .075f, .69f, -.69f), new Vector3(.085f, .045f, .60f - Mathf.Abs(feather) * .08f),
                        new Vector3(-13, feather * -6, 0), feather == 0 ? 7 : 6);
                }
            }
            else
            {
                geometry.Ellipsoid(new Vector3(0, 1.805f, .09f), new Vector3(.065f, .07f, .23f), Vector3.zero, 4);
                for (int lobe = 0; lobe < 4; lobe++)
                {
                    geometry.Ellipsoid(new Vector3(0, 1.865f + .04f * Mathf.Sin(lobe * Mathf.PI / 3), -.09f + lobe * .11f),
                        new Vector3(.06f, .10f, .075f), new Vector3(-10, 0, 0), 4);
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    geometry.Ellipsoid(new Vector3(side * .047f, 1.32f, .405f), new Vector3(.055f, .105f, .035f), new Vector3(-12, 0, side * -8), 4);
                }

                for (int feather = -2; feather <= 2; feather++)
                {
                    geometry.Ellipsoid(new Vector3(feather * .075f, .94f, -.40f), new Vector3(.075f, .31f, .095f),
                        new Vector3(-40, 0, feather * -12), feather % 2 == 0 ? 0 : 1);
                }
            }

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/" + name + ".asset");
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, Folder + "/" + name + ".asset");
            }

            geometry.Write(mesh);
            EditorUtility.SetDirty(mesh);
            var root = new GameObject(name + "Visual");
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = Material();
            return root;
        }

        public static GameObject BuildQuest(QuestBirdEnum species)
        {
            if (species == QuestBirdEnum.Hen)
            {
                return Build(false);
            }

            Directory.CreateDirectory(Folder);
            bool stork = species == QuestBirdEnum.Stork;
            bool swan = species == QuestBirdEnum.Swan;
            var geometry = new Geometry();
            float headY = stork ? 1.81f : swan ? 1.78f : 1.51f;
            float headZ = swan ? .23f : .11f;
            float legTop = stork ? .77f : .38f;
            int bodyColor = stork || swan ? 3 : 8;
            var profile = stork ? new[] {
                new Vector4(.67f,.025f,.025f,-.04f), new Vector4(.85f,.29f,.26f,-.04f),
                new Vector4(1.06f,.34f,.29f,-.05f), new Vector4(1.23f,.22f,.20f,0),
                new Vector4(1.43f,.10f,.11f,.015f), new Vector4(1.64f,.12f,.13f,.06f),
                new Vector4(1.81f,.23f,.22f,.11f), new Vector4(1.97f,.15f,.14f,.10f),
                new Vector4(2.02f,.002f,.002f,.10f)
            } : swan ? new[] {
                new Vector4(.32f,.02f,.02f,-.08f), new Vector4(.51f,.37f,.42f,-.12f),
                new Vector4(.76f,.43f,.43f,-.13f), new Vector4(.97f,.31f,.29f,-.07f),
                new Vector4(1.17f,.14f,.15f,-.02f), new Vector4(1.38f,.095f,.10f,-.07f),
                new Vector4(1.59f,.10f,.12f,.015f), new Vector4(1.78f,.22f,.22f,.23f),
                new Vector4(1.94f,.14f,.14f,.25f), new Vector4(1.99f,.002f,.002f,.25f)
            } : new[] {
                new Vector4(.32f,.02f,.02f,-.06f), new Vector4(.52f,.34f,.33f,-.06f),
                new Vector4(.77f,.41f,.36f,-.05f), new Vector4(1.02f,.34f,.29f,0),
                new Vector4(1.23f,.22f,.22f,.06f), new Vector4(1.38f,.23f,.22f,.10f),
                new Vector4(1.51f,.26f,.25f,.11f), new Vector4(1.68f,.17f,.17f,.10f),
                new Vector4(1.74f,.002f,.002f,.10f)
            };
            geometry.Loft(profile, (height, front) => !stork && !swan && height > 1.08f && height < 1.40f ? 7 : bodyColor);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * .18f;
                int feet = stork ? 4 : swan ? 2 : 4;
                geometry.Ellipsoid(new Vector3(x, (legTop + .09f) * .5f, .015f),
                    new Vector3(stork ? .026f : .04f, (legTop - .09f) * .5f, .035f), Vector3.zero, feet);
                for (int toe = -1; toe <= 1; toe++)
                {
                    geometry.Taper(new Vector3(x,.085f,.02f), new Vector3(x + toe * .08f,.065f,.25f - Mathf.Abs(toe)*.035f), .027f,.02f,feet);
                }

                if (swan)
                {
                    geometry.Ellipsoid(new Vector3(x,.074f,.13f),new Vector3(.11f,.025f,.14f),Vector3.zero,feet);
                }

                float wingY = stork ? 1.02f : .79f;
                float wingX = stork ? .30f : .37f;
                geometry.Ellipsoid(new Vector3(side * wingX,wingY,-.08f),new Vector3(.115f,.30f,.27f),new Vector3(25,0,-side*12),bodyColor);
                for (int feather = 0; feather < 5; feather++)
                {
                    geometry.Ellipsoid(new Vector3(side*(wingX+.025f),wingY-.13f+feather*.032f,-.20f+feather*.055f),
                        new Vector3(.075f,.21f,.065f),new Vector3(32+feather*7,0,-side*12),stork ? 2 : bodyColor);
                }

                if (!stork && !swan)
                {
                    for (int stripe = 0; stripe < 2; stripe++)
                    {
                        geometry.Ellipsoid(new Vector3(side*.47f,.80f+stripe*.13f,-.10f),new Vector3(.018f,.035f,.22f),new Vector3(15,0,0),9);
                    }
                }

                var rotation = Quaternion.Euler(0,side*35,0);
                var eye = new Vector3(side*.153f,headY+.025f,headZ+.17f);
                if (swan)
                {
                    geometry.Ellipsoid(eye,new Vector3(.075f,.065f,.027f),new Vector3(0,side*35,0),2);
                }

                geometry.Ellipsoid(eye+rotation*new Vector3(0,0,.014f),new Vector3(.047f,.055f,.024f),new Vector3(0,side*35,0),2);
                geometry.Ellipsoid(eye+rotation*new Vector3(-.012f,.018f,.038f),new Vector3(.013f,.016f,.007f),new Vector3(0,side*35,0),3);
            }

            geometry.Taper(new Vector3(0,headY-.06f,headZ+.18f),new Vector3(0,headY-.12f,headZ+(stork?.80f:swan?.49f:.43f)),
                stork?.065f:swan?.09f:.06f,stork?.055f:swan?.045f:.04f,stork?4:swan?5:9);
            if (!stork && !swan)
            {
                geometry.Ellipsoid(new Vector3(0,headY-.035f,headZ+.24f),new Vector3(.055f,.03f,.06f),Vector3.zero,3);
            }

            for (int feather = -2; feather <= 2; feather++)
            {
                geometry.Ellipsoid(new Vector3(feather*.07f,stork?.89f:.65f,-.39f),new Vector3(.075f,.06f,swan?.29f:.24f),new Vector3(swan?25:-12,feather*5,0),stork?2:swan?3:9);
            }

            string path = Folder + "/" + species + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh,path);
            }

            geometry.Write(mesh);
            mesh.name = species.ToString();
            EditorUtility.SetDirty(mesh);
            var model = new GameObject(species + "Visual");
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = Material();
            return model;
        }

        private static Material Material()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/BirdPalette.asset");
            if (atlas == null)
            {
                atlas = new Texture2D(160, 16, TextureFormat.RGBA32, false);
                AssetDatabase.CreateAsset(atlas, Folder + "/BirdPalette.asset");
            }

            atlas.Reinitialize(160,16);
            var pixels = new Color[160 * 16];
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 160; x++)
                {
                    pixels[y * 160 + x] = Palette[x / 16];
                }
            }

            atlas.SetPixels(pixels);
            atlas.Apply();
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(atlas);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Bird.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Folder + "/Bird.mat");
            }

            material.SetTexture("_BaseMap", atlas);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .32f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private sealed class Geometry
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<Vector2> _uv = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            private void Vertex(Vector3 point, Vector3 normal, int color)
            {
                _vertices.Add(point);
                _normals.Add(normal.normalized);
                _uv.Add(new Vector2((color + .5f) / Palette.Length, .5f));
            }

            public void Ellipsoid(Vector3 center, Vector3 radii, Vector3 euler, int color)
            {
                const int rings = 20;
                const int sectors = 32;
                int start = _vertices.Count;
                var rotation = Quaternion.Euler(euler);
                for (int ring = 0; ring <= rings; ring++)
                {
                    float latitude = Mathf.PI * ring / rings;
                    for (int sector = 0; sector <= sectors; sector++)
                    {
                        float longitude = 2 * Mathf.PI * sector / sectors;
                        var unit = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                        Vertex(center + rotation * Vector3.Scale(unit, radii),
                            rotation * new Vector3(unit.x / radii.x, unit.y / radii.y, unit.z / radii.z), color);
                    }
                }

                Grid(start, rings, sectors, true);
            }

            public void Taper(Vector3 startPoint, Vector3 endPoint, float width, float height, int color)
            {
                int start = _vertices.Count;
                const int sectors = 24;
                const int rings = 8;
                var rotation = Quaternion.LookRotation(endPoint - startPoint);
                for (int ring = 0; ring <= rings; ring++)
                {
                    float t = ring / (float)rings;
                    float radius = Mathf.Max(.002f, 1 - t);
                    for (int sector = 0; sector <= sectors; sector++)
                    {
                        float angle = sector * 2 * Mathf.PI / sectors;
                        var radial = new Vector3(Mathf.Cos(angle) * width, Mathf.Sin(angle) * height, 0);
                        Vertex(Vector3.Lerp(startPoint, endPoint, t) + rotation * radial * radius,
                            rotation * new Vector3(Mathf.Cos(angle) / width, Mathf.Sin(angle) / height, 1 / (endPoint - startPoint).magnitude), color);
                    }
                }

                Grid(start, rings, sectors, true);
            }

            public void Body(bool magpie)
            {
                // Continuous breast, neck and head; profile = height, width, depth, forward offset.
                var profile = new[] {
                    new Vector4(.38f, .02f, .02f, -.03f), new Vector4(.50f, .28f, .27f, -.04f),
                    new Vector4(.75f, .40f, .36f, -.045f), new Vector4(.98f, .36f, .31f, -.015f),
                    new Vector4(1.19f, .22f, .22f, .045f), new Vector4(1.36f, .27f, .255f, .10f),
                    new Vector4(1.55f, .31f, .29f, .13f), new Vector4(1.71f, .21f, .20f, .11f),
                    new Vector4(1.79f, .002f, .002f, .10f)
                };
                Loft(profile, (height, front) => magpie ? (height < 1.13f && front > .30f ? 3 : 2) : 0);
            }

            public void Loft(Vector4[] profile, Func<float,float,int> colorAt)
            {
                const int steps = 8;
                const int sectors = 64;
                int rings = (profile.Length - 1) * steps;
                int start = _vertices.Count;
                for (int ring = 0; ring <= rings; ring++)
                {
                    int section = Mathf.Min(ring / steps, profile.Length - 2);
                    float t = (ring - section * steps) / (float)steps;
                    var a = profile[Mathf.Max(0, section - 1)];
                    var b = profile[section];
                    var c = profile[section + 1];
                    var d = profile[Mathf.Min(profile.Length - 1, section + 2)];
                    var p = .5f * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t);
                    var slope = .5f * ((-a + c) + 2 * (2 * a - 5 * b + 4 * c - d) * t + 3 * (-a + 3 * b - 3 * c + d) * t * t);
                    for (int sector = 0; sector <= sectors; sector++)
                    {
                        float angle = sector * 2 * Mathf.PI / sectors;
                        float sin = Mathf.Sin(angle);
                        float cos = Mathf.Cos(angle);
                        var tangent = new Vector3(p.y * cos, 0, -p.z * sin);
                        var vertical = new Vector3(slope.y * sin, slope.x, slope.w + slope.z * cos);
                        int color = colorAt(p.x, cos);
                        Vertex(new Vector3(p.y * sin, p.x, p.w + p.z * cos), Vector3.Cross(tangent, vertical), color);
                    }
                }

                Grid(start, rings, sectors, true);
            }

            private void Grid(int start, int rings, int sectors, bool reverse = false)
            {
                for (int ring = 0; ring < rings; ring++)
                {
                    for (int sector = 0; sector < sectors; sector++)
                    {
                        int a = start + ring * (sectors + 1) + sector;
                        int b = a + sectors + 1;
                        if (reverse)
                        {
                            _triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                        }
                        else
                        {
                            _triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                        }
                    }
                }
            }

            public void Write(Mesh mesh)
            {
                // Palette regions must not interpolate through unrelated atlas cells.
                // Split only boundary triangles, retaining the smooth authored normals.
                for (int triangle = 0; triangle < _triangles.Count; triangle += 3)
                {
                    int a = _triangles[triangle];
                    int b = _triangles[triangle + 1];
                    int c = _triangles[triangle + 2];
                    if (_uv[a] == _uv[b] && _uv[a] == _uv[c])
                    {
                        continue;
                    }

                    var color = _uv[a];
                    for (int corner = 0; corner < 3; corner++)
                    {
                        int source = _triangles[triangle + corner];
                        _triangles[triangle + corner] = _vertices.Count;
                        _vertices.Add(_vertices[source]);
                        _normals.Add(_normals[source]);
                        _uv.Add(color);
                    }
                }

                mesh.Clear();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uv);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
            }
        }
    }
}
