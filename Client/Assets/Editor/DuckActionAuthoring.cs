using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Areas.Character.Mono;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ProjectX.Editor
{
    internal static class DuckActionAuthoring
    {
        private const string Folder = "Assets/Art/Characters/Duck";
        private static readonly string[] Names = { "Bow", "Sword", "Wand", "Picaxe", "FishingRod", "Sickle", "Hammer", "Axe", "Spoon" };

        public static void Controller(AnimatorController controller, Transform visual)
        {
            var layers = new List<AnimatorControllerLayer>(controller.layers);
            layers.RemoveAll(layer => layer.name == "Actions");
            controller.layers = layers.ToArray();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)))
            {
                if (asset is AnimatorStateMachine machine && machine.name == "Duck Actions")
                {
                    foreach (var child in machine.states)
                    {
                        machine.RemoveState(child.state);
                    }

                    UnityEngine.Object.DestroyImmediate(machine, true);
                }
            }

            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(Folder + "/DuckUpperBody.mask");
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, Folder + "/DuckUpperBody.mask");
            }

            var transforms = visual.GetComponentsInChildren<Transform>(true);
            mask.transformCount = transforms.Length + 1;
            mask.SetTransformPath(0, "");
            mask.SetTransformActive(0, false);
            for (int i = 0; i < transforms.Length; i++)
            {
                string relative = AnimationUtility.CalculateTransformPath(transforms[i], visual);
                string path = "DuckVisual" + (relative.Length == 0 ? "" : "/" + relative);
                mask.SetTransformPath(i + 1, path);
                mask.SetTransformActive(i + 1, path.StartsWith("DuckVisual/Body/", StringComparison.Ordinal));
            }

            var states = new AnimatorStateMachine { name = "Duck Actions" };
            AssetDatabase.AddObjectToAsset(states, controller);
            controller.AddLayer(new AnimatorControllerLayer { name = "Actions", stateMachine = states,
                avatarMask = mask, defaultWeight = 0, blendingMode = AnimatorLayerBlendingMode.Override });
            foreach (DuckActionEnum action in Enum.GetValues(typeof(DuckActionEnum)))
            {
                if (action == DuckActionEnum.None)
                {
                    continue;
                }

                string path = Folder + "/" + action + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    clip = new AnimationClip();
                    AssetDatabase.CreateAsset(clip, path);
                }

                clip.ClearCurves();
                clip.name = action.ToString();
                clip.frameRate = 60;
                float duration = action >= DuckActionEnum.Mining ? DuckActionPresentation.CycleLength(action) : .4f;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = action >= DuckActionEnum.Mining;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // Every channel on the masked joints is explicit, avoiding defaults from
                // another action leaking into blends or observer reconstruction.
                Pose(clip, "LeftWing", duration, 0, 0, 0);
                Pose(clip, "RightWing", duration, 0, 0, 0);
                Pose(clip, "LeftWing/Tip", duration, 0, 0, 0);
                Pose(clip, "RightWing/Tip", duration, 0, 0, 0);
                Pose(clip, "Neck", duration, 0, 0, 0);
                Pose(clip, "Neck/Head", duration, 0, 0, 0);
                Position(clip, "LeftWing", duration, new Vector3(-.32f, .16f, -.02f));
                Position(clip, "RightWing", duration, new Vector3(.32f, .16f, -.02f));
                Position(clip, "LeftWing/Tip", duration, new Vector3(0, -.25f, -.04f));
                Position(clip, "RightWing/Tip", duration, new Vector3(0, -.25f, -.04f));

                if (action == DuckActionEnum.BowDraw || action == DuckActionEnum.BowRelease)
                {
                    Position(clip, "LeftWing", duration, new Vector3(-.32f, .16f, .15f));
                    Position(clip, "RightWing", duration, new Vector3(.32f, .16f, .15f));
                    Position(clip, "LeftWing/Tip", duration, new Vector3(0, -.4f, -.04f));
                }

                switch (action)
                {
                    case DuckActionEnum.SwordWindup:
                        Curve(clip, "RightWing", "x", duration, -20, -65, -135);
                        Curve(clip, "RightWing", "z", duration, 10, 35, 55);
                        Curve(clip, "Neck", "y", duration, 0, -8, -12);
                        break;
                    case DuckActionEnum.SwordStrike:
                        Curve(clip, "RightWing", "x", duration, -135, -65, -30);
                        Curve(clip, "RightWing", "z", duration, 55, 15, 10);
                        Curve(clip, "Neck", "y", duration, -12, 12, 0);
                        break;
                    case DuckActionEnum.BowDraw:
                        Pose(clip, "LeftWing", duration, -90, 30, 0);
                        Pose(clip, "RightWing", duration, -90, 0, 0);
                        Curve(clip, "RightWing", "y", duration, -35, -45, -55);
                        break;
                    case DuckActionEnum.BowRelease:
                        Pose(clip, "LeftWing", duration, -90, 30, 0);
                        Curve(clip, "RightWing", "x", duration, -90, -80, -60);
                        Curve(clip, "RightWing", "y", duration, -55, -35, 0);
                        break;
                    case DuckActionEnum.WandCharge:
                        Curve(clip, "RightWing", "x", duration, -35, -110, -130);
                        Curve(clip, "RightWing/Tip", "z", duration, 0, -20, 12);
                        Curve(clip, "LeftWing", "x", duration, -10, -45, -65);
                        break;
                    case DuckActionEnum.WandRelease:
                        Curve(clip, "RightWing", "x", duration, -130, -75, -45);
                        Curve(clip, "LeftWing", "x", duration, -65, -40, 0);
                        break;
                    case DuckActionEnum.Fishing:
                        Curve(clip, "RightWing", "x", duration, -65, -72, -65);
                        Curve(clip, "RightWing/Tip", "x", duration, -8, -15, -8);
                        Pose(clip, "LeftWing", duration, -60, -25, -12);
                        Curve(clip, "Neck", "x", duration, 8, 13, 8);
                        break;
                    case DuckActionEnum.Herbalism:
                        Curve(clip, "Neck", "x", duration, 15, 25, 15);
                        Curve(clip, "RightWing", "x", duration, -40, -80, -40);
                        Curve(clip, "RightWing", "z", duration, 15, -25, 15);
                        Curve(clip, "LeftWing", "x", duration, -45, -65, -45);
                        break;
                    case DuckActionEnum.Crafting:
                        Curve(clip, "RightWing", "x", duration, -60, -115, -55, -60);
                        Curve(clip, "RightWing/Tip", "x", duration, 0, -25, 15, 0);
                        Pose(clip, "LeftWing", duration, -70, 0, 0);
                        Curve(clip, "Neck", "x", duration, 10, 5, 15, 10);
                        break;
                    case DuckActionEnum.Cooking: case DuckActionEnum.Alchemy:
                        Pose(clip, "RightWing", duration, -80, 0, 0);
                        Curve(clip, "RightWing/Tip", "z", duration, -20, 0, 20, 0, -20);
                        Curve(clip, "RightWing/Tip", "x", duration, 0, 15, 0, -15, 0);
                        Pose(clip, "LeftWing", duration, -55, -20, 0);
                        break;
                    default:
                        Curve(clip, "RightWing", "x", duration, -55, -150, -45, -55);
                        Curve(clip, "RightWing/Tip", "x", duration, 0, -15, 20, 0);
                        Curve(clip, "LeftWing", "x", duration, -50, -110, -50, -50);
                        Curve(clip, "Neck", "x", duration, 5, -5, 15, 5);
                        break;
                }

                var state = states.AddState(action.ToString());
                state.motion = clip;
                state.writeDefaultValues = false;
                EditorUtility.SetDirty(clip);
            }

            EditorUtility.SetDirty(mask);
        }

        private static void Pose(AnimationClip clip, string bone, float duration, float x, float y, float z)
        {
            Curve(clip, bone, "x", duration, x, x);
            Curve(clip, bone, "y", duration, y, y);
            Curve(clip, bone, "z", duration, z, z);
        }

        private static void Position(AnimationClip clip, string bone, float duration, Vector3 position)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                clip.SetCurve("DuckVisual/Body/" + bone, typeof(Transform), "localPosition." + "xyz"[axis],
                    AnimationCurve.Constant(0, duration, position[axis]));
            }
        }

        private static void Curve(AnimationClip clip, string bone, string axis, float duration, params float[] values)
        {
            var keys = new Keyframe[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                keys[i] = new Keyframe(duration * i / (values.Length - 1), values[i]);
            }

            clip.SetCurve("DuckVisual/Body/" + bone, typeof(Transform), "localEulerAnglesRaw." + axis, new AnimationCurve(keys));
        }

        public static void Install(GameObject actor)
        {
            var visual = actor.transform.Find("DuckVisual");
            var left = Grip(visual.Find("Body/LeftWing/Tip"));
            var right = Grip(visual.Find("Body/RightWing/Tip"));
            var presentation = actor.GetComponent<DuckActionPresentation>() ?? actor.AddComponent<DuckActionPresentation>();
            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("_animator").objectReferenceValue = actor.GetComponent<Animator>();
            serialized.FindProperty("_visual").objectReferenceValue = visual;
            serialized.FindProperty("_leftGrip").objectReferenceValue = left;
            serialized.FindProperty("_rightGrip").objectReferenceValue = right;
            var props = serialized.FindProperty("_props");
            props.arraySize = Names.Length;

            for (int i = 0; i < Names.Length; i++)
            {
                var existing = actor.transform.Find(Names[i]);
                var prop = existing != null ? existing.gameObject : new GameObject(Names[i]);
                if (PrefabUtility.IsAnyPrefabInstanceRoot(prop))
                {
                    PrefabUtility.UnpackPrefabInstance(prop, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                // Tool wrappers are gameplay attachment points, not imported models.
                // Remove inherited mesh components as well as obsolete children.
                foreach (var component in prop.GetComponents<Component>())
                {
                    if (component is MeshRenderer || component is SkinnedMeshRenderer
                        || component is MeshFilter || component is Collider || component is Animator)
                    {
                        UnityEngine.Object.DestroyImmediate(component);
                    }
                }

                prop.transform.SetParent(actor.transform, false);
                prop.transform.localScale = Vector3.one * DuckCharacterBuilder.VisualScale;
                var previous = prop.transform.Find("HeldModel");
                if (previous != null)
                {
                    UnityEngine.Object.DestroyImmediate(previous.gameObject);
                }

                // Keep the profession wrapper and fishing endpoints; replace obsolete
                // imported geometry instead of accumulating disabled tool models.
                var obsoleteChildren = new List<GameObject>();
                foreach (Transform child in prop.transform)
                {
                    if (child.name != "Tip")
                    {
                        if (child.GetComponentInChildren<Unity.Netcode.NetworkBehaviour>(true) != null)
                        {
                            throw new InvalidOperationException("Cannot replace networked tool hierarchy: " + child.name);
                        }

                        obsoleteChildren.Add(child.gameObject);
                    }
                }

                foreach (var child in obsoleteChildren)
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }

                var model = BuildProp(i);
                model.name = "HeldModel";
                model.transform.SetParent(prop.transform, false);
                props.GetArrayElementAtIndex(i).objectReferenceValue = prop;
                if (i == 0)
                {
                    serialized.FindProperty("_bowString").objectReferenceValue = model.GetComponentInChildren<LineRenderer>();
                    serialized.FindProperty("_nockedArrow").objectReferenceValue = model.transform.Find("Arrow").gameObject;
                }

                if (i == 4)
                {
                    prop.GetComponent<LineRenderer>().positionCount = 0;
                    var tip = prop.transform.Find("Tip");
                    if (tip != null)
                    {
                        tip.localPosition = new Vector3(0, 1.2f, 0);
                    }
                }

                prop.SetActive(false);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform Grip(Transform bone)
        {
            var grip = bone.Find("Grip");
            if (grip == null)
            {
                grip = new GameObject("Grip").transform;
                grip.SetParent(bone, false);
            }

            grip.localPosition = new Vector3(0, -.10f, -.03f);
            grip.localRotation = Quaternion.Euler(180, 0, 0);
            return grip;
        }

        private static GameObject BuildProp(int kind)
        {
            var root = new GameObject(Names[kind]);
            var wood = Material("ToolWood", new Color(.24f, .095f, .035f), .25f, 0);
            var steel = Material("ToolSteel", new Color(.55f, .65f, .72f), .65f, .8f);
            var bronze = Material("ToolBronze", new Color(.58f, .32f, .085f), .55f, .7f);
            var gem = Material("ToolCrystal", new Color(.08f, .65f, .9f), .8f, .2f);
            var stringMaterial = Material("ToolString", new Color(.8f, .74f, .53f), .15f, 0);

            if (kind == 0)
            {
                Vector3 previous = new Vector3(0, -.48f, -.19f);
                for (int i = 1; i <= 16; i++)
                {
                    float t = i / 16f;
                    var next = new Vector3(0, Mathf.Lerp(-.48f, .48f, t), .19f * (Mathf.Sin(t * Mathf.PI) - 1));
                    Rod(root, previous, next, .022f, wood);
                    previous = next;
                }

                Rod(root, new Vector3(0, -.09f, 0), new Vector3(0, .09f, 0), .035f, bronze);
                var line = new GameObject("String").AddComponent<LineRenderer>();
                line.transform.SetParent(root.transform, false);
                line.useWorldSpace = false;
                line.sharedMaterial = stringMaterial;
                line.widthMultiplier = .008f;
                line.positionCount = 3;
                line.SetPositions(new[] { new Vector3(0, -.48f, -.19f), new Vector3(0, 0, -.19f), new Vector3(0, .48f, -.19f) });
                var arrow = new GameObject("Arrow");
                arrow.transform.SetParent(root.transform, false);
                Rod(arrow, new Vector3(0, 0, -.24f), new Vector3(0, 0, .40f), .008f, wood);
                Shape(arrow, PrimitiveType.Cube, new Vector3(0, 0, .40f), new Vector3(.03f, .025f, .065f), steel);
            }
            else if (kind == 1)
            {
                Rod(root, new Vector3(0, -.14f, 0), new Vector3(0, .14f, 0), .035f, wood);
                Rod(root, new Vector3(-.15f, .14f, 0), new Vector3(.15f, .14f, 0), .025f, bronze);
                Blade(root, steel);
                Shape(root, PrimitiveType.Sphere, new Vector3(0, -.16f, 0), Vector3.one * .085f, bronze);
            }
            else if (kind == 2)
            {
                Rod(root, new Vector3(0, -.1f, 0), new Vector3(0, .4f, 0), .025f, wood);
                Shape(root, PrimitiveType.Sphere, new Vector3(0, .43f, 0), new Vector3(.11f, .17f, .11f), gem);
                Shape(root, PrimitiveType.Cylinder, new Vector3(0, .34f, 0), new Vector3(.085f, .025f, .085f), bronze);
            }
            else if (kind == 4)
            {
                Rod(root, new Vector3(0, -.16f, 0), new Vector3(0, .3f, 0), .035f, wood);
                Rod(root, new Vector3(0, .3f, 0), new Vector3(0, 1.2f, 0), .012f, wood);
                Shape(root, PrimitiveType.Cylinder, new Vector3(.055f, .08f, 0), new Vector3(.12f, .035f, .12f), bronze);
            }
            else
            {
                Rod(root, new Vector3(0, -.15f, 0), new Vector3(0, .48f, 0), .028f, wood);
                if (kind == 3)
                {
                    PickHead(root, steel);
                    Shape(root, PrimitiveType.Cylinder, new Vector3(0, .44f, 0), new Vector3(.08f, .055f, .08f), bronze);
                }
                else if (kind == 5)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * Mathf.PI / 12;
                        float b = (i + 1) * Mathf.PI / 12;
                        Rod(root, new Vector3(.14f * Mathf.Sin(a), .48f + .14f * Mathf.Cos(a), 0),
                            new Vector3(.14f * Mathf.Sin(b), .48f + .14f * Mathf.Cos(b), 0), .018f, steel);
                    }
                }
                else if (kind == 8)
                {
                    Shape(root, PrimitiveType.Sphere, new Vector3(0, .52f, 0), new Vector3(.12f, .18f, .04f), wood);
                }
                else if (kind == 7)
                {
                    AxeHead(root, steel);
                }
                else
                {
                    Shape(root, PrimitiveType.Cube, new Vector3(kind == 7 ? .07f : 0, .47f, 0),
                        new Vector3(kind == 7 ? .26f : .32f, .15f, .12f), steel);
                }
            }

            CombineProp(root, Names[kind]);
            // Working heads strike in the wrist's sagittal swing plane.
            if (kind == 3 || kind == 6 || kind == 7)
            {
                root.transform.localRotation = Quaternion.Euler(0, -90, 0);
            }

            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + Names[kind] + "Held.prefab");
            return root;
        }

        private static void CombineProp(GameObject root, string name)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var filters = new List<MeshFilter>();
            foreach (Transform child in root.transform)
            {
                var filter = child.GetComponent<MeshFilter>();
                if (filter == null)
                {
                    continue;
                }

                var material = child.GetComponent<MeshRenderer>().sharedMaterial;
                if (!groups.TryGetValue(material, out var group))
                {
                    group = new List<CombineInstance>();
                    groups.Add(material, group);
                }

                group.Add(new CombineInstance { mesh = filter.sharedMesh, transform = child.localToWorldMatrix });
                filters.Add(filter);
            }

            var combined = new List<CombineInstance>();
            var materials = new List<Material>();
            foreach (var group in groups)
            {
                var submesh = new Mesh();
                submesh.CombineMeshes(group.Value.ToArray(), true, true);
                combined.Add(new CombineInstance { mesh = submesh, transform = Matrix4x4.identity });
                materials.Add(group.Key);
            }

            string path = Folder + "/" + name + "HeldMesh.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.CombineMeshes(combined.ToArray(), false, false);
            EditorUtility.SetDirty(mesh);
            var geometry = new GameObject("Geometry");
            geometry.transform.SetParent(root.transform, false);
            geometry.AddComponent<MeshFilter>().sharedMesh = mesh;
            geometry.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
            foreach (var part in combined)
            {
                UnityEngine.Object.DestroyImmediate(part.mesh);
            }

            foreach (var filter in filters)
            {
                UnityEngine.Object.DestroyImmediate(filter.gameObject);
            }
        }

        private static void AxeHead(GameObject root, Material material)
        {
            var vertices = new[] {
                new Vector3(-.065f, .41f, -.045f), new Vector3(-.065f, .53f, -.045f),
                new Vector3(-.065f, .41f, .045f), new Vector3(-.065f, .53f, .045f),
                new Vector3(.12f, .38f, -.035f), new Vector3(.12f, .56f, -.035f),
                new Vector3(.12f, .38f, .035f), new Vector3(.12f, .56f, .035f),
                new Vector3(.24f, .34f, 0), new Vector3(.24f, .60f, 0)
            };
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/AxeHead.asset");
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, Folder + "/AxeHead.asset");
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = new[] { 0,1,4, 1,5,4, 2,6,3, 3,6,7, 0,2,1, 1,2,3,
                0,4,2, 2,4,6, 1,3,5, 3,7,5, 4,5,8, 5,9,8, 6,8,7, 7,8,9,
                4,8,6, 5,7,9 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            var head = new GameObject("BeveledAxeHead");
            head.transform.SetParent(root.transform, false);
            head.AddComponent<MeshFilter>().sharedMesh = mesh;
            head.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void PickHead(GameObject root, Material material)
        {
            const int sections = 24;
            const int sides = 12;
            var vertices = new Vector3[(sections + 1) * sides];
            var triangles = new List<int>();
            for (int section = 0; section <= sections; section++)
            {
                float t = section * 2f / sections - 1;
                float radius = .042f * Mathf.Sqrt(Mathf.Max(.001f, 1 - t * t));
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * 2 * Mathf.PI / sides;
                    vertices[section * sides + side] = new Vector3(.34f * t,
                        .51f - .14f * t * t + radius * Mathf.Cos(angle), radius * .7f * Mathf.Sin(angle));
                    if (section < sections)
                    {
                        int a = section * sides + side;
                        int b = section * sides + (side + 1) % sides;
                        triangles.AddRange(new[] { a, b, a + sides, b, b + sides, a + sides });
                    }
                }
            }

            string path = Folder + "/PickHead.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            var head = new GameObject("ForgedPickHead");
            head.transform.SetParent(root.transform, false);
            head.AddComponent<MeshFilter>().sharedMesh = mesh;
            head.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material Material(string name, Color color, float smoothness, float metallic)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Rod(GameObject root, Vector3 start, Vector3 end, float radius, Material material)
        {
            var part = Shape(root, PrimitiveType.Cylinder, (start + end) * .5f,
                new Vector3(radius * 2, (end - start).magnitude * .5f, radius * 2), material);
            part.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
        }

        private static Transform Shape(GameObject root, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part.transform;
        }

        private static void Blade(GameObject root, Material material)
        {
            var vertices = new[] { new Vector3(-.065f,.16f,0), new Vector3(0,.16f,.025f), new Vector3(.065f,.16f,0),
                new Vector3(0,.16f,-.025f), new Vector3(-.04f,.68f,0), new Vector3(0,.68f,.015f),
                new Vector3(.04f,.68f,0), new Vector3(0,.68f,-.015f), new Vector3(0,.83f,0) };
            var triangles = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                triangles.AddRange(new[] { i, i + 4, next, next, i + 4, next + 4, i + 4, 8, next + 4 });
            }

            string path = Folder + "/SwordBlade.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var blade = new GameObject("BeveledBlade");
            blade.transform.SetParent(root.transform, false);
            blade.AddComponent<MeshFilter>().sharedMesh = mesh;
            blade.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
