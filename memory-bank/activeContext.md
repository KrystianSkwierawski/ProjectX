# Active Context

## Current Focus
- TemplateScene (LocationEnum value 2) has its own TemplateEnvironmentPrefab, a floor and a return portal, with no enemy or walls. A second world portal at (-1, 2.984, -3) leads there. Both instance types use the same party/travel code; enemy spawning is optional. Arrival transforms are restored in the shared portal and all instances; their position and yaw define the destination pose. Bake Template navigation through ProjectX > Bake Template Navigation. Unity validation passed for both baked routes; arrival pose validation covers explicit position and facing away from the portal; final client/server compilation also passed. Authenticated TemplateScene travel/party smoke remains outstanding.
- Locations: EnvironmentScene now contains the former TestScene NPC/crafting and ServerScene spawners; those scenes and their build/startup references were removed. Client travel unloads the previous environment before loading the destination, preserving Main/UI/audio/LoadingScene. The dedicated server retains the world for its occupants and creates separate instance scenes. NGO global scene synchronization is disabled; scene validation confirms environments contain no scene-owned NetworkObjects. Portals serialize only the destination LocationEnum; the server resolves the matching portal (one portal per destination in a location) in the current location and validates its range; instances are keyed by party and destination.
- DungeonEnvironmentPrefab is shared by DungeonScene and server instances, with a checked-in baked NavMeshSurface/DungeonNavMesh.asset. `ProjectX > Bake Dungeon Navigation` rebuilds it from authored prefab geometry. Arrival is an explicit child Transform beside the portal. Its position and yaw are applied on both server and client, and the camera is aligned to the new heading. Reconnect restores the persisted world return pose.
- Movement: idle input now forces zero horizontal speed instead of feeding collision velocity back into motion. An isolated Unity Play Mode probe at the dungeon offset seeded horizontal velocity, verified idle stability, intentional movement and stopping after input release: zero horizontal drift. This covers the movement regression, not a full authenticated multiplayer session.
- Verification: final generated client/server builds pass. Unity import/Netcode processing and isolated scene/prefab/NavMesh checks pass; the first fresh headless import logged unrelated rounded-corner UI shader validation errors. The licensing failure was specific to sandbox execution; Unity validation succeeds outside it. Two authenticated clients still need the transition/party scenarios in `Client/Automation/dungeon-smoke.md`. Previous API build/OpenAPI/format/SQL-model checks and 427 tests remain the backend baseline (no API changes in this follow-up).
- ActionBars and per-character settings are implemented: ten InventorySlot-based shortcuts (1–0), inventory drag assignment, right-click/key use, drag-out clearing, live stock counts, and responsive bottom-center placement. `CharacterSettings` persists language and bindings and restores them before client UI startup. Migration `20260912100007_AddCharacterSettings` backfills existing character defaults.

## ActionBars Verification
- API build/OpenAPI generation, formatting, SQL Server-configured EF pending-model check and all 413 backend tests pass. A newly started isolated in-memory API passed HTTP settings read/write/read, foreign-character 404, invalid-item 400 and anonymous 401 checks; this did not migrate the normal SQL database.
- Both generated Unity client/server-symbol compilations pass with existing warnings. Static prefab/scene reference, metadata, localization and five landscape/portrait layout-bound checks pass.
- Unity import now ran successfully in an isolated project outside the sandbox. Actual Play Mode drag/drop, keyboard use, counters after consumption, reconnect restoration and visual layout checks remain required. Compilation/static geometry do not replace these checks. Existing two-client trade smoke coverage remains outstanding.

## Previous Focus
- Player-to-player trade and its UI refinement are implemented across API and Unity, committed in separate localization, backend, Unity integration, and font-asset changes.
- Trade flow: friend invitation, bilateral offers, per-side lock/unlock, both-side confirmation, cancellation/disconnect cleanup, atomic capacity-aware commit, persistent idempotency receipt, and retry of genuinely uncertain commits with one immutable trade GUID/payload. The same API transaction resynchronizes active Collect quests from both post-trade inventories, including for a participant who disconnects during commit. Quest completion and Collect acceptance/progress join the inventory-mutation barrier, so commit waits for already-started inventory-dependent quest writes while locked state blocks new ones. Quest completion sends only the character-quest ID and uses the quest ID returned by the authenticated API. Persistence uses the server lifetime after entering the barrier, while RPC/UI effects remain guarded by the participant's network-spawn lifetime; despawn therefore cannot release a tracker before the API request settles. A server-lifetime coordinator owns commit/retry across participant despawns and cancels only on server shutdown. A disconnected participant's delegated API credential remains valid until an in-flight commit resolves; after an uncertain response, a server-authorized receipt lookup resolves success even when a later retry loses its player session.
- Current TradeUI uses a preferred 720x440 session and 460x192 invitation on a height-matched 1920x900 Canvas; LogMessage uses 28-point wrapping text.
- Offers start empty and populate lazily with `TradeOfferSlot`. Their fields align with the title/status inset; items render left-to-right in exactly four columns with equal side/top/bottom padding, smaller inset artwork, responsive square cells, vertical scrolling, and left-aligned partial rows.
- TradeUI detects an actual overlap with active LootUI and fits itself into the space to its right when enough room exists. If that side is too narrow, it chooses the best-scaled non-overlapping area from the four sides while preserving Inventory and Log avoidance rules.
- `TradeInventoryProjection` hides active/pending offered quantities from the clicked inventory stack without changing authoritative inventory data. Add rejection, withdrawal, cancellation, and session closure restore the item. Own offer supports click/right-click and drag/drop return to inventory.
- Opening trade hides Crafting/Quest/Merchant/Gear/Character. Opening one of those during trade requests cancellation and defers the target panel until an authoritative empty trade state; committing trades cannot be cancelled.
- A successful API response or durable receipt lookup carries authoritative snapshots of both inventories. The Unity server routes them by authenticated character ID rather than response position. Each client clears the offer projection before replacing local inventory, then performs one authoritative GET as a post-commit reconciliation; local state versioning prevents older asynchronous loads from winning. This fixes the observed case where the API committed correctly but a client kept showing its pre-trade inventory until reconnect.
- Tracked inventory persistence passes a server-lifetime cancellation token through the HTTP request and quest-progress continuation, while its RPC/UI response remains guarded by the originating network-spawn token. A quest-completion concurrency loss now refreshes inventory and shows a quest-specific message rather than a trade error.

## Current Verification
- Follow-up on the still-visible stale trade inventory found a prefab wiring bug: `Trade` is on the player root while `CharacterInventory` is on a child. Completion used `GetComponent` and silently skipped both snapshot application and reconciliation through null-conditional calls. It now uses `GetComponentInChildren(includeInactive: true)` and reports a missing component explicitly. Both generated compile variants pass; two-client runtime confirmation is still required. Earlier snapshot routing/reconciliation alone did not fix this wiring issue.
- Trade backend: API build, OpenAPI generation, formatting, EF pending-model check, and all 386 backend tests passed. Receipt resolution has applied/not-found integration coverage; atomic post-trade Collect progress has two-character integration coverage; the resolver has an OpenAPI contract check.
- Unity: import and Netcode IL processing passed; generated client and `UNITY_SERVER` builds pass with zero errors and existing dependency-conflict warnings. The latest post-trade reconciliation, character-ID snapshot routing, quest-conflict refresh, and inventory operation-lifetime fixes pass both generated-project compile variants directly; localization JSON also parses. The preceding spacing/Loot-avoidance refinement passed static prefab geometry checks.
- Trade UI: 20 generated cases across 1900x650, 1600x900, 1024x768, 720x1280, and 480x1280 passed projection, rollback/restore, drag guards, four-column geometry, symmetric padding, non-overlap, and log wrapping checks.
- Authenticated two-client Play Mode trade remains untested. Generated checks do not replace live pointer/network verification.

## Active Decisions
- Prioritize correct programmer-owned systems/mechanics over art polish and economic balancing.
- Keep code simple, readable, and consistent with repository conventions; avoid unnecessary abstractions.
- Treat Unity dedicated server as gameplay authority and API as durable transactional authority.
- Preserve exact inventory positions and the atomic `1024` max-stack/capacity rules.
- Keep the current maximum of six unique offered item types as a review/abuse bound, not as a technical inventory limit.
- Keep current character stats as persisted mutable totals until the base-versus-gear model is clarified.
- Keep ammo consumption/zero-count unequip on Unity server; API persists submitted state.
- Keep temporary first-character selection isolated; do not schedule a selector unless requested.
- Keep English client `pl.json` as the intentional development fallback until complete Polish localization is requested.
- Keep Animator root motion disabled unless a root-motion movement model is explicitly requested.
- Repository instructions and task-dependent documentation routing live in root `AGENTS.md`.
- Prefer code, builds, tests, logs, and generated previews. Use `computer-use` only when direct UI interaction is necessary or materially simplifies implementation or verification.
- Preserve user/unrelated working-tree changes. New/moved Unity assets require their `.meta` files.

## Next Steps
1. Run an authenticated two-client trade smoke test: invite/accept/decline/cancel, offer add/remove by click and drag, pending visual reservation/rollback, lock/unlock, both confirmations, disconnect, inventory-full/max-stack success and failure, and reconnect persistence.
2. Smoke-test the broader Inventory/Gear/Loot/Merchant drag lifecycle and ammo final-unit persistence in Unity Play Mode.
3. Add centralized Escape/UI-close behavior, visible quest rewards, and Gear Score.
4. Revisit Party reward radius/EXP split after playtesting, then implement Guilds and Auction House.
5. Before production scaling, replace in-memory session/ticket state with a shared atomic store and complete Relay/UGS linkage/runtime validation.

## Important Local Notes
- Development API startup intentionally deletes/recreates the database; be cautious with local data.
- Git status emits a known warning about inaccessible `C:/Users/pc/.config/git/ignore`.
- Update both `Client/Automation/run.ps1` and `Client/Assets/Editor/ProjectXDevAutomation.cs` when startup/build behavior changes.
- This Memory Bank was compacted on 2026-09-08; detailed chronology remains in Git history rather than repeated documentation.

- Spawner now supports one-shot populations via Maintain Population=false. Dungeon uses prefab spawners for one Bean and one chamomile; Template retains LocationEnvironment for navigation/portal lookup. Client/server compilation and isolated Unity asset/import/Arrival validation passed. Live kill/mine/re-entry and two-client party-isolation smoke remain pending.

- Enemy patrol now initializes on every network spawn and clears agent/subscription state on despawn. Weapons return to their owning pool after impact or invalid target and are cleaned up with the owner; duplicate pending cast requests are ignored. Idle TargetSelector no longer hides profession progress/failure bars; UIScene bar is anchored above the hotbar. Client/server compilation and isolated Play Mode probes verified initial/reused patrol and invalid-target Fireball/Arrow release; authenticated two-client combat/portal smoke is still pending.

- Portal rejection for persisted 0 HP is intentional; it now has a dedicated localized message. Rejection logs distinguish session, character health, missing portal, distance and active trade. No automatic resurrection or health reset was added.

- Location loading now cancels Mining, Lumberjack, Herbalism, Fishing and Crafting before unloading the environment; profession input is blocked during travel. Gathering handles missing targets, fishing release is idempotent and old bite animations cannot mutate a reused bait. Full authenticated portal-during-gathering/fishing smoke remains pending.
