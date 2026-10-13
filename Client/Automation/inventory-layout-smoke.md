# Inventory layout regression smoke

Status: pending authenticated runtime validation. Generated C# compilation does not validate RPC processing or these scenarios.

Use a client and dedicated server with the same scripts. Place different items in slots 0, 1 and 2, plus a stack with at least two items. Inject latency/failures only in a local test environment.

1. Delay CharacterInventories POST. Move 0 to 1 and immediately attempt 1 to 2 or split a stack. Only the first command may change the client layout or reach the API while awaiting acknowledgement. After acknowledgement, another operation must work. Reconnect and compare slot types/counts.
2. Repeat with Split first, then Move. Confirm the shared pending gate covers both operations and no items are lost or duplicated.
3. Return a rejection, HTTP 500 or timeout for a layout POST. The client must request an authoritative GET, discard the optimistic layout and allow another Move/Split only after that GET is applied. The POST must not be retried.
4. Fail the recovery GET, then restore it. Move/Split stay blocked during recovery; reads retry at two-second intervals. A concurrent inventory update that supersedes a GET must not release the gate before a later authoritative read succeeds.
5. Disconnect while a layout POST is pending. The accepted server operation must finish under inventory tracking; no owner RPC/UI work may execute after despawn. Reconnect and verify the persisted slots.
6. Lock a trade before a layout RPC reaches the server. The layout is rejected and reloaded; after trade cancellation the client must not remain stuck in the pending state.
7. With a stack of 10, start gathering 2 more and immediately split. Delay responses so the API adds before splitting (6/6) while the client initially predicts 5/5. Even when both writes return Applied, wait for the layout reload and verify exact slot counts match the API, not the transient client 7/5. Repeat with selling/removing items during the split.

## Equipment commit regression

Run `dotnet run --project Client/Automation/InventoryRegression/InventoryRegression.csproj` from the repository root. This dependency-free harness compiles the actual gear classes and DTOs with `UNITY_SERVER` and without `UNITY_EDITOR`, replacing only engine/UI/service boundaries. It checks that pending/rejected equipment changes do not affect live state, accepted changes survive despawn without overwriting current health/buffs, and weapon/ammo switching and unequip commit together. It does not exercise NGO or HTTP.

Runtime check still required: delay/reject an equip request for an unowned item while attacking/taking damage. Server combat must use the original equipment until API acceptance; a rejected request must never grant its bonus. Verify successful equip and unequip as well as incompatible ammo return.

Health check: equip IronChest and heal to 120/120, then unequip. After acceptance and reconnect, expect 100/100. Delay the save and inflict damage before acceptance: current HP must become min(current HP, 100), including remaining at zero after death. Reject the unequip and confirm that HP and MaxHealth are unchanged. The harness covers the pending/rejected state, the HP included in the transaction and commit-time clamping at 120, 110, 85 and 0 HP; runtime networking remains a separate check.

Diagnostic messages: `Inventory layout saved`, `Inventory layout save failed`, `Inventory layout reload failed`, `Inventory layout reconciled`.

## Uncertain potion response

At 50/100 HP, use a health potion and inflict 10 damage while its POST is pending. Let the API commit but drop the response. After disconnect and receipt-backed retry, queued damage persistence must store 60 HP; reconnect must show 60 HP and one potion consumed. Also hold the original POST before SaveChanges and allow the identical retry to finish first: the late original must resolve the same receipt and consume nothing further. Repeat without a potion: expect a durable OperationRejected receipt, 40 HP and no consumption. Fail retry transport temporarily: queued HP writes and session revocation must remain blocked until a final receipt-backed response. API regression tests model the overlapping-save conflict and verify replay after later damage; the client harness validates healing after despawn. HTTP ordering and reconnect still require runtime verification.

## Ammo hit ordering and uncertain response

Automated checks (2026-10-12): `AMMO_HIT_REGRESSION_PASSED` in the same InventoryRegression harness covers waiting before hit calculation, ammo switch/unequip, final defensive charge, immutable retries, a blocked subsequent equipment write, recovery after despawn and invalid/dodged hits. Two API regressions cover replay after newer equipment/HP and an overlapping retry followed by newer equipment. All 570 backend tests pass. Client/server builds without UNITY_EDITOR and Unity import/Netcode pass. The isolated editor probe emits `AMMO_UNITY_THREAD_PROBE_PASSED` in ignored `Client/Logs/ammo-unity-probe.log`, verifying that callbacks remain on Unity's main thread after semaphore waits and timeout retry delays. These checks use injected persistence and do not replace authenticated HTTP/network gameplay.

Manual runtime checks still required:

1. Equip AmmoOil1, delay an unequip until a projectile reaches its target, then let unequip commit. The queued hit must calculate damage without the removed ammo bonus and consume no charge. Repeat switching to AmmoOil3: damage must use Oil3's bonus and consume exactly one Oil3 charge.
2. Take a non-dodged hit with one AmmoFeather1 remaining: that hit uses its armor bonus, consumes the last charge and removes the bonus before the next hit. A dodge consumes no charge.
3. Drop the response of a committed ammo transaction. The player disconnects and receipt recovery retries the identical operation/payload; queued equipment and trade completion stay blocked until it resolves. Reconnect must show one consumed charge, correct final-charge stats and current HP.
4. Delay the original ammo save, allow the identical retry to commit, then make a later equipment change. Releasing the original request must not restore older ammo/stats. Temporary retry transport failures must keep the barrier held.
5. Despawn the projectile while its hit waits on equipment, reuse the projectile pool for a different target, and verify the old callback cannot read the reused projectile's fields or hit a despawned/reused target. Disconnect the player before hit resolution: an unaccepted hit must be skipped; an already accepted cost must still persist.

Equipment receipt smoke: delay an IronChest equip/unequip POST past its response timeout, allow the same-ID retry to finish before the original, then resume the original. Reconnect must show exactly one inventory transition and matching gear/stats. Inflict damage while pending and verify that it survives recovery and queued HP persistence. Test incompatible ammo return during weapon switch and rejection for missing items/full inventory. These HTTP/reconnect scenarios remain pending; API regression tests simulate the late-save concurrency conflict.
