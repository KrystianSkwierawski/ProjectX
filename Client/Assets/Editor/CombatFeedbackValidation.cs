using System;
using System.IO;
using System.Reflection;
using Assets.Scripts.Areas.Character.Mono;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Shared.Extensions;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace ProjectX.Editor
{
    public static class CombatFeedbackValidation
    {
        private const string _prefabPath = "Assets/Prefabs/Combat/Resources/CombatHitEffect.prefab";
        private const string _materialPath = "Assets/Materials/CombatHit.mat";
        private const string _texturePath = "Assets/Materials/CombatHit.png";
        private const BindingFlags _private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("ProjectX/Combat/Rebuild Hit Effect")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_prefabPath));

            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);

            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var radius = new Vector2((x - 15.5f) / 15f, (y - 15.5f) / 15f).magnitude;
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - radius) * 6)));
                }
            }

            File.WriteAllBytes(_texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(_texturePath);

            var material = AssetDatabase.LoadAssetAtPath<Material>(_materialPath);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, _materialPath);
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(_texturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);

            var effect = new GameObject("CombatHitEffect");
            var particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.duration = 0.12f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.085f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.025f, 0.015f), new Color(1f, 0.18f, 0.08f));
            main.gravityModifier = 0.65f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = 24;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 24) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 55;
            shape.radius = 0.12f;

            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.35f), new GradientAlphaKey(0, 1) }
            };

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.2f));

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            PrefabUtility.SaveAsPrefabAsset(effect, _prefabPath);
            UnityEngine.Object.DestroyImmediate(effect);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Run();
        }

        public static void Run()
        {
            ValidateCameraSteering();
            ValidateParticles();

            Debug.Log($"COMBAT_FEEDBACK_VALIDATED: RMB retains active combat for both camera modes; shared {TransformExtensions.MaxFacingAngle} degree boundary; detached hit burst, material and lifetime.");
        }

        public static void Preview()
        {
            Run();

            var scene = EditorSceneManager.NewPreviewScene();
            var cameraObject = new GameObject("HitPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0, 0.15f, -2);
            camera.orthographic = true;
            camera.orthographicSize = 0.75f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.18f, 0.24f, 0.29f);

            var effect = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("CombatHitEffect"));
            SceneManager.MoveGameObjectToScene(effect, scene);
            effect.GetComponent<ParticleSystem>().Simulate(0.15f, true, true);

            var render = new RenderTexture(512, 512, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = render;
            camera.Render();
            RenderTexture.active = render;

            var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/combat-hit-preview.png", image.EncodeToPNG());

            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(render);
            EditorSceneManager.ClosePreviewScene(scene);
        }

        private static void ValidateCameraSteering()
        {
            var actor = new GameObject("CombatSteeringProbe");
            var selector = actor.AddComponent<TargetSelector>();
            var movement = actor.AddComponent<ThirdPersonController>();
            movement.Input = actor.AddComponent<StarterAssetsInputs>();
            movement.CinemachineCameraTarget = new GameObject("CameraTarget");
            var target = new GameObject("CombatTarget") { tag = "Target" };
            target.transform.position = Vector3.forward * 3;

            foreach (var manual in new[] { false, true })
            {
                movement.LockCameraToTarget(target.transform);
                Set(movement, "_targetSelector", selector);
                Set(movement, "_manualTargetCamera", manual);
                Set(selector, "_selectedTarget", target);
                Set(selector, "_isCasting", true);
                Set(movement.Input, "_rotate", true);
                movement.Input.LookInput(Vector2.one);

                typeof(ThirdPersonController).GetMethod("CameraRotation", _private).Invoke(movement, null);

                Require(!movement.LockCameraPosition, "RMB did not release camera focus.");
                Require((bool)Get(selector, "_isCasting"), "RMB cancelled the active cast.");
                Require((GameObject)Get(selector, "_selectedTarget") == target, "RMB cleared the selected combat target.");
            }

            var limit = TransformExtensions.MaxFacingAngle;

            foreach (var angle in new[] { 0f, 45f, 89f, 90f, 91f, limit - 1, limit - 0.01f, limit, limit + 0.01f, limit + 1, 180f,
                -89f, -90f, -91f, -limit + 1, -limit + 0.01f, -limit, -limit - 0.01f, -limit - 1 })
            {
                actor.transform.rotation = Quaternion.Euler(0, angle, 0);

                // Quaternion/vector conversion can put an exact mathematical boundary
                // on either side by float epsilon. Check its angle, then test both sides.
                if (Mathf.Abs(angle) == limit)
                {
                    var measuredAngle = Vector3.Angle(actor.transform.forward, target.transform.position - actor.transform.position);
                    Require(Mathf.Abs(measuredAngle - limit) < 0.001f, "Exact boundary angle drifted.");

                    continue;
                }

                Require(actor.transform.IsFacingTarget(target.transform) == (Mathf.Abs(angle) < limit),
                    $"Incorrect combat/gathering angle boundary at {angle} degrees.");
            }

            UnityEngine.Object.DestroyImmediate(movement.CinemachineCameraTarget);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(actor);
        }

        private static void ValidateParticles()
        {
            var prefab = Resources.Load<GameObject>("CombatHitEffect");
            Require(prefab != null, "Resources hit prefab missing.");

            // Exercise the same presentation method used by the RPC. In particular,
            // this must exist when the editor also has the dedicated-server symbol.
            var presentation = typeof(Health).GetMethod("PlayHitEffect", BindingFlags.Static | BindingFlags.NonPublic);
            Require(presentation != null, "Hit presentation was compiled out of the editor client.");

            var position = new Vector3(1234, 2, 1234);
            presentation.Invoke(null, new object[] { position, Vector3.forward });

            var effects = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            ParticleSystem spawned = null;

            foreach (var candidate in effects)
            {
                if (candidate.name == "CombatHitEffect(Clone)" && candidate.transform.position == position)
                {
                    spawned = candidate;
                    break;
                }
            }

            Require(spawned != null, "The RPC presentation did not create its hit effect.");

            var effect = spawned.gameObject;
            var particles = effect.GetComponent<ParticleSystem>();
            var material = particles.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            Require(effect.transform.parent == null, "Effect depends on the target lifetime.");
            Require(material != null && material.shader.name == "Universal Render Pipeline/Particles/Unlit", "Particle shader missing.");
            Require(material.GetTexture("_BaseMap") != null, "Droplet texture missing.");
            Require(!particles.main.loop && particles.main.stopAction == ParticleSystemStopAction.Destroy, "Effect does not clean itself up.");
            Require(particles.main.simulationSpace == ParticleSystemSimulationSpace.World, "Hit particles follow a reused target.");

            particles.Simulate(0.06f, true, true);
            Require(particles.particleCount > 0 && particles.particleCount <= 24, "Hit burst did not emit its bounded particles.");
            particles.Simulate(0.8f, true, true);
            Require(particles.particleCount == 0, "Hit particles outlived their short feedback window.");

            UnityEngine.Object.DestroyImmediate(effect);

            Debug.Log("HIT_PRESENTATION_VALIDATED: RPC presentation creates the detached burst in the editor client, including server-symbol editor compilation.");
        }

        // Run on a live frame in an empty validation scene so Move uses Unity's
        // actual delta time and CharacterController instead of an edit-mode zero delta.
        public static void RunMovement()
        {
            Require(Application.isPlaying && Time.deltaTime > 0, "Movement probe requires a live Play Mode frame.");

            var previousManager = NetworkManager.Singleton;
            var managerObject = new GameObject("MovementProbeNetwork");
            var manager = managerObject.AddComponent<NetworkManager>();
            manager.SetSingleton();

            var characters = UserManager.Instance.Characters;
            var clientId = manager.LocalClientId;
            var hadCharacter = characters.TryGetValue(clientId, out var previousCharacter);
            characters[clientId] = new CharacterDto();

            var root = new GameObject("MovementDirectionProbeRoot");
            new GameObject("MainCamera").transform.SetParent(root.transform);
            var followCamera = new GameObject("PlayerFollowCamera");
            followCamera.transform.SetParent(root.transform);
            followCamera.AddComponent<Cinemachine.CinemachineVirtualCamera>();

            var actor = new GameObject("MovementDirectionProbe");
            actor.transform.SetParent(root.transform);
            var movement = actor.AddComponent<ThirdPersonController>();
            movement.enabled = false;
            var controller = actor.GetComponent<CharacterController>();
            movement.Input = actor.AddComponent<StarterAssetsInputs>();
            movement.CinemachineCameraTarget = new GameObject("MovementProbeCamera");
            movement.SpeedChangeRate = 1000000;
            Set(movement, "_controller", controller);

            var target = new GameObject("MovementProbeTarget");
            var move = typeof(ThirdPersonController).GetMethod("Move", _private);
            var steer = typeof(ThirdPersonController).GetMethod("RotateCharacterWithCamera", _private);
            var origin = new Vector3(10000, 10000, 10000);

            try
            {
                foreach (var locked in new[] { false, true })
                {
                    foreach (var rmb in new[] { false, true })
                    {
                        foreach (var input in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right,
                            new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
                        {
                            controller.enabled = false;
                            actor.transform.SetPositionAndRotation(origin, Quaternion.Euler(0, 37, 0));
                            controller.enabled = true;
                            movement.Input.MoveInput(input);
                            movement.Input.SprintInput(!locked);
                            Set(movement.Input, "_rotate", rmb);
                            Set(movement, "_isOrbiting", !rmb);
                            Set(movement, "_cinemachineTargetYaw", 123f);
                            Set(movement, "_lockTarget", locked ? target.transform : null);
                            movement.LockCameraPosition = locked;
                            target.transform.position = origin + actor.transform.forward * 5;

                            steer.Invoke(movement, null);

                            var expectedYaw = rmb && !locked ? 123f : 37f;
                            var facing = Quaternion.Euler(0, expectedYaw, 0);
                            var expectedDirection = facing * new Vector3(input.x, 0, input.y).normalized;
                            move.Invoke(movement, new object[] { true });

                            var expectedSpeed = movement.LockSpeed;

                            if (!locked)
                            {
                                if (input.y > 0f)
                                {
                                    expectedSpeed = movement.SprintSpeed;
                                }
                                else if (input.y == 0f)
                                {
                                    expectedSpeed = movement.LockSpeed + (movement.SprintSpeed - movement.LockSpeed) * 0.2f;
                                }
                            }

                            Require(Mathf.Abs((float)Get(movement, "_speed") - expectedSpeed) < 0.002f,
                                $"Wrong movement speed: input={input}, locked={locked}, RMB={rmb}.");

                            var displacement = actor.transform.position - origin;
                            Require(displacement.sqrMagnitude > 0.0000001f, "Movement probe did not move the CharacterController.");
                            Require(Vector3.Angle(displacement, expectedDirection) < 1f,
                                $"Wrong signed movement: input={input}, locked={locked}, RMB={rmb}, actual={displacement}.");
                            Require(Mathf.Abs(Mathf.DeltaAngle(actor.transform.eulerAngles.y, expectedYaw)) < 0.01f,
                                $"Movement input changed facing: input={input}, locked={locked}, RMB={rmb}.");
                        }
                    }
                }

                Debug.Log("ACTOR_MOVEMENT_VALIDATED: W/S/A/D and four diagonals use signed actor axes; S backpedals, A/D strafe; target lock and RMB/LMB facing preserved.");
            }
            finally
            {
                if (hadCharacter)
                {
                    characters[clientId] = previousCharacter;
                }
                else
                {
                    characters.Remove(clientId);
                }

                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(movement.CinemachineCameraTarget);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(managerObject);
                previousManager?.SetSingleton();
            }
        }

        private static object Get(object target, string name) => target.GetType().GetField(name, _private).GetValue(target);

        private static void Set(object target, string name, object value) => target.GetType().GetField(name, _private).SetValue(target, value);

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
