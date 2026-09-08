# Progress

## Current Status
- Authentication is hardened with Identity lockout/rate limiting, fail-closed JWT authorization, one-hour access tokens, a signed 24-hour session ceiling, bounded refresh, client logout recovery, and dedicated-server supervised exit.
- Direct/Relay admission uses server registration/lease, one-time connection tickets, NGO approval, and server-only `PlayerSessionId` delegation; client JWTs never cross Netcode.
- Character health/max health, percentage-based combat stats, weapon-specific scaling, code-driven movement, gear, compatible tiered ammo, per-hit ammo consumption, and health/Strength/Speed potions are implemented.
- Inventory supports stable positioned slots, 1024 max stacks, atomic capacity checks, split/move/merge/swap, Inventory/Gear drag/drop, Loot pickup, Merchant buy/sell, and shared previews/source restoration.
- Quests support additive Kill and absolute inventory-backed Collect progress, serialized mutation ordering, completion, and Finished-to-Accepted regression. Crafting supports ordered recipes and client interruption rules.
- Persistent Friends support invitations, online/level state, removal, exact-name whispers, bounded mutations, and lifecycle-safe refresh. Ephemeral Parties support leadership, invitations, live roster state, disconnect cleanup, and reward sharing.
- Player trade is implemented end to end in the current working tree: one invitation/session per player, bilateral offers, lock/confirm, inventory reservation, atomic two-inventory commit, optimistic concurrency, persistent idempotency receipt, cancellation, and disconnect cleanup.
- Trade UI is compact/responsive, non-overlapping, four-column/scrollable, and visually reserves offered stacks. LogUI wraps full messages; conflicting panels close/defer correctly.
- Unity UI generally uses scene-backed prefabs, responsive anchors/layout, declarative static translation, shared hover/drag behavior, global loading scopes, Quick Access, Friend/Party panels, and Buff timers.
- Backend uses .NET 10/C# 14 Clean Architecture boundaries, responsibility-aligned tests, global ProblemDetails mapping, MediatR logging, Development-only reset/seeding, UTC `DateTimeOffset`, and checked-in NSwag output.

## Latest Verification Baseline
- Backend trade baseline: build/OpenAPI/format/EF validation passed; 383 tests passed.
- Unity trade baseline: editor import/Netcode processing and generated client/server-symbol builds passed with zero errors; seven existing warnings remain.
- Trade UI refinement: 20 generated preview/interaction cases passed across ultrawide, landscape, 4:3, portrait, and narrow screens.
- JSON/localization and Unity `.meta`/prefab references were validated during the feature work.

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
