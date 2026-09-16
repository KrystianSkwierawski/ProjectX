# Minimal dungeon instances

Research reviewed on 2026-09-15 concerns player-facing rules, not the proprietary server implementations.

## Reference games

- **Path of Exile 1:** entering an area reuses an available instance created by the player or a party member. Empty campaign instances normally survive about 8–15 minutes; map-device instances have longer lifetimes. This is the closest reference for individually entering one shared run. See [PoE Wiki: Area](https://www.poewiki.net/wiki/Area). Previously visited instances can remain accessible after leaving a party; [Game mechanics](https://www.poewiki.net/wiki/Game_mechanics) describes this, with historical developer confirmation in [Instance Stealing](https://www.pathofexile.com/forum/view-thread/1112727). The historical post is not a current official specification.
- **World of Warcraft:** groups use separate dungeon copies, with reset and lockout rules varying by mode. Those additional progression restrictions are outside this prototype. See [Instance](https://warcraft.wiki.gg/wiki/Instance) and [Instance reset](https://warcraft.wiki.gg/wiki/Instance_reset).
- **New World:** Expeditions combine instanced group content with optional cross-world matchmaking. The official Group Finder description includes creating a new instance for a matched group and reconnect/backfill handling. None of that matchmaking infrastructure is needed here. See [official Cross-World Expeditions description](https://www.newworld.com/en-us/news/articles/cross-world-expeditions-with-improved-group-finder).

## ProjectX decision

Use the basic PoE-style shared run, preserving the user's previously agreed reset and party rules:

1. First entrance creates a private solo or party run. Each member enters through the portal independently; subsequent entrants share the existing Bean and its state.
2. Party identity is independent of its leader. New members can join the ongoing run. Leaving the party returns that member outside; creating a party can adopt a solo run when the party has no existing run.
3. Killing the Bean completes the run without immediately ejecting anyone. Last departure or disconnect destroys it. The next entrance starts fresh.
4. Reconnect starts at the saved world return point. Temporary instance identity is not durable character state.
5. Portal interaction uses the existing pointer cursor, right-click, server range validation and LoadingScene scope.

Immediate empty-instance cleanup and removal on leaving party intentionally differ from PoE. They retain the explicit ProjectX requirements and avoid expiry timers and historical access lists. No instance chooser, map items, entry budgets, matchmaking or difficulty lockouts are introduced.

The client unloads its previous environment before loading the next one under the persistent LoadingScene scope. MainScene, UI and audio are session infrastructure; they stay loaded. EnvironmentScene includes NPC/crafting and server spawners previously split across TestScene and ServerScene. On the dedicated server, the world must remain available to other players while separate instance scenes hold dungeon environments.

`DungeonPortal.Destination` is a `LocationEnum` enum. The client sends that destination; the server finds the matching portal (one portal per destination in a location) in the current location/instance and validates range. There is no separate PortalId. The `Arrival` child Transform defines the exact standing position and yaw. Its blue Z axis faces away from the portal. No raycasts or automatic ground projection choose the destination. Server/client teleport and the saved world return pose include rotation; the camera heading is reset to match. The matching return portal determines entry into the destination. Additional dungeon locations use a corresponding `<Name>Scene`, a `Resources/<Name>EnvironmentPrefab` with `LocationEnvironment` and a baked `NavMeshSurface`. Runs are keyed by party and location.

The server owns in-memory room records and filters network visibility and gameplay interactions by instance. Travel has one ordered stage (`Idle`, `Saving`, `Loading`, `Arriving`) and guarded acknowledgements. Party reconciliation runs on party changes and after arrival, with a delayed retry only after failed persistence.

## Verification

Unity asset validation in an isolated project loaded the consolidated world and dungeon in turn, verified missing-script counts, shared portal destinations/arrival points and main-scene NPC/spawner presence. The dungeon NavMesh was baked from the authored prefab in Unity and a complete path to the Bean was verified after translating the room to its server offset. Floor collision normals are vertical. See dungeon-smoke.md for remaining authenticated multi-client scenarios.

An isolated Play Mode movement probe seeded approximately 5.98 units/s of controller velocity at x=10100, then verified idle movement, intentional input and input release. Horizontal drift was zero. This is not a two-client portal/party smoke test. Generated client/server compilation also passes.

TemplateScene (enum value 2) is a floor-only environment plus a return portal, with no enemies or walls. Its world entrance is adjacent to the dungeon entrance at (-1, 2.984, -3). TemplateEnvironmentPrefab and TemplateNavMesh.asset are included in both builds. Unity validation baked both environments and verified complete navigation routes. Arrival position/yaw is now explicit and independent of physics raycasts. Authenticated multi-client travel remains a separate smoke check.
