# Account stash smoke test

Rebuild/restart the changed API and dedicated server before testing. Development API startup resets its database; use disposable development data. Migration `20260921161117_AddAccountStash` is required for an existing SQL database.

The Stash cube is in EnvironmentScene at `(-25.01, 1.781, 5.7495)`, near crafting stations. Interact using right-click within 5 units. The account stash starts with 64 empty slots.

## Pending authenticated runtime checks

1. Open the stash: pointer hover near the cube, no interaction through UI, no open outside range. Walk away, press Escape, close inventory, open another panel, or travel through a portal: stash closes and a late response must not reopen it. Delay the initial stash read, then open Character, Gear, Quest, Crafting or Merchant before the response: the newly selected panel must stay open and the stash must remain closed.
2. Right-click a stack in inventory to deposit and in stash to withdraw. Verify all units move and equipment/consumables are not used while the stash is open. Drag both ways onto empty and compatible slots. Dropping outside cancels and restores the source icon/count.
3. Alt+right-click a stash stack of 11: verify 6 remain and 5 occupy the first free slot. Drag within stash to move, merge or swap; confirm stable indices and a maximum of 1024 per stack.
4. Fill the destination. Verify full transfer/split rejection leaves both inventories unchanged. Verify automatic transfer fills partial stacks and then free slots, while a targeted drop that cannot fit the entire stack rejects without partial removal.
5. Scroll with the wheel over occupied and empty cells, drag the scrollbar, and reach slot 64. Check tooltips, counts and both windows in landscape and portrait. Inventory must retain its usual position; Stash uses the standard Quest/Character/Merchant position when space permits, with the Friends close button, inventory slot spacing and thin scrollbar. Confirm one InventoryOpen/InventoryClose sound per transition.
6. Accept a Collect quest. Withdraw enough items to finish it, then deposit them: progress returns to Accepted and the NPC marker changes in both directions. Repeat rapidly with delayed/reordered quest GET responses; the newest refresh must win. Accept/progress/complete a quest while a refresh is pending; an intervening server update must trigger a fresh read rather than be overwritten. Complete it while performing stash operations; no double consumption or stale progress should remain. When a refresh observes Completed before the completion RPC arrives, verify the quest log entry disappears and the NPC advances exactly once; repeat with RPC arriving first. Also let a refresh observe a newly accepted quest before its acceptance RPC: verify one CharacterQuests entry, one log entry and one NPC notification, including repeated delivery. These timing and NPC-marker checks remain pending authenticated runtime verification.
7. Reconnect and select a second character on the same account: stash is shared; another account sees a separate stash. With two sessions on the same account, simultaneous stale operations must report a conflict without duplicating/loss of items.
8. Start/lock a player trade while a stash request is pending. Verify accepted persistence settles before trade commit; locked inventory rejects further stash mutations. Disconnect during a transfer and reopen after reconnect to verify authoritative state.
9. Interrupt the API response during a transfer. No optimistic removal or automatic transfer replay should occur. Reopening the stash must reconcile its contents and inventory.

## Completed verification

- Backend build/OpenAPI generation, formatting, SQL Server-configured EF pending-model check and 446 tests passed. Stash coverage includes account ownership/sharing, Collect progression/regression, stale revisions/source stacks, full destinations, stable indices, split/move and max-stack rules.
- Unity generated compilation passed without `UNITY_SERVER` and with `UNITY_SERVER` excluding editor symbols; existing warnings remain.
- Isolated Unity import/Netcode processing and editor probes passed for 64 slots, 1024-count visibility, drag source restoration, wheel forwarding, bottom-slot access and bounds at 1600×900, 1280×720, 1024×768, 720×1280, 480×1280 and 320×900. These probes do not replace the authenticated Play Mode checks above.
