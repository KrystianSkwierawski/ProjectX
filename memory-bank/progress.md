# Progress

## Current Status
- ActionBars persists ten item-type bindings plus language in per-character settings. Scene-authored InventorySlot variants support inventory drag assignment, 1–0/right-click use, drag-out clearing and current stock counts. Backend checks and 413 tests pass, as do generated Unity builds and static asset/layout checks. Live API settings smoke passes against an isolated in-memory instance; Unity import/Play Mode remains blocked by the installed editor's missing license (see activeContext.md).
- Authentication is hardened with Identity lockout/rate limiting, fail-closed JWT authorization, one-hour access tokens, a signed 24-hour session ceiling, bounded refresh, client logout recovery, and dedicated-server supervised exit.
- Direct/Relay admission uses server registration/lease, one-time connection tickets, NGO approval, and server-only `PlayerSessionId` delegation; client JWTs never cross Netcode.
- Character health/max health, percentage-based combat stats, weapon-specific scaling, code-driven movement, gear, compatible tiered ammo, per-hit ammo consumption, and health/Strength/Speed potions are implemented.
- Inventory supports stable positioned slots, 1024 max stacks, atomic capacity checks, split/move/merge/swap, Inventory/Gear drag/drop, Loot pickup, Merchant buy/sell, and shared previews/source restoration.
- Quests support additive Kill and absolute inventory-backed Collect progress, serialized mutation ordering, completion, and Finished-to-Accepted regression. Crafting supports ordered recipes and client interruption rules.
- Persistent Friends support invitations, online/level state, removal, exact-name whispers, bounded mutations, and lifecycle-safe refresh. Ephemeral Parties support leadership, invitations, live roster state, disconnect cleanup, and reward sharing.
- Player trade is implemented end to end in the current working tree: one invitation/session per player, bilateral offers, lock/confirm, inventory reservation, draining of pre-lock inventory and inventory-dependent quest writes, atomic two-inventory plus active Collect-progress commit, optimistic concurrency, persistent idempotency receipt, cancellation, and disconnect cleanup. Quest completion is tracked conservatively and resolves its quest type from the authenticated API response instead of client input. Tracked persistence survives participant despawn until its API request settles, while player-facing continuations remain spawn-lifetime guarded. A server-lifetime coordinator owns an accepted commit across player despawns and stops it on server shutdown. Uncertain failures keep the immutable idempotent retry and can resolve the durable receipt without an ephemeral player session. Successful commits route version-protected authoritative snapshots by authenticated character ID, clear offer projection before applying them, and reconcile with one post-commit GET so stale client inventory cannot survive a correct API commit; disconnected participants retain correct persisted Collect progress.
- Trade UI is compact/responsive, avoids Inventory/Loot/Log collisions with a best-side fallback, aligns offer fields with their labels, keeps four padded icon columns, scrolls, and visually reserves offered stacks. LogUI wraps full messages; conflicting panels close/defer correctly.
- Unity UI generally uses scene-backed prefabs, responsive anchors/layout, declarative static translation, shared hover/drag behavior, global loading scopes, Quick Access, Friend/Party panels, and Buff timers.
- Backend uses .NET 10/C# 14 Clean Architecture boundaries, responsibility-aligned tests, global ProblemDetails mapping, MediatR logging, Development-only reset/seeding, UTC `DateTimeOffset`, and checked-in NSwag output.

## Latest Verification Baseline
- Backend trade baseline: build/OpenAPI/format/EF validation passed; all 386 tests passed after durable receipt-resolution and atomic post-trade Collect-progress coverage were added.
- Unity trade baseline: editor import/Netcode processing and generated client/server-symbol builds passed with zero errors; the latest mutation-drain, versioned snapshot, and receipt-resolution changes pass both generated-project variants. Seven existing warnings remain.
- Trade UI refinement: 20 generated preview/interaction cases passed across ultrawide, landscape, 4:3, portrait, and narrow screens.
- The later offer-spacing and Loot-collision refinement passed client and `UNITY_SERVER` generated-project builds plus static prefab geometry assertions. The subsequent commit-failure and narrow Loot fallback fixes also pass both compile variants; they still need live runtime confirmation.
- JSON/localization and Unity `.meta`/prefab references were validated during the feature work.
- The latest post-trade client reconciliation, quest-conflict refresh, and server-lifetime inventory-request fixes pass the generated Unity client and `UNITY_SERVER` compile variants directly with no errors; only seven existing Unity dependency-conflict/unused-field warnings remain. All four mirrored localization JSON files parse. The preceding backend baseline remains all 386 tests passing.

## Remaining Verification And Known Gaps
- Run a live authenticated two-client trade smoke test, especially simultaneous state changes, drag/drop, disconnects, inventory full, max-stack merging, commit retry, and reconnect persistence.
- Run Play Mode pointer tests for Inventory/Gear/Loot/Merchant drag/drop, hover clamping, ammo final-unit removal/UI refresh, buff timing, health text, and crafting interruptions.
- Complete live Relay validation after UGS project/build-profile linkage; current local Direct mode is validated more thoroughly.
- Run a long-duration JWT refresh/session-deadline soak test.
- Crafting still needs authoritative server-side recipe/material/station validation before hostile clients are fully handled.
- In-memory game-session/ticket state prevents safe multi-instance API scaling.

## Planned Work
- Centralized Escape/UI-close policy.
- Visible item/experience quest rewards.
- Gear Score.
- Party reward balancing/configuration after playtesting.
- Guilds, followed by Auction House once ownership/transaction rules remain stable.
- Full Polish client localization when explicitly prioritized.

## Documentation State
- Memory Bank compacted on 2026-09-08. It records current behavior, decisions, invariants, verification, and gaps; detailed chronological implementation history is intentionally left to Git.
