# Active Context

## Current Focus
- Player-to-player trade and its UI refinement are implemented in the current uncommitted working tree across API and Unity.
- Trade flow: friend invitation, bilateral offers, per-side lock/unlock, both-side confirmation, cancellation/disconnect cleanup, atomic capacity-aware commit, persistent idempotency receipt, and retry of uncertain commits with one immutable trade GUID/payload.
- Current TradeUI uses a preferred 720x440 session and 460x192 invitation on a height-matched 1920x900 Canvas; LogMessage uses 28-point wrapping text.
- Offers start empty and populate lazily with `TradeOfferSlot`. They render left-to-right in exactly four columns with equal side/top/bottom padding, responsive square cells, vertical scrolling, and left-aligned partial rows.
- `TradeInventoryProjection` hides active/pending offered quantities from the clicked inventory stack without changing authoritative inventory data. Add rejection, withdrawal, cancellation, and session closure restore the item. Own offer supports click/right-click and drag/drop return to inventory.
- Opening trade hides Crafting/Quest/Merchant/Gear/Character. Opening one of those during trade requests cancellation and defers the target panel until an authoritative empty trade state; committing trades cannot be cancelled.

## Current Verification
- Trade backend: API build, OpenAPI generation, formatting, EF pending-model check, and all 383 backend tests passed.
- Unity: import and Netcode IL processing passed; generated client and `UNITY_SERVER` builds pass with zero errors and seven existing warnings.
- Trade UI: 20 generated cases across 1900x650, 1600x900, 1024x768, 720x1280, and 480x1280 passed projection, rollback/restore, drag guards, four-column geometry, symmetric padding, non-overlap, and log wrapping checks.
- Authenticated two-client Play Mode trade remains untested. Generated checks do not replace live pointer/network verification.

## Active Decisions
- Prioritize correct programmer-owned systems/mechanics over art polish and economic balancing.
- Keep code simple, readable, and consistent with repository conventions; avoid unnecessary abstractions.
- Treat Unity dedicated server as gameplay authority and API as durable transactional authority.
- Preserve exact inventory positions and the atomic `1024` max-stack/capacity rules.
- Keep current character stats as persisted mutable totals until the base-versus-gear model is clarified.
- Keep ammo consumption/zero-count unequip on Unity server; API persists submitted state.
- Keep temporary first-character selection isolated; do not schedule a selector unless requested.
- Keep English client `pl.json` as the intentional development fallback until complete Polish localization is requested.
- Keep Animator root motion disabled unless a root-motion movement model is explicitly requested.
- Read every Memory Bank file at the start of each task as required by `.Codexrules`.
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
