# Duck player character

The main network player prefab uses a skinned duck. The legacy `Geometry` and humanoid `Skeleton` objects are removed as prefab overrides, not hidden. The inherited gameplay hierarchy, actor Animator, NetworkAnimator reference, collider, camera and network prefab GUID/local file ID are preserved. Existing profession wrappers and references remain; their geometry is replaced with duck-sized held models. Combat and profession presentation is driven by server-owned action snapshots.

## Runtime assets

`Client/Assets/Art/Characters/Duck/DuckCharacter.prefab` contains a `DuckVisual` transform with one `DuckMesh` SkinnedMeshRenderer and an eleven-bone rig. `DuckSkin.asset` has 23,615 vertices and 42,720 triangles, one submesh and normalized weights. 5,613 vertices blend across joints. The continuous torso surface includes belly, neck and head; separate feather, eye, bill and foot surfaces share the same skinned mesh. This is a generic transform rig, not a Humanoid avatar.

Rig: Body -> Neck -> Head; Body -> LeftWing/RightWing -> Tip; LeftFoot/RightFoot -> Toes. Neck, shoulder, wing-tip, ankle and toe regions deform with blended weights. Feet and wings retain their existing animation paths where possible. All five clips animate the rig; transforms are in place and the movement controller owns translation.

`DuckSkin.mat` uses one 1024x512 UV atlas (`DuckAtlas.asset`), with padded tiles and mipmaps. Materials and `Plumage.asset` used by the authoring generator are source inputs; the runtime renderer uses only the atlas material. Intermediate surface meshes exist only during editor generation. The previous separate part mesh assets were removed after checking references.

The model faces +Z. Skin bounds include tested jump/wing poses; offscreen skin updates are disabled. Mesh index format is UInt16. No runtime procedural mesh construction is required.

## Authoring and animation

`ProjectX > Character > Rebuild Duck` regenerates the skinned mesh, atlas, prefab and clips in place while retaining asset GUIDs, and updates the player prefab. Geometry/profile and weight authoring live in `DuckSkinAuthoring.cs`; animations and surface authoring are in `DuckCharacterBuilder.cs`.

Idle/Walk blend by Speed. Jump, Fall and Land use the existing Jump, FreeFall and Grounded parameters; MotionSpeed remains for the existing contract. Walking includes toe roll and wing-tip motion; jumping bends the neck and wing tips; landing compresses the body. Sprint currently uses the same gait.

## Verification

Unity import and editor compilation passed. Generated runtime C# projects compiled both without UNITY_SERVER and with UNITY_SERVER (zero errors; 36 existing warnings per build). The focused validator checks:
- one skin/material, normalized weights and normals, bone counts and bindings;
- 25 CPU-baked poses for each of five clips (125 total), nonzero deformation and containment within skin culling bounds;
- Animator transitions Locomotion -> Jump -> Fall -> Land -> Locomotion;
- the saved player prefab GUID/file ID, removal of legacy geometry, serialized references, skin bone ownership and NetworkAnimator reference.

Pose previews are written to ignored `Client/Logs/DuckPreview`. They render CPU-baked copies of each sampled skin pose so the editor preview loop cannot overwrite the pose with the live Animator controller.

These are isolated Unity editor checks, not authenticated gameplay. Authenticated movement, slopes, camera framing, multiplayer spawn and remote animation synchronization still require Play Mode/full-stack smoke. Authenticated combat/profession behavior and remote playback are not yet verified.

## Camera and stance regression (2026-10-15)

The camera controller previously searched only for removed `Geometry`, throwing in Start and then every HandleZoom update. Its serialized visual reference now points to DuckVisual; fallback resolves DuckVisual or legacy Geometry, and missing visuals log once without interrupting zoom. The prefab probe checks this binding and close/far visibility, including a missing visual.

Direct clip sampling had hidden another bug: Animator evaluation of the walking position curves reset unkeyed foot X/Z to zero. A new real-Animator regression reproduced both feet at `(0, 0.19, 0)` before the fix. Every clip now explicitly keys foot position XYZ (keeping the walking Y lift), preserving X=-0.22/+0.22 and Z=0.03 through blends. The validator runs sixty Animator walking updates and asserts the lateral stance. The original 125 baked-pose and state-transition checks still pass. Logs: `Client/Logs/duck-regression-before.log` and `duck-regression-after.log` (ignored). Authenticated gameplay verification remains pending.

## NPCs and tool cleanup (2026-10-17)

QuestNpc now uses an authored hen mesh; MerchantNpc uses a black/white magpie with a long teal tail. `NpcVisualCleanup.Build` migrates the model variants to standalone prefabs, preserves retained object/component IDs and verifies scene overrides, all eight quest NPCs, markers, colliders and the merchant's 47 offers. `BirdNpcAuthoring` writes combined static meshes and a shared palette material under `Assets/Art/Characters/Npc`. These NPC prefabs have no AssetHunts dependencies. Rebuild with `ProjectX > Character > Rebuild bird NPC visuals`; preview output is `Temp/npc-birds.png`.

The legacy Picaxe/FishingRod/Axe wrappers remain for existing gameplay references, but their imported model instances, renderers and colliders are removed. HeldModel is the authored replacement. Hammer, pickaxe and axe heads now rotate 90 degrees about the handle axis to strike in the wrist swing plane; the axe has a dedicated beveled blade mesh. The action validator checks this orientation and absence of legacy root geometry. Editor import, preview scene checks and 273 Animator poses passed; authenticated multiplayer and in-world target contact still need a live smoke test.

Five empty-equipment-slot icons are transparent monochrome outlines; original texture GUIDs remain unchanged. Generation prompts are recorded in `Automation/template-icons.md`.

## Combat and professions (2026-10-17)

Eyes and glints now conform to the actual head loft and use the same skin weights as its surface. They no longer intersect an approximate head ellipsoid or detach when the neck moves.

`DuckActionAuthoring` writes an Actions override layer with a transform mask for wings and neck. Clips cover SwordWindup/Strike, BowDraw/Release, WandCharge/Release, Mining, Fishing, Herbalism, Crafting, Lumberjack, Cooking and Alchemy. The base layer retains locomotion and feet. All action position channels and their locomotion defaults are explicit so releasing an action restores the rig, including the extended bow arm. Pose curves are editor-authored assets, not runtime mesh generation.

`DuckActionPresentation` replicates an action, server start time and duration, plus the equipped weapon category. Clients reconstruct phase from server time; the existing NetworkAnimator still handles locomotion. Combat begins/releases/cancels from TargetSelector's accepted server operations. Immediate auto-attack recasts preserve the preceding follow-through while keeping the original gameplay timer. Death, travel, equipment change and cancellation clear presentation as applicable. Mining/lumberjack/fishing reuse their existing server activity transitions. Herbalism/crafting request cosmetic states through an owner RPC with action allowlisting, range, target-tag and instance checks; loot, inventory changes and damage stay in their existing coordinators.

Nine held models are stored as prefabs and combined meshes: bow, sword, wand, pickaxe, fishing rod, sickle, hammer, axe and spoon. Runtime wrappers retain the existing `Picaxe`, `FishingRod` and `Axe` names; obsolete child geometry is removed during authoring. Models follow wing-tip grips in LateUpdate and are selected from replicated state. The equipped weapon returns after work. Camera visual hiding also hides props. Bow string/nock follows the other grip; fishing line updates after the rod, with a replicated bait reference allowing a late observer to resolve its endpoint.

Rebuild through `ProjectX > Character > Rebuild Duck`. For an already-open editor, writing `Client/Temp/duck-build.request` invokes the same builder in edit mode; `duck-build.status` records completion or the exception. It waits during Play Mode. After changing authoring scripts, let Unity compile them before requesting the final rebuild (a rebuild using the old assembly is not proof of the latest source).

Validation on 2026-10-17: Unity compilation and Netcode IL postprocessing passed; generated Assembly-CSharp builds passed with and without UNITY_SERVER (zero errors, 10 existing warnings each). The action probe evaluates 273 real-Animator poses across 13 clips, compares authored wing rotation, checks skin culling bounds, unchanged foot spacing, selected prop/grip attachment, bow string attachment, cancellation/reset, equipped-weapon mapping, close-camera visibility and snapshot serialization. The earlier 125 baked locomotion poses, transitions, skin/mesh, prefab and zoom checks also pass. Preview renders are in ignored `Client/Logs/DuckActions/`. These tests caught an inactive transform mask and an offscreen-culling issue in the isolated test setup; previews explicitly evaluate with AlwaysAnimate.

Remaining authenticated runtime smoke (not replaced by these probes):
1. On two clients, equip each weapon and attack, including uninterrupted repeated attacks, cancellation, target death and changing weapon while casting. Compare owner/observer phase and prop visibility.
2. Mine, harvest, craft at each station and fish. Verify targeting/turning, contact height, cancellation on movement/jump, and unchanged rewards/costs.
3. Join during active fishing and verify rod, bait and line; leave/rejoin and travel while actions are active. No old prop or line should remain.
4. Check close camera, slopes, jump/landing while equipped, remote culling/reappearance, and server-only gameplay. Final camera/gameplay aesthetics still need inspection in the live scenes.

## Quest bird variety (2026-10-17)

Quest NPC visuals now cycle Hen, Stork, Pigeon, Swan in stable serialized scene-instance order. The eight EnvironmentScene instances use two of each species. Each instance overrides only its visual MeshFilter mesh and visual child name; quest IDs, gameplay components, transforms and marker references are unchanged. The base QuestNpc remains a hen. The editor rebuild regenerates all five bird meshes against the shared ten-color palette, writes reusable visual prefabs for the four quest species, and reapplies the deterministic scene cycle. No runtime randomization or additional network state is used. Validation checks species counts, mesh bindings, colliders/markers and the merchant's existing offers. Unity preview/import passed; authenticated in-game interaction has not been rerun for this visual-only change.

## Slimmer player silhouette (2026-10-17)

DuckVisual has a fixed 0.9 uniform scale. The continuous torso is 15% narrower and roughly 10% shallower through the belly, with head cross-sections reduced about 7%. Eyes still conform to the same loft. The bill is shorter, shoulder pivots move from +/-0.37 to +/-0.32 in both locomotion and action clips, and wing surfaces are slimmer. All nine prop wrappers share the visual's 0.9 scale while their sockets follow the scaled bones. The actor/network transform remains unchanged. CharacterController is height 1.62, radius 0.28, center Y 0.81; camera target Y is 1.25. Rebuild uses absolute dimensions, so repeated authoring does not progressively shrink the player. Isolated validation checks the capsule, rig scale, pose deformation and tool/grip world-scale agreement. Live multiplayer and navigation against scene obstacles remain a separate smoke check.

## Bow and manual target camera regression (2026-10-18)

The network Arrow now hides its renderers until AbstractWeapon's server-written released NetworkVariable becomes true in Cast. The duck's nocked arrow remains the sole draw visual. Spawn applies the current snapshot (including late observers), despawn resets the pooled release state, and other weapon visuals remain unchanged. A focused editor probe applies false/true/false replicated snapshots and checks renderer visibility.

After an LMB drag, target camera auto-follow stays suspended until unlock or a different target is selected. The gameplay target remains bound for facing/interaction. Repeated focus on the same target does not overwrite the retained yaw/pitch. RMB unlocks camera focus; as of 2026-10-30 it preserves the combat target until normal facing/range validation fails. The regression checks release, a moving target and repeated focus without recentering.

Weapon range increased by 50% in the existing authoritative distance check: sword 4.5, wand 18, bow 27 Unity units. Unity import/Netcode processing, client compilation and focused camera/arrow/animation probes passed. These isolated checks do not replace authenticated two-client arrow lifecycle or physical mouse/mining/lumberjack smoke.

## Stationary RMB facing (2026-10-19)

Owner LateUpdate now aligns a stationary character to the updated camera yaw while RMB is held, after camera rotation/unlock processing. It leaves moving-character steering and LMB-only orbit intact. The child camera target's world rotation is preserved while rotating its actor, preventing a second yaw application. The focused regression checks RMB actor facing, unchanged world camera rotation, LMB preserving actor rotation and no position change. Physical pointer/full-stack verification remains pending.

## Gathering facing cancellation (2026-10-19)

Mining, Lumberjack and Herbalism interrupt through their existing cast-failure path when manual actor steering reaches the shared facing limit. Since 2026-10-31 this is 110 degrees: 90 degrees plus a 20-degree buffer. LMB-only camera orbit keeps the actor focused and does not interrupt. Initial automatic facing is allowed to settle while LockCameraPosition is active. Interrupts clear the local target, unlock camera focus and stop the existing replicated profession presentation. Completion RPCs also reject a target outside the shared IsFacingTarget cone before loot, EXP or hideout harvest. Combat uses that same strict <110-degree helper. This adds facing validation, not a full server-side profession timer redesign.

Client and UNITY_SERVER generated builds passed (10 warnings, zero errors each). A focused boundary probe covers front, +/-89, +/-90, +/-91 and rear; it was added but not run in this pass because the editor was in Play Mode. Live pointer cancellation and authenticated multiplayer reward checks remain pending. The authoring request was removed rather than scheduling a surprise rebuild when Play Mode ends.

## High-speed weapon movement (2026-10-20)

MoveTowardsTarget now uses Vector3.MoveTowards with a maximum Speed * deltaTime step, retaining optional horizontal-only movement. High-speed sword projectiles cannot repeatedly overshoot a target outside the hit radius. Client/server builds and the close-target Unity movement probe passed; live combat smoke remains pending.

## Combat steering and hit feedback (2026-10-30)

RMB steering releases camera focus without immediately clearing the combat target or failing its cast. Sword, bow and wand use the shared strict <110-degree facing check used before casting and by gathering: sideways 90-degree facing has a 20-degree buffer, while reaching 110 degrees away interrupts through the existing failure path. The limit is defined once by TransformExtensions.MaxFacingAngle and is also used by profession completion validation and diagnostics. Pressing RMB on the currently attacked target defers toggling until release: a click clears the target and stops casting; mouse travel of at least 4 pixels is treated as camera steering and preserves the cast. RMB on a target selected for viewing with LMB still starts attacking. Free movement uses actor-relative axes: S backpedals and A/D strafe without turning the actor. W and forward diagonals can use SprintSpeed. Backward movement and backward diagonals use LockSpeed; pure strafing uses LockSpeed plus 20% of the difference to SprintSpeed while sprint is enabled. Target-locked movement uses LockSpeed. Diagonals are normalized, RMB steers facing while moving or stationary, and LMB orbits without changing facing. Target-lock movement retains target-facing strafing until RMB takes control. Bow/sword casts allow movement within range and angle; the existing wand movement/jump restriction remains in force.

Positive server-accepted damage sends a hit effect to the target's observers before lethal pool release. `CombatHitEffect` is a detached, world-space ParticleSystem with 24 short-lived red droplets and automatic destruction. Its referenced URP particle material and texture are checked in. The presentation path is compiled for editor clients even when UNITY_SERVER is also defined; only standalone dedicated servers omit it. The previous !UNITY_SERVER-only guard suppressed the effect in that editor configuration. Rebuild through `ProjectX > Combat > Rebuild Hit Effect`; `ProjectX.Editor.CombatFeedbackValidation.Run` checks both camera steering branches, angle boundaries and particle configuration/lifetime.

Validation: client and UNITY_SERVER generated builds passed with zero errors; isolated Unity import/Netcode processing and the camera/angle/particle probe passed. A rendered preview confirmed the particle material and color. These checks do not validate physical input or multiplayer delivery.

The 100-degree buffer follow-up passed client/server compilation and both isolated editor facing probes, including sideways +/-90, +/-99 and +/-99.99 degrees inside the cone, +/-100.01 and +/-101 degrees outside, and rear facing. Nominal exact-boundary angles are checked with float tolerance because quaternion conversion can place them on either side by numerical epsilon. The existing player prefab, stationary steering and camera-orbit probes also passed. Authenticated A/D firing remains a manual smoke check.

Remaining authenticated gameplay checks:
0. With an active attack, click the current target again using RMB: release must clear the target, cast bar and pending server weapon. Repeat with RMB drag: target/cast must remain within the facing cone. A target selected only for viewing with LMB must still start attacking on RMB. Check release after losing focus or changing target cannot cancel a different target.
1. For sword, bow and wand, start a cast, hold RMB and turn in either direction. Cast progress must continue through 90 and 109 degrees; turning at least 110 degrees away must fail. Repeat after an LMB camera orbit and with the pointer over the selected target when pressing RMB. For bow/sword, also hold A/D to run sideways while firing within range. Repeat the 90/109/110-degree checks during Mining, Lumberjack and Herbalism, while stationary.
2. Check that LMB-only orbit, selecting another target, leaving range/line of sight and wand movement/jump retain their established behavior.
3. With two clients observing one enemy, verify one red burst per accepted hit, including killing blows and quickly reused pooled enemies. Misses/dodges and cancelled casts must not produce a hit burst; effects must disappear without accumulating objects.

Follow-up validation (2026-10-31): standalone client, standalone UNITY_SERVER, combined UNITY_SERVER/UNITY_EDITOR runtime and editor-tool compilation passed. After repairing the isolated project's asset database, Unity import/Netcode, the 110-degree camera/facing probe and Health's actual particle-presentation method passed. An empty-scene Play Mode probe exercised CharacterController.Move for 32 combinations of cardinal/diagonal input, target lock and RMB/LMB facing; all passed. The 24-particle prefab was regenerated with preserved asset identities. These isolated tests do not establish authenticated casting, physical mouse handling or remote/lethal RPC delivery; the gameplay checks above remain pending.

Speed/click follow-up: client/server compilation and isolated Unity import/Netcode passed. The 32-case movement probe now also checks actual computed speed: forward unlocked input, including forward diagonals, uses SprintSpeed; pure strafing uses LockSpeed plus 20% of the difference to SprintSpeed; backward input, backward diagonals and target-locked movement use LockSpeed. Physical RMB click-versus-drag and server cancellation still need the gameplay check above.
