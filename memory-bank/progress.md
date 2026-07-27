# Progress

## Current Status
- Memory bank initialized on 2026-05-07.
- Memory bank reviewed and refreshed through repository HEAD `8c954ff` on 2026-07-13.
- Repository contains an existing Unity client and ASP.NET Core backend with major gameplay support areas already scaffolded or implemented.
- Before this memory-bank edit, local branch `dev` was clean and exactly aligned with `origin/dev` (`0` ahead, `0` behind).

## What Works / Exists
- Backend solution structure is present.
- API startup configures Kestrel, Serilog, Swagger/NSwag, database initialization, HTTPS redirection, and endpoint mapping.
- Infrastructure configures EF Core, Identity, JWT bearer authentication, authorization policies, and database context services.
- Application layer configures MediatR, FluentValidation, logging and validation behaviors, and translation service.
- Domain entities exist for users, characters, transforms, inventory, quests, crafting recipes, and experience.
- API endpoints exist for users, application users, characters, transforms, inventories, quests, character quests, crafting recipes, character experience, and partial character updates.
- Unity client contains scenes, prefabs, networking scripts, UI scripts, shared managers, API models, and localization resources.
- Unity character state is stored per Netcode client ID in `UserManager.Characters`.
- Character health and max health exist in backend entities/DTOs and Unity client models/UI.
- Character update flow can persist health, max health, Strength, Dexterity, Speed, Intellect, Armor, equipped `HelmetType`/`ChestType`/`BootsType`/`WeaponType`/`AmmoType`, and `AmmoCount`.
- Server-side player attack handling reduces health, sends a targeted client RPC, updates player UI/audio/death response, and persists health through the API.
- Runtime stat calculation exists: Iron Sword/Wand/Bow select Strength/Intellect/Dexterity for outgoing fireball damage, Dexterity controls dodge chance, Speed scales controller movement, and Armor reduces incoming damage.
- Gear equip/unequip flow uses concrete usable item classes for helmet, chest, boots, and weapon slots and updates Gear UI plus backend character gear/stat totals in server builds.
- Iron Sword, Wand, and Bow are mirrored API/client items. All route through `InventoryItemEnum.IsWeapon()` to the shared weapon usable-item path, grant +20 Strength, Intellect, and Dexterity respectively, and select that same stat for outgoing fireball damage scaling.
- Ammo-slot data exists across API and Unity as `AmmoType` plus `AmmoCount`; the count is persisted/network-serialized, defaults to `0` for seeded template ammo, is shown in the ammo gear preview, and is preserved across first equip, same-type merge, different-type swap, explicit unequip, and weapon-triggered auto-unequip. Combat consumes one applicable unit per hit: Feather on positive non-dodged incoming attacks, or Arrow/Rune/Oil on outgoing hits. Reaching zero removes the item's stats, clears the slot, persists the result, and refreshes the owner Gear UI.
- Usable-item calls carry exact `UsableItemFromEnum.Inventory` / `.Gear` origins and a full `InventoryItemDto` from inventory/gear UI through RPC. Normal gear rejects re-equipping the same type; a different item swaps and returns the old item, and a Gear-origin call unequips/returns it.
- `AbstractGearUsableItem` centralizes removal/addition of all six declared stat bonuses, full character gear/stat/count persistence, editor UI refresh, and inventory add/remove operations; concrete gear classes define current item, slot, template, and wear/unwear mutation.
- Twelve tiered ammo items exist (Arrow/Rune/Feather/Oil I-III) with mirrored API/client enum values, icons, translations, merchant offers/prices, +5/+10/+15 stat metadata, localized required-weapon previews, and enforced compatibility: Arrow/Bow, Rune/Wand, Feather+Oil/Sword.
- All 12 ammo items have crafting recipes: Arrow/Rune/Feather use Blacksmithing and Oil uses Alchemy. API results are ordered by recipe `Id`, and pooled client recipe buttons preserve that order.
- Gear stat bonuses are declared on Unity inventory enum values with `InventoryItemParametersAttribute`; inventory, merchant, crafting, and gear previews display those bonuses through `InventoryUI.PrepareDescription(InventoryItemDto)`.
- Shared item previews now accept a full `InventoryItemDto` and display count-aware sell prices in inventory, loot, gear, crafting, and merchant contexts.
- Inventory items and equipped gear can be dragged with an icon/count preview instantiated from the serialized `InventoryDragPreview` prefab. Inventory drops swap different items, merge matching stacks, and preserve empty target indices. Equippable inventory items can be dropped onto any Helmet/Chest/Boots/Weapon/Ammo slot and are routed to their actual equipment slot, while equipped items can be dropped back onto inventory to unequip them through the existing persisted usable-item flow. Empty positions use `None`/count `0` placeholders, and add/split/remove flows preserve those positions.
- Merchant and crafting panels close on Escape or after the player moves out of interaction range.
- Character UI has a `RefreshDescription()` path used when opening the character panel and when level changes arrive from the server.
- Health potions restore up to 20 health capped at max, persist the resulting `Health` through `UpdateCharacterCommand` in dedicated-server builds, and consume one potion through the shared item flow; use at full health returns without consuming.
- API migrations are currently consolidated to a single `20260706184932_Init` migration plus the snapshot.
- Local one-command/dev-menu startup automation exists:
  - `Client/Automation/run.bat`
  - `Client/Automation/run.ps1`
  - Unity menu entries `ProjectX > Run` and `ProjectX > Build And Run`.
- Scene-backed quick-access bar exists for gear, inventory, character, and chat shortcuts using `QuickAccessUI` and `Resources/Icons/QuickAccess*.png`.
- Player locomotion is currently code-driven/root-motion OFF: `PlayerArmature.prefab` stores Animator `m_ApplyRootMotion: 0`, and movement is applied in `ThirdPersonController` through `CharacterController.Move(...)`.
- Local movement processing waits until `UserManager.Characters` contains the local client ID, preventing early stat-dependent character access.

## What's Left / Unknown
- Exact gameplay roadmap and release target are not documented.
- Automated coverage is narrow: only `TranslateServiceTests.cs` was found, although its parameterized cases currently total 190; stat, ammo, gear, potion, crafting, and Unity gameplay paths have no focused tests.
- Backend and generated Unity client projects compiled with zero errors and all 182 API unit tests passed on 2026-07-13, but the Unity client/dedicated server/API stack was not verified end-to-end at runtime.
- Client/server contract drift risk should be checked before API or DTO changes.
- Base-stat versus gear-derived-stat ownership is not documented; current gear use mutates persisted totals directly.
- Ammo compatibility, merge, swap, auto-unequip, inventory transfer, and Gear UI payload/count paths need focused tests.
- Additional ammo-specific combat effects beyond the current stat bonuses and one-unit consumption are not defined.
- Intellect is persisted, displayed, granted by Wand/Rune metadata, and used for outgoing fireball damage while Iron Wand is equipped.
- Full Polish client localization remains future work; the current English content in `Client/Assets/Resources/i18n/pl.json` is an intentional temporary development fallback.

## Known Issues / Risks
- Secrets or development keys appear in `API/src/API/appsettings.json`; treat as local development values unless the user confirms otherwise.
- Generated Unity and .NET artifacts are present in the workspace, so future searches and edits should avoid build/cache directories.
- A repo-level Visual Studio/.NET-style `.gitignore` is tracked, but generated Unity/.NET artifacts may still exist locally, so searches and edits should keep explicit exclusions.
- Full runtime automation has not been end-to-end verified after the final rename to `run`; only no-op script wiring was smoke-tested.
- `.claude/settings.local.json` was not present during the 2026-07-06 refresh; if it reappears, treat it as local-only secret configuration.
- `UpdateCharacterCommandHandler` currently resolves the current user's character and has the direct `CharacterId` filter commented out; confirm intended multi-character behavior before expanding character update endpoints.
- Seeded characters start with `AmmoType = AmmoTemplate` and the default `AmmoCount = 0`; the new combat-consumption flow has compile/test coverage but not a full Unity dedicated-server/API runtime test.
- Tiered ammo repurposed persisted IDs `1010`-`1012` from the former Rune/Feather/Oil meanings. The intentional database recreation masks this during development, but a non-destructive persistence flow will require explicit data migration.
- `.gitignore` ignores `*.meta`; the known affected project-owned files include 13 ammo icon/template metas plus `CharacterStatsCalculator.cs.meta`, `AmmoUsableItem.cs.meta`, and `UsableItemFromEnum.cs.meta`, so their Unity GUIDs/import settings are not versioned. Other ignored package/cache metas also exist locally.
- Merchant offer tooltips use the shared sell-price description while the adjacent currency amount is the full purchase price, so the displayed `Price` inside the tooltip can be misleading.
- Hiding the crafting panel or walking out of range does not stop an in-progress craft; the timer can finish and invoke the server craft after the UI closes.
- Git reports permission warnings for `C:\Users\pc/.config/git/ignore` during status checks.
- API startup currently calls `InitialiseDatabaseAsync()` unconditionally; the initializer uses `EnsureDeletedAsync()` and `EnsureCreatedAsync()`. This database reset is intentional and temporary for developer work, but should be revisited before preserving real data matters.
- `PlayerUI.SetPlayer()` currently calls `SetHealth()` and then `SetMaxHealth()` on the same text field, so the initial displayed value may be max health rather than current health.

## Evolution Notes
- 2026-07-24: Added bidirectional Inventory ↔ Gear drag-and-drop. Gear slots act as shared drop targets and occupied drag sources; any Gear slot accepts an equippable item, while usable-item dispatch selects its actual slot. Equip/unequip drops reuse `UseItemSubscribtion`, preserving gear swaps, whole ammo stacks, weapon/ammo validation, stat changes, UI refreshes, and server/API persistence. Hover callbacks are registered once per Gear slot, and the shared drag preview uses override sorting across both canvases. Client compilation passed with zero errors and the existing warnings.
- 2026-07-24: Fixed inventory stack splitting after the positioned-slot/drag changes. The right-click callback now captures the slot's stable index instead of the completed `for` loop counter, so Alt + right-click reaches the split subscription again.
- 2026-07-24: Added inventory drag-and-drop with a cursor-following preview, same-type stack merging, different-type swapping, exact empty-slot placement, client/server/API persistence, and focused move/slot-invariant tests. The drag preview is now a serialized prefab referenced by `InventoryUI` instead of a hierarchy assembled at runtime. Inventory hover callbacks are registered once per slot. API/client builds passed with zero errors, the API specification JSON parsed successfully, and all 206 API tests passed.
- 2026-07-17: Corrected shared Fireball/Arrow projectile cleanup. A hit now hides the complete projectile on clients, returns it to its pool after damage processing, deactivates it on release/network despawn, and reactivates it on get; client compilation passed with zero errors and six existing warnings.
- 2026-05-07: Initial memory bank created from repo inspection.
- 2026-07-02: Added Unity/API dev stack run automation, moved scripts under `Client/Automation/`, renamed from `serve` to `run`, and exposed Unity menu entries for `Run` and `Build And Run`.
- 2026-07-02: Added a scene-backed bottom-right Unity quick-access bar that reuses `InventorySlot` hover previews, loads quick-access icons from `Resources/Icons`, and toggles gear, inventory, character, and chat UI by mouse click.
- 2026-07-02: Refreshed memory bank to document current health, character update, gear usable item, quick-access, automation, and local-secret status.
- 2026-07-06: Refreshed memory bank to document `.Codexrules`, consolidated migrations, max health, gear stat attributes/previews, character description refresh, database reset behavior, and current localization/UI risks.
- 2026-07-07: User clarified that database reset on startup and English text in client `pl.json` are intentional temporary development choices.
- 2026-07-08: Confirmed and documented that player animation/locomotion uses Animator root motion OFF, with movement driven by `ThirdPersonController`/`CharacterController`.
- 2026-07-08: Added percentage-based runtime Strength/Dexterity/Speed/Armor behavior, moved character state to a per-client-ID dictionary, and added initial ammo-slot API/client scaffolding. Documented the append-only translation-key ordering convention.
- 2026-07-09: Renamed character stats and equipped fields to the current Dexterity/Speed and `*Type` contract, removed Spirit, expanded ammo to 12 tiered variants, and added recipes for every ammo type with deterministic API/client display ordering.
- 2026-07-10: Refreshed inventory/loot/gear/crafting/merchant previews to use DTO/count-aware descriptions and sell prices; added merchant/crafting distance/Escape close behavior and substantial `UIScene` layout updates.
- 2026-07-10: Corrected memory-bank ammo claims after static inspection: `AmmoCount` exists in the contract but stack management and attack consumption are not implemented, and Arrow Dexterity bonuses are not applied by the current ammo usable-item path.
- 2026-07-13: Commit `8c954ff` added explicit Inventory/Gear item-use origins, full-DTO gear handling, centralized stat/slot/inventory transitions, first-equip/swap/unequip ammo stack handling, server-persisted health-potion healing, template ammo count `0`, and a local-character readiness guard for movement.
- 2026-07-13: Refreshed all memory-bank files, compilation-smoke-tested the backend and generated Unity client projects, passed all 182 API unit tests, and documented the remaining same-type ammo duplication/stale-UI defect plus the lack of end-to-end runtime coverage.
- 2026-07-14: Added Iron Wand and Iron Bow content across enums, localization, API specification, icon lookup, pricing, and weapon-equipment dispatch. Introduced `InventoryItemEnum.IsWeapon()`, assigned +20 Intellect to the wand and +20 Dexterity to the bow while retaining +20 Strength on the sword, compiled API/client with zero errors, and passed all 190 translation tests.
- 2026-07-14: Replaced unconditional Strength-based fireball scaling with weapon-aware `ApplyWeaponDamage`: sword uses Strength, wand Intellect, bow Dexterity, and empty/unknown weapons use base damage. Client compilation passed with zero errors and existing warnings.
- 2026-07-14: Converted all `CharacterStatsCalculator` operations to `CharacterDto` extensions and their shared private calculations to `short` extensions; updated fireball, damage/dodge, and movement call sites. Client compilation passed with zero errors and existing warnings.
- 2026-07-14: Enforced ammo/weapon categories (Arrow/Bow, Rune/Wand, Feather+Oil/Sword), added localized required-weapon preview lines, rejected incompatible ammo use, and made weapon changes atomically auto-unequip incompatible ammo with full stack/stat/UI/persistence handling. The shared transition also fixed same-type ammo duplication and stale Gear UI payloads; client compilation and localization JSON validation passed.
- 2026-07-15: Replaced the weapon/ammo category mapping extensions with `InventoryItemParametersAttribute.WeaponCategory` metadata assigned to all current weapons and ammo. Compatibility validation and required-weapon descriptions now read the same metadata; client compilation passed with zero errors and six existing warnings.
- 2026-07-15: Implemented one-unit combat ammo consumption across Unity server, owner client/UI, and API persistence. Feather armor ammo is consumed on positive non-dodged incoming hits; Arrow/Rune/Oil damage ammo is consumed on outgoing hits. The last unit contributes to its hit, then the item bonus and slot are removed. API/client builds passed with zero errors and all 190 API tests passed.
