using System;
using UnityEditor;
using UnityEngine;

namespace ProjectX.Editor
{
    internal static class DuckSkinValidation
    {
        private const string Folder = "Assets/Art/Characters/Duck";

        public static void RemoveLegacyVisual(GameObject player, Transform actor)
        {
            var geometry = actor.Find("Geometry");
            var skeleton = actor.Find("Skeleton");
            foreach (var component in player.GetComponentsInChildren<Component>(true))
            {
                if (component == null || Inside(component.transform, geometry, skeleton))
                {
                    continue;
                }

                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    var target = property.objectReferenceValue;
                    var targetTransform = target is Component targetComponent ? targetComponent.transform
                        : target is GameObject targetObject ? targetObject.transform : null;
                    if (targetTransform != null && Inside(targetTransform, geometry, skeleton)
                        && !(component is Transform))
                    {
                        throw new InvalidOperationException($"Legacy visual is still referenced by {component.name}/{component.GetType().Name}.{property.propertyPath}");
                    }
                }
            }

            if (geometry != null)
            {
                UnityEngine.Object.DestroyImmediate(geometry.gameObject);
            }

            if (skeleton != null)
            {
                UnityEngine.Object.DestroyImmediate(skeleton.gameObject);
            }

            Debug.Log("[DuckSkin] Removed legacy Geometry and Skeleton after checking serialized references; actor components retained.");
        }

        private static bool Inside(Transform target, Transform geometry, Transform skeleton)
        {
            return geometry != null && target.IsChildOf(geometry) || skeleton != null && target.IsChildOf(skeleton);
        }

        public static void ValidatePlayer(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long fileId);
            Require(guid == "31287fd960052de4f954cad63c9ea181" && fileId == 2599698213384624033,
                "Player prefab identity changed; scene/network references would break.");
            var player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animator = player.GetComponentInChildren<Animator>(true);
                Require(animator != null && animator.runtimeAnimatorController != null, "Player Animator missing.");
                Require(animator.transform.Find("Geometry") == null && animator.transform.Find("Skeleton") == null,
                    "Legacy humanoid hierarchy remains.");
                var visual = animator.transform.Find("DuckVisual");
                Require(visual != null, "Duck visual missing.");
                Require(Vector3.Distance(visual.localScale, Vector3.one * .9f) < .001f, "Duck presentation scale changed.");
                var capsule = animator.GetComponent<CharacterController>();
                Require(Mathf.Abs(capsule.height - 1.62f) < .001f && Mathf.Abs(capsule.radius - .28f) < .001f
                    && Mathf.Abs(capsule.center.y - capsule.height * .5f) < .001f, "Duck collision capsule does not match its smaller stance.");

                ValidateZoom(animator, visual.gameObject);
                var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                Require(skin != null && skin.enabled && visual.GetComponentsInChildren<Renderer>().Length == 1,
                    "Player duck renderer is not correctly installed.");
                foreach (var bone in skin.bones)
                {
                    Require(bone != null && bone.IsChildOf(visual), "Player skin refers to an external bone.");
                }

                bool networkAnimatorFound = false;
                foreach (var component in player.GetComponentsInChildren<Component>(true))
                {
                    Require(component != null, "Missing player script.");
                    var serialized = new SerializedObject(component);
                    if (component.GetType().Name == "ClientNetworkAnimator")
                    {
                        networkAnimatorFound = true;
                        Require(serialized.FindProperty("m_Animator").objectReferenceValue == animator,
                            "NetworkAnimator no longer points at the actor Animator.");
                    }

                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            Require(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                                "Broken serialized reference: " + component.GetType().Name + "." + property.propertyPath);
                        }
                    }
                }

                Require(networkAnimatorFound, "Player NetworkAnimator missing.");
                Debug.Log("DUCK_PLAYER_VALIDATED: prefab GUID/fileID preserved, legacy geometry removed, skin bones and network Animator references intact.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        public static void Validate(GameObject model)
        {
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            Require(skins.Length == 1, "Expected one duck skinned renderer.");
            Require(model.GetComponentsInChildren<MeshRenderer>().Length == 0, "Rigid visual renderers remain.");
            var skin = skins[0];
            Require(skin.sharedMaterials.Length == 1, "Expected one atlas material.");
            Require(skin.sharedMesh.bindposes.Length == skin.bones.Length, "Bind pose count mismatch.");
            Require(skin.bones.Length == 11, "Expected 11 duck bones.");
            int blended = 0;
            foreach (var weight in skin.sharedMesh.boneWeights)
            {
                float sum = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;
                Require(Mathf.Abs(sum - 1) < .0001f, "Skin weights are not normalized.");
                Require(weight.boneIndex0 < skin.bones.Length && weight.boneIndex1 < skin.bones.Length
                    && weight.boneIndex2 < skin.bones.Length, "Skin bone index out of range.");
                if (weight.weight0 > .01f && weight.weight1 > .01f)
                {
                    blended++;
                }
            }

            Require(blended > 1000, "Expected smoothly blended joint regions.");
            var animator = model.GetComponent<Animator>();
            var baked = new Mesh();
            try
            {
                foreach (string name in new[] { "Idle", "Walk", "Jump", "Fall", "Land" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + name + ".anim");
                    Vector3[] initial = null;
                    float deformation = 0;
                    for (int frame = 0; frame <= 24; frame++)
                    {
                        animator.Rebind();
                        clip.SampleAnimation(model, clip.length * frame / 24);
                        skin.BakeMesh(baked);
                        var positions = baked.vertices;
                        if (initial == null)
                        {
                            initial = positions;
                        }

                        for (int i = 0; i < positions.Length; i++)
                        {
                            Require(!float.IsNaN(positions[i].x) && !float.IsInfinity(positions[i].x), "Non-finite skin pose.");
                            Require(skin.localBounds.Contains(positions[i]), "Animated vertex outside culling bounds: " + name);
                            deformation = Mathf.Max(deformation, (positions[i] - initial[i]).magnitude);
                        }
                    }

                    Require(deformation > .001f, "Animation does not deform the skin: " + name);
                    Debug.Log($"[DuckSkin] {name}: 25 baked poses, max displacement={deformation:F4}m, culling bounds valid.");
                }

                animator.Rebind();
                animator.SetBool("Grounded", true);
                animator.Play("Locomotion", 0, 0);
                animator.Update(.1f);
                animator.SetFloat("Speed", 3);
                for (int frame = 0; frame < 60; frame++)
                {
                    animator.Update(1f / 60);
                    var left = model.transform.Find("DuckVisual/LeftFoot").localPosition;
                    var right = model.transform.Find("DuckVisual/RightFoot").localPosition;
                    Require(Mathf.Abs(left.x + .22f) < .001f && Mathf.Abs(right.x - .22f) < .001f,
                        $"Animator collapsed foot spacing: left={left}, right={right}");
                }

                animator.SetBool("Jump", true);
                animator.SetBool("Grounded", false);
                Advance(animator, .2f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"), "Jump transition failed.");
                animator.SetBool("FreeFall", true);
                Advance(animator, .2f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Fall"), "Fall transition failed.");
                animator.SetBool("Jump", false);
                animator.SetBool("FreeFall", false);
                animator.SetBool("Grounded", true);
                Advance(animator, .14f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Land"), "Landing transition failed.");
                Advance(animator, .5f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), "Return to locomotion failed.");
                Debug.Log($"DUCK_SKIN_VALIDATED: blendedVertices={blended}; 125 baked poses; Jump/Fall/Land/Locomotion transitions passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void ValidateZoom(Animator animator, GameObject visual)
        {
            var movement = animator.GetComponent<StarterAssets.ThirdPersonController>();
            var type = movement.GetType();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var geometry = type.GetField("_geometry", flags);
            Require(geometry.GetValue(movement) as GameObject == visual, "Camera visual reference is not bound to DuckVisual.");
            var camera = animator.transform.parent.Find("PlayerFollowCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>();
            var follow = camera.GetCinemachineComponent<Cinemachine.Cinemachine3rdPersonFollow>();
            type.GetField("_thirdPersonFollow", flags).SetValue(movement, follow);
            var zoom = type.GetMethod("HandleZoom", flags);
            var distance = type.GetField("_currentZoom", flags);

            follow.CameraDistance = .3f;
            distance.SetValue(movement, .3f);
            zoom.Invoke(movement, new object[] { false });
            Require(!visual.activeSelf, "Close zoom did not hide the duck.");

            follow.CameraDistance = 2;
            distance.SetValue(movement, 2f);
            zoom.Invoke(movement, new object[] { false });
            Require(visual.activeSelf, "Zoom out did not restore the duck.");

            geometry.SetValue(movement, null);
            zoom.Invoke(movement, new object[] { false });
            geometry.SetValue(movement, visual);
            Debug.Log("DUCK_ZOOM_VALIDATED: bound visual, close/far visibility and missing visual without exception.");
            movement.Input = animator.GetComponent<StarterAssets.StarterAssetsInputs>();
            var target = new GameObject("CameraRegressionTarget");
            try
            {
                target.transform.position = new Vector3(5, 0, 5);
                movement.LockCameraToTarget(target.transform, -30);
                type.GetField("_manualTargetCamera", flags).SetValue(movement, true);
                type.GetField("_cinemachineTargetYaw", flags).SetValue(movement, 123f);
                type.GetField("_cinemachineTargetPitch", flags).SetValue(movement, 15f);
                movement.Input.LookInput(Vector2.zero);
                var rotate = type.GetMethod("CameraRotation", flags);
                rotate.Invoke(movement, null);
                var expected = movement.CinemachineCameraTarget.transform.rotation;

                target.transform.position = new Vector3(-5, 2, 0);
                movement.LockCameraToTarget(target.transform, -30);
                rotate.Invoke(movement, null);
                Require(Quaternion.Angle(expected, movement.CinemachineCameraTarget.transform.rotation) < .001f,
                    "Camera recentered after orbit release/repeated focus.");
                Require(type.GetField("_lockTarget", flags).GetValue(movement) == target.transform,
                    "Manual camera orbit lost the gameplay target.");
                movement.UnlockCamera();
                Require(!(bool)type.GetField("_manualTargetCamera", flags).GetValue(movement), "Manual camera latch survived unlock.");
                Debug.Log("TARGET_CAMERA_VALIDATED: released orbit holds yaw/pitch, repeated focus preserves it, gameplay target retained.");
                var rotateHeld = movement.Input.GetType().GetField("_rotate", flags);
                var faceCamera = type.GetMethod("RotateCharacterWithCamera", flags);
                var position = movement.transform.position;
                movement.Input.MoveInput(Vector2.zero);
                type.GetField("_cinemachineTargetYaw", flags).SetValue(movement, 137f);
                rotateHeld.SetValue(movement.Input, true);
                var retainedCameraRotation = movement.CinemachineCameraTarget.transform.rotation;
                faceCamera.Invoke(movement, null);
                Require(Mathf.Abs(Mathf.DeltaAngle(movement.transform.eulerAngles.y, 137f)) < .001f,
                    "Stationary RMB did not rotate the character to camera yaw.");
                Require(Quaternion.Angle(retainedCameraRotation, movement.CinemachineCameraTarget.transform.rotation) < .001f,
                    "Rotating the actor applied its yaw twice to the child camera target.");

                rotateHeld.SetValue(movement.Input, false);
                type.GetField("_isOrbiting", flags).SetValue(movement, true);
                type.GetField("_cinemachineTargetYaw", flags).SetValue(movement, 220f);
                faceCamera.Invoke(movement, null);
                Require(Mathf.Abs(Mathf.DeltaAngle(movement.transform.eulerAngles.y, 137f)) < .001f
                    && movement.transform.position == position, "LMB orbit rotated or moved the stationary character.");
                type.GetField("_isOrbiting", flags).SetValue(movement, false);
                Debug.Log("STATIONARY_CAMERA_VALIDATED: RMB faces camera yaw, LMB preserves actor rotation, position unchanged.");
                movement.transform.rotation = Quaternion.identity;
                var facingLimit = Assets.Scripts.Areas.Shared.Extensions.TransformExtensions.MaxFacingAngle;

                foreach (float angle in new[] { 0f, 89f, 90f, 91f, facingLimit - 1, facingLimit - 0.01f, facingLimit, facingLimit + 0.01f, facingLimit + 1, 180f,
                    -89f, -90f, -91f, -facingLimit + 1, -facingLimit + 0.01f, -facingLimit, -facingLimit - 0.01f, -facingLimit - 1 })
                {
                    var direction = Mathf.Abs(angle) == 90f ? Vector3.right * Mathf.Sign(angle)
                        : Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    target.transform.position = movement.transform.position + direction;

                    if (Mathf.Abs(angle) == facingLimit)
                    {
                        var measuredAngle = Vector3.Angle(movement.transform.forward, target.transform.position - movement.transform.position);
                        Require(Mathf.Abs(measuredAngle - facingLimit) < 0.001f, "Exact gathering boundary angle drifted.");

                        continue;
                    }

                    bool facing = Assets.Scripts.Areas.Shared.Extensions.TransformExtensions.IsFacingTarget(movement.transform, target.transform);
                    Require(facing == (Mathf.Abs(angle) < facingLimit), "Combat/gathering facing boundary failed at " + angle);
                }

                Debug.Log($"GATHERING_FACING_VALIDATED: common combat/gathering cone accepts sideways turns and rejects turns beyond {facingLimit} degrees and rear.");
                target.transform.position = movement.transform.position + new Vector3(0, 0, .1f);
                Assets.Scripts.Areas.Shared.Extensions.TransformExtensions.MoveTowardsTarget(movement.transform, target, true, 100f);
                Require(movement.transform.position.z <= target.transform.position.z + .0001f,
                    "Fast weapon movement overshot a nearby target.");
                Debug.Log("WEAPON_MOVEMENT_VALIDATED: close target cannot be overshot at sword speed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void Advance(Animator animator, float duration)
        {
            for (int i = 0; i < 20; i++)
            {
                animator.Update(duration / 20);
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
