using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ProjectX.Editor
{
    public static class DuckCharacterBuilder
    {
        private const string Folder = "Assets/Art/Characters/Duck";
        internal const float VisualScale = .9f;
        private const string PlayerPath = "Assets/Prefabs/Character/NestedParentArmature_Unpack Variant.prefab";

        [MenuItem("ProjectX/Character/Rebuild Duck")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();

            var cream = Material("Feathers", new Color(1f, .82f, .32f));
            var pale = Material("Breast", new Color(1f, .95f, .72f));
            var orange = Material("BillAndFeet", new Color(1f, .38f, .055f));
            var dark = Material("Eyes", new Color(.025f, .037f, .045f));
            var white = Material("EyeHighlights", Color.white);
            var bodyMaterial = BodyMaterial(cream, pale);
            var root = new GameObject("DuckVisual");
            var body = Joint(root.transform, "Body", new Vector3(0, .72f, 0));
            Part(body, "BodyMesh", Vector3.zero, new Vector3(.92f, 1.12f, .80f), bodyMaterial);


            var neck = Joint(body, "Neck", new Vector3(0, .38f, .04f));
            var head = Joint(neck, "Head", new Vector3(0, .24f, .06f));
            Part(head, "HeadMesh", Vector3.zero, new Vector3(.79f, .75f, .73f), cream);
            Part(head, "Bill", new Vector3(0, -.115f, .365f), new Vector3(.46f, .12f, .39f), orange);
            Part(head, "LowerBill", new Vector3(0, -.172f, .365f), new Vector3(.435f, .05f, .365f), orange);

            for (int side = -1; side <= 1; side += 2)
            {
                string label = side < 0 ? "Left" : "Right";
                var eyeRotation = Quaternion.Euler(0, side * 40f, 0);
                var eyePosition = new Vector3(side * .242f, .045f, .30f);
                Part(head, label + "Eye", eyePosition, new Vector3(.125f, .17f, .075f), dark).localRotation = eyeRotation;
                Part(head, label + "Glint", eyePosition + eyeRotation * new Vector3(-.018f, .03f, .037f),
                    new Vector3(.035f, .045f, .018f), white).localRotation = eyeRotation;
                Part(head, label + "Nostril", new Vector3(side * .095f, -.056f, .42f), new Vector3(.035f, .018f, .05f), dark);

                var wing = Joint(body, label + "Wing", new Vector3(side * .32f, .16f, -.02f));
                Joint(wing, "Tip", new Vector3(0, -.25f, -.04f));
                Part(wing, "WingMesh", new Vector3(side * .025f, -.17f, -.015f), new Vector3(.22f, .58f, .40f), pale);

                var foot = Joint(root.transform, label + "Foot", new Vector3(side * .22f, .19f, .03f));
                Joint(foot, "Toes", new Vector3(0, -.10f, .14f));
                Part(foot, "Ankle", Vector3.zero, new Vector3(.13f, .34f, .135f), orange);
                Part(foot, "WebbedFoot", new Vector3(0, -.12f, .105f), new Vector3(.34f, .11f, .43f), orange);
                for (int feather = 0; feather < 3; feather++)
                {
                    Part(wing, "FlightFeather" + feather,
                        new Vector3(side * .075f, -.28f + feather * .025f, -.14f + feather * .105f),
                        new Vector3(.16f, .38f - feather * .025f, .14f), pale);
                }
            }

            var tail = Part(body, "Tail", new Vector3(0, -.03f, -.39f), new Vector3(.36f, .23f, .40f), pale);
            tail.localRotation = Quaternion.Euler(-25, 0, 0);
            Part(head, "Crest", new Vector3(-.035f, .34f, -.035f), new Vector3(.19f, .29f, .19f), pale).localRotation = Quaternion.Euler(-20, 0, -18);

            var idle = Clip("Idle", 1.6f, true);
            Curve(idle, "Body", "m_LocalPosition.y", .72f, .735f, .72f);
            Curve(idle, "Body/Neck", "localEulerAnglesRaw.x", 0, -2, 0);
            var walk = Clip("Walk", .64f, true);
            Curve(walk, "Body", "m_LocalPosition.y", .72f, .76f, .72f, .76f, .72f);
            Curve(walk, "Body", "localEulerAnglesRaw.z", -6, 0, 6, 0, -6);
            Curve(walk, "Body/Neck/Head", "localEulerAnglesRaw.z", 4, 0, -4, 0, 4);
            Curve(walk, "LeftFoot", "localEulerAnglesRaw.x", -28, 0, 28, 0, -28);
            Curve(walk, "RightFoot", "localEulerAnglesRaw.x", 28, 0, -28, 0, 28);
            Curve(walk, "LeftFoot", "m_LocalPosition.y", .19f, .29f, .19f, .19f, .19f);
            Curve(walk, "RightFoot", "m_LocalPosition.y", .19f, .19f, .19f, .29f, .19f);
            Curve(walk, "Body/LeftWing", "localEulerAnglesRaw.x", 12, 0, -12, 0, 12);
            Curve(walk, "Body/RightWing", "localEulerAnglesRaw.x", -12, 0, 12, 0, -12);

            Curve(walk, "Body/LeftWing/Tip", "localEulerAnglesRaw.x", -8, 0, 8, 0, -8);
            Curve(walk, "Body/RightWing/Tip", "localEulerAnglesRaw.x", 8, 0, -8, 0, 8);
            Curve(walk, "LeftFoot/Toes", "localEulerAnglesRaw.x", 0, -18, 8, 0, 0);
            Curve(walk, "RightFoot/Toes", "localEulerAnglesRaw.x", 8, 0, 0, -18, 8);

            var jump = Clip("Jump", .4f, false);
            Curve(jump, "Body/Neck", "localEulerAnglesRaw.x", 0, -10, -4);
            Curve(jump, "Body/LeftWing/Tip", "localEulerAnglesRaw.z", 0, -20, -12);
            Curve(jump, "Body/RightWing/Tip", "localEulerAnglesRaw.z", 0, 20, 12);
            Curve(jump, "Body", "localEulerAnglesRaw.x", -12, -5, 5);
            Curve(jump, "Body/LeftWing", "localEulerAnglesRaw.z", -15, -65, -55);
            Curve(jump, "Body/RightWing", "localEulerAnglesRaw.z", 15, 65, 55);
            Curve(jump, "LeftFoot", "localEulerAnglesRaw.x", 0, -40, -25);
            Curve(jump, "RightFoot", "localEulerAnglesRaw.x", 0, -40, -25);
            var fall = Clip("Fall", .6f, true);
            Curve(fall, "Body/LeftWing", "localEulerAnglesRaw.z", -55, -70, -55);
            Curve(fall, "Body/RightWing", "localEulerAnglesRaw.z", 55, 70, 55);
            var land = Clip("Land", .18f, false);
            Curve(land, "Body", "m_LocalScale.y", 1, .86f, 1);
            Curve(land, "Body", "m_LocalPosition.y", .72f, .65f, .72f);

            string controllerPath = Folder + "/Duck.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Bool);
            controller.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states)
            {
                machine.RemoveState(child.state);
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(controllerPath))
            {
                if (asset is BlendTree oldBlend && oldBlend.name == "Duck Locomotion")
                {
                    UnityEngine.Object.DestroyImmediate(oldBlend, true);
                }
            }

            var locomotion = machine.AddState("Locomotion");
            var blend = new BlendTree { name = "Duck Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(idle, 0);
            blend.AddChild(walk, 3);
            locomotion.motion = blend;
            machine.defaultState = locomotion;
            var jumping = machine.AddState("Jump");
            jumping.motion = jump;
            var falling = machine.AddState("Fall");
            falling.motion = fall;
            var landing = machine.AddState("Land");
            landing.motion = land;
            Transition(locomotion, jumping, "Jump", true);
            Transition(locomotion, falling, "Grounded", false);
            Transition(jumping, falling, "FreeFall", true);
            Transition(jumping, landing, "Grounded", true).conditions = new[] {
                new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "Grounded" },
                new AnimatorCondition { mode = AnimatorConditionMode.IfNot, parameter = "Jump" }
            };
            Transition(falling, landing, "Grounded", true);
            var settled = landing.AddTransition(locomotion);
            settled.hasExitTime = true;
            settled.exitTime = 1;
            settled.duration = .06f;
            Transition(landing, jumping, "Jump", true);
            Transition(landing, falling, "Grounded", false);

            // Bind paths relative to the existing root Animator for NetworkAnimator compatibility.
            foreach (var clip in new[] { idle, walk, jump, fall, land })
            {
                foreach (string side in new[] { "Left", "Right" })
                {
                    string wing = "Body/" + side + "Wing";
                    float x = side == "Left" ? -.32f : .32f;
                    Curve(clip, wing, "m_LocalPosition.x", x, x);
                    Curve(clip, wing, "m_LocalPosition.y", .16f, .16f);
                    Curve(clip, wing, "m_LocalPosition.z", -.02f, -.02f);
                    Curve(clip, wing + "/Tip", "m_LocalPosition.x", 0, 0);
                    Curve(clip, wing + "/Tip", "m_LocalPosition.y", -.25f, -.25f);
                    Curve(clip, wing + "/Tip", "m_LocalPosition.z", -.04f, -.04f);
                }

                // Animator evaluates complete position channels. Keep the authored stance
                // explicit in every state, including blends with the walking foot lift.
                Curve(clip, "LeftFoot", "m_LocalPosition.x", -.22f, -.22f);
                Curve(clip, "RightFoot", "m_LocalPosition.x", .22f, .22f);
                Curve(clip, "LeftFoot", "m_LocalPosition.z", .03f, .03f);
                Curve(clip, "RightFoot", "m_LocalPosition.z", .03f, .03f);
                if (clip != walk)
                {
                    Curve(clip, "LeftFoot", "m_LocalPosition.y", .19f, .19f);
                    Curve(clip, "RightFoot", "m_LocalPosition.y", .19f, .19f);
                }

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    var prefixed = binding;
                    prefixed.path = "DuckVisual/" + binding.path;
                    AnimationUtility.SetEditorCurve(clip, prefixed, curve);
                }
            }

            DuckSkinAuthoring.Build(root.transform);
            root.transform.localScale = Vector3.one * VisualScale;
            DuckActionAuthoring.Controller(controller, root.transform);

            var model = new GameObject("DuckCharacter");
            root.transform.SetParent(model.transform, false);
            model.AddComponent<Animator>().runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(model, Folder + "/DuckCharacter.prefab");

            var player = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var animator = player.GetComponentInChildren<Animator>(true);
                var previous = animator.transform.Find("DuckVisual");
                if (previous != null)
                {
                    UnityEngine.Object.DestroyImmediate(previous.gameObject);
                }

                DuckSkinValidation.RemoveLegacyVisual(player, animator.transform);

                var playerVisual = UnityEngine.Object.Instantiate(root, animator.transform, false);
                playerVisual.name = "DuckVisual";
                var movement = new SerializedObject(animator.GetComponent<StarterAssets.ThirdPersonController>());
                movement.FindProperty("_geometry").objectReferenceValue = playerVisual;
                movement.ApplyModifiedPropertiesWithoutUndo();

                var capsule = animator.GetComponent<CharacterController>();
                capsule.height = 1.62f;
                capsule.center = new Vector3(0, .81f, 0);
                capsule.radius = .28f;
                var cameraTarget = animator.GetComponent<StarterAssets.ThirdPersonController>().CinemachineCameraTarget.transform;
                cameraTarget.localPosition = new Vector3(0, 1.25f, 0);

                animator.avatar = null;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                DuckActionAuthoring.Install(animator.gameObject);
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
                UnityEngine.Object.DestroyImmediate(model);
            }

            AssetDatabase.SaveAssets();
            DuckSkinValidation.ValidatePlayer(PlayerPath);
            ValidateAndPreview();
            DuckActionValidation.Run();
            Debug.Log("DUCK_CHARACTER_BUILT: model, five animation clips and network player appearance saved.");
        }

        [MenuItem("ProjectX/Character/Validate And Preview Duck")]
        public static void ValidateAndPreview()
        {
            var preview = new PreviewRenderUtility();
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/DuckCharacter.prefab"));
            preview.AddSingleGO(model);
            preview.camera.transform.position = new Vector3(2.6f, 1.9f, 3.8f);
            preview.camera.transform.LookAt(new Vector3(0, .85f, 0));
            preview.camera.fieldOfView = 29;
            preview.camera.nearClipPlane = .05f;
            preview.camera.farClipPlane = 100;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.12f, .18f, .22f);
            preview.lights[0].intensity = 1.4f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35, -30, 0);
            preview.lights[1].intensity = .8f;
            preview.ambientColor = new Color(.45f, .45f, .45f);
            Directory.CreateDirectory("Logs/DuckPreview");

            try
            {
                int triangleCount = 0;
                foreach (var filter in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = filter.sharedMesh;
                    triangleCount += mesh.triangles.Length / 3;
                    foreach (var normal in mesh.normals)
                    {
                        if (float.IsNaN(normal.x) || Mathf.Abs(normal.sqrMagnitude - 1) > .01f)
                        {
                            throw new InvalidOperationException("Invalid sculpted mesh normal: " + filter.name);
                        }
                    }
                }

                DuckSkinValidation.Validate(model);
                Debug.Log("DUCK_MESH_VALIDATED: triangles=" + triangleCount + "; normals normalized.");

                foreach (string name in new[] { "Idle", "Walk", "Jump", "Fall", "Land" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + name + ".anim");
                    if (clip == null || clip.length <= 0)
                    {
                        throw new InvalidOperationException("Invalid duck clip: " + name);
                    }

                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (model.transform.Find(binding.path) == null)
                        {
                            throw new InvalidOperationException("Missing animation target: " + binding.path);
                        }
                    }

                    // Reset all animated transforms before sampling an independent pose.
                    model.GetComponent<Animator>().Rebind();
                    clip.SampleAnimation(model, clip.length * .5f);
                    // Freeze the sampled deformation for the editor preview camera; its
                    // render loop must not re-evaluate the live controller over this pose.
                    var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                    var poseMesh = new Mesh();
                    skin.BakeMesh(poseMesh);
                    var pose = new GameObject("SampledPose");
                    pose.transform.SetParent(skin.transform, false);
                    pose.AddComponent<MeshFilter>().sharedMesh = poseMesh;
                    pose.AddComponent<MeshRenderer>().sharedMaterial = skin.sharedMaterial;
                    skin.enabled = false;

                    preview.BeginStaticPreview(new Rect(0, 0, 768, 768));
                    preview.Render(true);
                    var texture = preview.EndStaticPreview();
                    File.WriteAllBytes("Logs/DuckPreview/" + name + ".png", texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                    UnityEngine.Object.DestroyImmediate(pose);
                    UnityEngine.Object.DestroyImmediate(poseMesh);
                    skin.enabled = true;
                }

                Debug.Log("DUCK_CHARACTER_VALIDATED: five clips have valid transform bindings and rendered poses.");
            }
            finally
            {
                preview.Cleanup();
            }
        }

        private static Material Material(string name, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .28f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform Joint(Transform parent, string name, Vector3 position)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = position;
            return joint;
        }

        private static Transform Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = new GameObject(name);
            part.name = name;
            part.AddComponent<MeshFilter>().sharedMesh = SculptedMesh(name);
            part.AddComponent<MeshRenderer>();
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }

        private static Material BodyMaterial(Material feathers, Material breast)
        {
            const int size = 256;
            string path = Folder + "/Plumage.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
                AssetDatabase.CreateAsset(texture, path);
            }

            var gold = feathers.GetColor("_BaseColor");
            var ivory = breast.GetColor("_BaseColor");
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float v = (float)y / (size - 1);
                    float front = Mathf.Abs(u - .25f) / .19f;
                    float height = (v - .59f) / .32f;
                    float edge = Mathf.InverseLerp(.82f, 1.08f, Mathf.Sqrt(front * front + height * height));
                    float patch = 1 - Mathf.SmoothStep(0, 1, edge);
                    var color = Color.Lerp(gold, ivory, patch);
                    color *= Mathf.Lerp(.88f, 1.05f, 1 - v);
                    color.a = 1;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            EditorUtility.SetDirty(texture);
            var material = Material("BodyPlumage", Color.white);
            material.SetTexture("_BaseMap", texture);
            return material;
        }

        // Dense, shaped surfaces rather than scaled built-in primitives. All meshes
        // are persisted so the runtime has no procedural construction cost.
        private static Mesh SculptedMesh(string name)
        {
            int rings = name.Contains("Eye") || name.Contains("Glint") || name.Contains("Nostril") ? 12 : 24;
            int segments = rings == 12 ? 24 : 40;
            var mesh = new Mesh();

            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var indices = new int[(rings - 1) * segments * 6];
            int index = 0;

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = (float)ring / rings;
                float theta = v * Mathf.PI;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = (float)segment / segments;
                    float phi = u * Mathf.PI * 2;
                    int vertex = ring * (segments + 1) + segment;
                    vertices[vertex] = Surface(name, theta, phi);
                    uv[vertex] = new Vector2(u, v);

                    // Analytic finite differences keep normals smooth across the UV seam.
                    float safeTheta = Mathf.Clamp(theta, .0005f, Mathf.PI - .0005f);
                    var latitude = Surface(name, safeTheta + .0001f, phi) - Surface(name, safeTheta - .0001f, phi);
                    var longitude = Surface(name, safeTheta, phi + .0001f) - Surface(name, safeTheta, phi - .0001f);
                    normals[vertex] = Vector3.Cross(longitude * 10000, latitude * 10000).normalized;

                    if (ring == rings || segment == segments)
                    {
                        continue;
                    }

                    int next = vertex + segments + 1;
                    if (ring > 0)
                    {
                        indices[index++] = vertex;
                        indices[index++] = vertex + 1;
                        indices[index++] = next;
                    }

                    if (ring < rings - 1)
                    {
                        indices[index++] = vertex + 1;
                        indices[index++] = next + 1;
                        indices[index++] = next;
                    }
                }
            }

            mesh.Clear();
            mesh.name = name + " Sculpt";
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Vector3 Surface(string name, float theta, float phi)
        {
            float height = Mathf.Cos(theta);
            float radius = Mathf.Sin(theta);
            float x = radius * Mathf.Cos(phi);
            float z = radius * Mathf.Sin(phi);

            if (name == "BodyMesh")
            {
                float pear = 1 - .23f * height;
                x *= pear;
                z = z * pear - .08f * (1 - height * height);
            }
            else if (name == "HeadMesh")
            {
                x *= 1 + .10f * Mathf.Exp(-Mathf.Pow((height + .35f) * 2.5f, 2));
                z += .07f * (1 - height * height);
            }
            else if (name.Contains("Bill"))
            {
                x = SignedPower(x, .7f) * (1 + .10f * z);
                z = SignedPower(z, .75f);
                height = SignedPower(height, .75f) - .13f * z;
            }
            else if (name == "WebbedFoot")
            {
                // Broad front fan, scalloped toe edge, narrow heel; one continuous web.
                float fan = .67f + .33f * Mathf.Sin(phi);
                x *= fan;
                if (z > 0)
                {
                    z *= .91f + .09f * Mathf.Cos(3 * Mathf.PI * x);
                }

                height *= .72f + .28f * (1 - z);
            }
            else if (name == "WingMesh" || name.StartsWith("FlightFeather", StringComparison.Ordinal))
            {
                float taper = .72f + .28f * height;
                x *= taper;
                z = z * taper - .19f * (1 - height);
            }
            else if (name == "Crest" || name == "Tail")
            {
                x *= .75f - .25f * height;
                z -= .22f * (height + 1) * (height + 1);
            }

            return new Vector3(x, height, z) * .5f;
        }

        private static float SignedPower(float value, float exponent)
        {
            return Mathf.Sign(value) * Mathf.Pow(Mathf.Abs(value), exponent);
        }

        private static AnimationClip Clip(string name, float duration, bool loop)
        {
            string path = Folder + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.name = name;
            clip.frameRate = 30;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void Curve(AnimationClip clip, string path, string property, params float[] values)
        {
            float duration = AnimationUtility.GetAnimationClipSettings(clip).stopTime;
            var keys = new Keyframe[values.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = new Keyframe(duration * i / (keys.Length - 1), values[i]);
            }

            clip.SetCurve(path, typeof(Transform), property, new AnimationCurve(keys));
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = .09f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter);
            return transition;
        }
    }
}
