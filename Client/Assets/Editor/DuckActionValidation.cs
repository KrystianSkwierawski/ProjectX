using System;
using System.IO;
using System.Reflection;
using Assets.Scripts.Areas.Character.Mono;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace ProjectX.Editor
{
    internal static class DuckActionValidation
    {
        public static void Run()
        {
            const string folder = "Assets/Art/Characters/Duck/";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Character/NestedParentArmature_Unpack Variant.prefab");
            var model = UnityEngine.Object.Instantiate(prefab);
            var selector = model.GetComponentInChildren<TargetSelector>(true);
            var arrowPrefab = new UnityEditor.SerializedObject(selector).FindProperty("_arrowPrefab").objectReferenceValue as GameObject;
            var projectile = UnityEngine.Object.Instantiate(arrowPrefab);
            try
            {
                const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
                var arrow = projectile.GetComponent<Arrow>();
                var released = (NetworkVariable<bool>)typeof(AbstractWeapon).GetField("_released", privateInstance).GetValue(arrow);
                foreach (bool visible in new[] { false, true, false })
                {
                    using (var writer = new FastBufferWriter(8, Allocator.Temp))
                    {
                        writer.WriteValueSafe(visible);
                        using (var reader = new FastBufferReader(writer, Allocator.Temp))
                        {
                            released.ReadField(reader);
                        }
                    }

                    typeof(AbstractWeapon).GetMethod("ApplyProjectileVisibility", privateInstance).Invoke(arrow, null);
                    foreach (var renderer in projectile.GetComponentsInChildren<Renderer>(true))
                    {
                        Require(renderer.enabled == visible, "Arrow visible before release or hidden in flight.");
                    }
                }

                Debug.Log("ARROW_VISIBILITY_VALIDATED: casting hidden, released visible, reset hidden; replicated snapshots applied.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectile);
            }

            foreach (var camera in model.GetComponentsInChildren<Camera>(true))
            {
                camera.gameObject.SetActive(false);
            }

            var animator = model.GetComponentInChildren<Animator>(true);
            animator.transform.position = Vector3.zero;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var presentation = animator.GetComponent<DuckActionPresentation>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(DuckActionPresentation);
            int layer = animator.GetLayerIndex("Actions");
            Require(layer > 0, "Actions layer missing.");
            type.GetField("_layer", flags).SetValue(presentation, layer);
            var apply = type.GetMethod("Apply", flags);
            var props = (GameObject[])type.GetField("_props", flags).GetValue(presentation);
            foreach (int index in new[] { 3, 6, 7 })
            {
                var held = props[index].transform.Find("HeldModel");
                Require(held != null && Vector3.Dot(held.localRotation * Vector3.right, Vector3.forward) > .999f,
                    "Tool working head is outside its swing plane: " + props[index].name);
                Require(props[index].GetComponent<MeshFilter>() == null
                    && props[index].GetComponent<Renderer>() == null, "Legacy tool geometry remains.");
            }

            var skin = animator.GetComponentInChildren<SkinnedMeshRenderer>();
            var preview = new PreviewRenderUtility();
            preview.AddSingleGO(model);
            preview.camera.transform.position = new Vector3(2.8f, 1.8f, 4.8f);
            preview.camera.transform.LookAt(new Vector3(0, .85f, 0));
            preview.camera.fieldOfView = 31;
            preview.camera.nearClipPlane = .05f;
            preview.camera.farClipPlane = 30;
            preview.camera.backgroundColor = new Color(.12f, .18f, .22f);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.lights[0].intensity = 1.4f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35, -30, 0);
            preview.lights[1].intensity = .8f;
            preview.ambientColor = Color.gray;
            Directory.CreateDirectory("Logs/DuckActions");
            var baked = new Mesh();
            var reference = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "DuckCharacter.prefab"));
            reference.SetActive(false);

            try
            {
                foreach (DuckActionEnum action in Enum.GetValues(typeof(DuckActionEnum)))
                {
                    if (action == DuckActionEnum.None)
                    {
                        continue;
                    }

                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + action + ".anim");
                    Require(clip != null && clip.length > 0, "Missing action clip: " + action);
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        Require(animator.transform.Find(binding.path) != null, "Missing action bone: " + binding.path);
                    }

                    var state = new DuckActionState { Action = action, Started = 10, Duration = clip.length };
                    using (var writer = new FastBufferWriter(64, Allocator.Temp))
                    {
                        writer.WriteNetworkSerializable(state);
                        using (var reader = new FastBufferReader(writer, Allocator.Temp))
                        {
                            reader.ReadNetworkSerializable(out DuckActionState restored);
                            Require(state.Equals(restored), "Network action serialization changed the pose time.");
                        }
                    }

                    for (int frame = 0; frame <= 20; frame++)
                    {
                        animator.Rebind();
                        animator.SetBool("Grounded", true);
                        animator.SetFloat("Speed", 3);
                        animator.Play("Locomotion", 0, frame / 20f);
                        animator.Update(0);
                        double now = state.Started + clip.length * frame / 20;
                        apply.Invoke(presentation, new object[] { state, now, 1f });
                        animator.Update(0);
                        apply.Invoke(presentation, new object[] { state, now, 0f });
                        clip.SampleAnimation(reference, clip.length * frame / 20);
                        var expectedWing = reference.transform.Find("DuckVisual/Body/RightWing").localRotation;
                        Require(Quaternion.Angle(expectedWing, animator.transform.Find("DuckVisual/Body/RightWing").localRotation) < 1,
                            "Action layer did not evaluate authored pose: " + action + ", frame=" + frame + ", expected=" + expectedWing.eulerAngles + ", actual=" + animator.transform.Find("DuckVisual/Body/RightWing").localEulerAngles + ", weight=" + animator.GetLayerWeight(layer));
                        skin.BakeMesh(baked);
                        Require(baked.bounds.size.sqrMagnitude < 12, "Exploded action skin: " + action);
                        foreach (var vertex in baked.vertices)
                        {
                            Require(skin.localBounds.Contains(vertex), "Action skin outside culling bounds: " + action);
                        }
                        Require(Mathf.Abs(animator.transform.Find("DuckVisual/LeftFoot").localPosition.x + .22f) < .001f,
                            "Action mask overwrote locomotion stance: " + action);
                        int expected = DuckActionPresentation.PropIndex(action, WeaponCategoryEnum.None);
                        for (int prop = 0; prop < props.Length; prop++)
                        {
                            Require(props[prop] != null && props[prop].activeSelf == (prop == expected), "Wrong prop visibility: " + action);
                        }

                        var grip = (Transform)type.GetField(expected == 0 ? "_leftGrip" : "_rightGrip", flags).GetValue(presentation);
                        Require(Vector3.Distance(props[expected].transform.position, grip.position) < .0001f, "Prop detached from grip: " + action);
                        Require(Vector3.Distance(props[expected].transform.lossyScale, grip.lossyScale) < .001f,
                            "Tool scale no longer matches the smaller rig: " + action);
                        if (action == DuckActionEnum.BowDraw)
                        {
                            var line = (LineRenderer)type.GetField("_bowString", flags).GetValue(presentation);
                            var hand = (Transform)type.GetField("_rightGrip", flags).GetValue(presentation);
                            Require(Vector3.Distance(line.transform.TransformPoint(line.GetPosition(1)), hand.position) < .0001f,
                                "Bow string detached from drawing hand.");
                        }
                    }

                    // A baked pose prevents editor repaint from evaluating the controller again.
                    apply.Invoke(presentation, new object[] { state, state.Started + clip.length * .5, 1f });
                    animator.Update(0);
                    apply.Invoke(presentation, new object[] { state, state.Started + clip.length * .5, 0f });
                    skin.BakeMesh(baked);
                    var frozen = new GameObject("FrozenPose");
                    frozen.transform.SetParent(skin.transform, false);
                    frozen.AddComponent<MeshFilter>().sharedMesh = baked;
                    frozen.AddComponent<MeshRenderer>().sharedMaterial = skin.sharedMaterial;
                    skin.enabled = false;
                    preview.BeginStaticPreview(new Rect(0, 0, 768, 768));
                    preview.Render(true);
                    var image = preview.EndStaticPreview();
                    File.WriteAllBytes("Logs/DuckActions/" + action + ".png", image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                    UnityEngine.Object.DestroyImmediate(frozen);
                    skin.enabled = true;

                    apply.Invoke(presentation, new object[] { default(DuckActionState), 20d, 1f });
                    animator.Update(0);
                    Require(Vector3.Distance(animator.transform.Find("DuckVisual/Body/LeftWing/Tip").localPosition,
                        new Vector3(0, -.25f, -.04f)) < .001f, "Action left a stretched wing after cancellation: " + action);
                }

                apply.Invoke(presentation, new object[] { default(DuckActionState), 20d, 1f });
                Require(animator.GetLayerWeight(layer) == 0, "Action did not return to locomotion.");
                foreach (var prop in props)
                {
                    Require(!prop.activeSelf, "Prop remained visible after cancellation.");
                }

                Require(DuckActionPresentation.PropIndex(DuckActionEnum.None, WeaponCategoryEnum.Bow) == 0
                    && DuckActionPresentation.PropIndex(DuckActionEnum.None, WeaponCategoryEnum.Sword) == 1
                    && DuckActionPresentation.PropIndex(DuckActionEnum.None, WeaponCategoryEnum.Wand) == 2,
                    "Equipped weapon was not restored after work.");

                var visual = (Transform)type.GetField("_visual", flags).GetValue(presentation);
                visual.gameObject.SetActive(false);
                apply.Invoke(presentation, new object[] { new DuckActionState { Action = DuckActionEnum.Mining, Started = 10, Duration = 3 }, 11d, 1f });
                foreach (var prop in props)
                {
                    Require(!prop.activeSelf, "Close-camera hiding left a floating tool.");
                }

                visual.gameObject.SetActive(true);

                Debug.Log("DUCK_ACTIONS_VALIDATED: 13 actions, 273 Animator poses, network snapshot serialization, locomotion masking, prop selection/grips, cancellation.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(reference);
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
