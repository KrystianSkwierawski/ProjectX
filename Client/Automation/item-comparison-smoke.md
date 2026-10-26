# Equipment comparison

Inventory, stash and merchant offers use the existing slot preview plus an adjacent equipped-item card. Deltas are candidate minus equipped **item stats**, not a prediction of total character stats, buffs or ammo removed when changing weapon category. All six item parameters use the same comparison: positive green, negative red, unchanged without a badge; missing candidate stats display zero and their loss. The companion reads the local character snapshot, independent of whether Gear is open. Empty equipped slots and non-gear keep a single preview. Narrow canvases stack the pair.

New items, appended IDs 1023–1026, are ScoutHelmet, GuardChest, TrailBoots and BalancedSword. The EnvironmentScene merchant sells one of each for the existing test price of one coin. They use existing equip, persistence, stash and action-bar paths, with PL/EN names/descriptions and individual PNG icons.

## Focused validation

Run Unity with `-batchmode -nographics -quit -executeMethod ProjectX.Editor.ItemComparisonValidation.Run`. Expected marker: `ITEM_COMPARISON_VALIDATED`. This instantiates the real slot prefab and checks slot matching, cross-category weapons, positive/negative/missing stats, equal items, empty slots, pool reuse, pointer exit, PL/EN, bounds at 1920×1080, 1280×720, 720×1280 and 480×900, and imported icons. It does not authenticate or connect clients.

## Live smoke

Verified on 2026-10-22: API/OpenAPI build (invocation-only `NuGetAudit=false` for the existing Pkcs advisory), 18 CharacterSettings tests, 27 OpenAPI contract tests, scoped formatting, normal client and dedicated `UNITY_SERVER` compilation (zero errors, 10 existing warnings), Unity import/Netcode processing and `ITEM_COMPARISON_VALIDATED`. The rendered PL comparison was visually inspected at `Client/Logs/item-comparison-preview.png`. The following authenticated checks remain pending:

1. Rebuild the client and dedicated server with the new catalogue, and rebuild/restart the API before testing. Development API startup recreates its database.
2. Buy the four variants from the existing merchant; equip the original iron items and hover each variant in merchant, inventory and stash. Confirm both cards stay readable at screen edges with Gear closed.
3. Compare GuardChest against IronChest: health −8, armor +12. Compare TrailBoots against IronBoots: health +3, speed +20, armor −3.
4. Compare IronHelmet against ScoutHelmet: dexterity 0 (−8), health +4, armor +6. Compare the same item to itself: no colored differences.
5. Swap equipment while hovering, then drag, close a window, reopen and hover a non-gear item. No stale companion should remain; both cards must ignore pointer raycasts.
6. Buy, equip, stash, withdraw and bind each new item to the action bar; reconnect to confirm persisted item IDs and gear state.

## Icon provenance

Created with built-in `image_gen`, four separate calls. Final assets: `Assets/Resources/Icons/{ScoutHelmet,GuardChest,TrailBoots,BalancedSword}.png`, with individual Unity metadata.

Shared prompt: “Use case: stylized-concept. Create one square fantasy RPG inventory icon: SUBJECT. Hand-painted semi-realistic game item art with visible brushwork and crisp silhouette, matching practical iron equipment in a cozy duck MMORPG. Single isolated item centered with safe margins, readable at 48px. Warm upper-left highlights, neutral materials. No text, border, scenery, characters, watermark or UI. Transparent background.”

Subjects: a brown leather scout helmet, open face, stitched panels and small steel brow guard; a heavy steel breastplate with reinforced shoulder guards, practical worn finish; a pair of supple brown leather walking boots, green cloth cuffs and sturdy soles; a slim steel one-handed sword with simple brass crossguard and brown wrapped grip.
