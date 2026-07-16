# Active Context

## Current Focus
- 2026-07-15: Implemented server-authoritative per-hit ammo consumption. Feather armor ammo is consumed after `ApplyArmor` on a positive, non-dodged incoming attack; Arrow/Rune/Oil damage ammo is consumed after outgoing weapon damage is calculated. The final item contributes to that hit, then its stats are removed, the ammo slot is cleared, API state is persisted, and the owner Gear UI is refreshed.
- 2026-07-14: Added `IronWand` and `IronBow` as mirrored API/client inventory items. Weapon classification now goes through `InventoryItemEnum.IsWeapon()`, iron weapon bonuses are Strength for sword, Intellect for wand, and Dexterity for bow, and outgoing fireball damage scales from the corresponding equipped-weapon stat.
- Memory bank reviewed and refreshed on 2026-07-13 against repository HEAD `8c954ff`.
- The 2026-07-13 gear usable-item refactor now carries a full item DTO plus exact `UsableItemFromEnum.Inventory` / `.Gear` origin through UI, subscription, RPC, and server handling; backend and generated-client compilation smoke checks pass with warnings and zero errors.
- Ammo equip, same-type merge, different-type swap, gear unequip, and incompatible-weapon auto-unequip preserve full stack counts, stat bonuses, Gear UI payloads, persistence, and inventory transfers.
- At the start of this refresh, branch `dev` was clean and exactly aligned with `origin/dev` (`0` ahead, `0` behind).

## Recent Changes
- Created the required memory bank structure:
  - `projectbrief.md`
  - `productContext.md`
  - `activeContext.md`
  - `systemPatterns.md`
  - `techContext.md`
  - `progress.md`
- 2026-07-02: Added local run automation for the Unity/API dev stack:
  - `Client/Automation/run.bat`
  - `Client/Automation/run.ps1`
  - `Client/Assets/Editor/ProjectXDevAutomation.cs`
- 2026-07-02: Added a scene-backed bottom-right quick-access UI bar in `UIScene`, using the existing `InventorySlot` prefab and `Resources/Icons/QuickAccess*.png` icons for gear, inventory, character, and chat shortcuts.
- 2026-07-02: Documented recent character/gear/health work now visible in the repository:
  - `UpdateCharacterCommand` persists optional character health, stats, and gear fields.
  - Player attack handling updates health through server-side subscription flow, targeted client RPC, and API persistence.
  - Gear usable items now share `AbstractGearUsableItem` behavior and concrete helmet/chest/boots/weapon item classes.
- 2026-07-02: Noted untracked `.claude/settings.local.json` as local tool configuration containing secret-like values that must not be committed or quoted.
- 2026-07-06: Confirmed `.Codexrules` is now the active memory-bank instruction file; `CLAUDE.md`, `.claude`, and a repo-level `.gitignore` were absent at that time.
- 2026-07-06: Confirmed character `MaxHealth` is part of backend domain state, API get/update DTOs, Unity character model serialization, seeded characters, and player UI update flow.
- 2026-07-06: Documented current gear-stat implementation:
  - Backend `Character`, `CharacterDto`, and `UpdateCharacterCommand` persist health, max health, stats, armor, and equipped `*Type` values.
  - Unity `InventoryItemParametersAttribute` annotates gear enum values, currently including iron helmet/chest/boots/sword bonuses.
  - Inventory, merchant, crafting, and gear slot previews reuse `InventoryUI.PrepareDescription(InventoryItemDto)` to show sell prices and stat bonuses.
  - Helmet/chest/boots/weapon usable items mutate the relevant entry in `UserManager.Characters` and persist updated slot/stat fields in server builds.
- 2026-07-06: Character UI description refresh now runs before showing the Character panel and after level-up RPCs.
- 2026-07-06: API migrations were consolidated to `20260706184932_Init`; previous incremental migration files are no longer present.
- 2026-07-06: Verified API startup still calls `InitialiseDatabaseAsync()` unconditionally, and the initializer deletes/recreates the database before seeding development data.
- 2026-07-07: User clarified that the API startup database reset via `EnsureDeletedAsync()` / `EnsureCreatedAsync()` is an intentional, temporary development workflow.
- 2026-07-07: User clarified that English text in `Client/Assets/Resources/i18n/pl.json` is an intentional, temporary development fallback, not an accidental localization bug.
- 2026-07-07: Observed a staged repo-level `.gitignore` with Visual Studio/.NET style ignore rules; it is tracked and the worktree/index are clean as of 2026-07-10.
- 2026-07-08: Confirmed player animation/locomotion root motion is OFF: `Client/Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab` has Animator `m_ApplyRootMotion: 0`, and `ThirdPersonController` moves the character through `CharacterController.Move(...)`.
- 2026-07-08: Added `CharacterStatsCalculator`: Strength scales outgoing fireball damage, Dexterity controls dodge chance, Speed scales controller movement, and Armor reduces incoming damage. Intellect has no runtime effect yet.
- 2026-07-08: Replaced the single `UserManager.Character` reference with `UserManager.Characters`, keyed by Netcode client ID, across player load/combat/UI/gear paths.
- 2026-07-08: Added ammo-slot contract scaffolding across API and Unity (`AmmoType`/`AmmoCount`) and an ammo gear slot. Static review on 2026-07-10 confirmed that stack transfer/count mutation and attack consumption are not implemented.
- 2026-07-08: User clarified that new `TranslateKeyEnum` values and matching `en.json`/`pl.json` entries must be appended at the end in enum order to preserve existing numeric ordering.
- 2026-07-09: Renamed character contract fields across API, migrations, generated API specification, and Unity: Agility became Dexterity, Stamina became Speed, Spirit was removed, and equipped gear fields became `HelmetType`, `ChestType`, `BootsType`, `WeaponType`, and `AmmoType`.
- 2026-07-09: Expanded ammo to 12 tiered items (Arrow/Rune/Feather/Oil I-III, IDs `1009`-`1020`) with mirrored enums, translations, icons, merchant stock/prices, and metadata bonuses of +5/+10/+15.
- 2026-07-09: Added recipes for all 12 ammo items. Arrow/Rune/Feather recipes use Blacksmithing, Oil recipes use Alchemy, API results are ordered by recipe `Id`, and pooled recipe buttons are moved to the last sibling to preserve that order.
- 2026-07-10: Centralized item previews through `InventoryUI.PrepareDescription(InventoryItemDto)`, including count-aware sell prices across inventory, loot, gear, crafting, and merchant UI. Ammo gear preview passes `AmmoCount`.
- 2026-07-10: Merchant and crafting panels now hide on Escape or when movement carries the player out of interaction range; `UIScene` received broad rounded-corner material/layout serialization changes.
- 2026-07-10: Iron Boots' Speed bonus changed from `15` to `80`; seeded inventory contains four health potions plus 9999 currency. An explicit seeded `AmmoCount = 1` existed at this point and was removed on 2026-07-13.
- 2026-07-13: Replaced inferred `isWearing` behavior with `UsableItemFromEnum.Inventory` / `.Gear` across UI, RPC, and usable items. `AbstractGearUsableItem` now centralizes full stat-bonus replacement, character persistence, and inventory transfer while concrete gear classes mutate their slots.
- 2026-07-13: Ammo first-equip, different-type swap, and unequip paths now transfer/persist whole stack counts and all declared bonuses, including Arrow Dexterity. Static review found that same-type merging still duplicates the previous stack into inventory and displays/captures only the incoming count in Gear UI.
- 2026-07-13: Health potions now persist the capped health restoration through `UpdateCharacterCommand` in dedicated-server builds before normal one-item consumption; use at full health returns without consuming.
- 2026-07-13: Seeded characters retain `AmmoType = AmmoTemplate` but no longer set `AmmoCount`, so its default is `0`. `ThirdPersonController.Update()` now waits for the local character dictionary entry before running stat-dependent movement.
- 2026-07-13: `dotnet build API/ProjectX.sln --no-restore` and `dotnet build Client/Assembly-CSharp.csproj --no-restore` completed with zero errors and existing warnings; `dotnet test API/ProjectX.sln --no-build --no-restore` passed all 182 translation-service test cases. No Unity/dedicated-server end-to-end runtime test was performed.
- 2026-07-14: Added `IronWand = 1021` and `IronBow = 1022` across API/client enums, generated API specification, API/client English and Polish translations, icon-based client loading, and merchant pricing. `IsWeapon()` now routes all three iron weapons to `WeaponUsableItem`; their +20 bonuses target Strength, Intellect, and Dexterity respectively. Safe builds passed with NSwag disabled for the API, and all 190 translation tests passed.
- 2026-07-14: Replaced unconditional fireball `ApplyStrength` scaling with `ApplyWeaponDamage`: Iron Sword selects Strength, Iron Wand Intellect, and Iron Bow Dexterity. Empty or unknown weapon types apply base damage without a stat multiplier; attack visuals and other combat behavior remain unchanged. The generated Unity client project compiled with zero errors and existing warnings.
- 2026-07-14: Refactored `ApplyWeaponDamage`, `IsAttackDodged`, `ApplySpeed`, and `ApplyArmor` into `CharacterDto` extension methods. Runtime call sites now use `character.Method(...)`; `GetIncreaseMultiplier` and `GetLimitedPercent` are private `short` extensions inside `CharacterStatsCalculator`.
- 2026-07-14: Added weapon-category requirements for ammo: Arrows require Bow, Runes require Wand, and Feathers/Oils require Sword. Incompatible right-click equip attempts are rejected. Equipping or removing a weapon now auto-unequips incompatible ammo, returns its full stack, removes its stats, clears its Gear slot, and persists both slots atomically. Ammo previews show a localized required-weapon line. The same transition refactor also fixed same-type ammo inventory duplication and stale Gear UI counts.
- 2026-07-15: Moved weapon/ammo category assignment into `InventoryItemParametersAttribute.WeaponCategory`. Compatibility checks and ammo preview requirements now compare item metadata directly; the former `GetWeaponCategory()` and `GetRequiredWeaponCategory()` mapping extensions were removed.
- 2026-07-15: Added combat ammo consumption across Unity server, owner client, Gear UI, and API persistence. `AmmoCount` decrements once per applicable hit; reaching zero subtracts all item parameters and replaces the equipped ammo with `AmmoTemplate`. The Unity server owns this transition; the API only persists the submitted `AmmoType`, `AmmoCount`, and stat values without deriving or normalizing them.

## Active Decisions
- Treat the repository as two cooperating applications:
  - `Client/`: Unity 6000.1 project using Netcode for GameObjects.
  - `API/`: ASP.NET Core API using clean/layered architecture.
- Preserve existing project organization and naming patterns when making future changes.
- Read all memory bank files at the start of every task, per `.Codexrules`.
- Local dev startup automation lives inside the Unity project under `Client/Automation/`, not at repository root.
- Unity menu entries under `ProjectX` should remain aligned with `Client/Automation/run.bat`.
- If `.claude/settings.local.json` reappears, treat it as local-only secret configuration and do not commit or quote it.
- Treat current character stat totals as persisted mutable values until the user clarifies whether stats should be recomputed from base stats plus gear bonuses.
- Keep ammo-consumption and zero-count unequip decisions on the Unity server. `UpdateCharacterCommand` is a persistence pass-through for the submitted `AmmoType`, `AmmoCount`, and stats and must not automatically replace ammo types or normalize ammo counts.
- Resolve character state through `UserManager.Characters[clientId]`; do not reintroduce a process-wide single `Character` reference.
- Treat current stat mechanics as percentage-based: outgoing fireball damage uses Strength with Iron Sword, Intellect with Iron Wand, and Dexterity with Iron Bow; empty/unknown weapon types use base damage. Dexterity is also capped dodge chance, Speed increases movement, and Armor is capped damage reduction.
- Classify equippable weapons through client `InventoryItemEnum.IsWeapon()` rather than enumerating weapon cases in usable-item dispatch. Weapon metadata controls the persisted equipment bonus, while `CharacterStatsCalculator.ApplyWeaponDamage` maps weapon type to its damage stat.
- Treat the ammo gear contract as `AmmoType` plus `AmmoCount`, with use origin selected by `UsableItemFromEnum.Inventory` / `.Gear`. Weapon/ammo compatibility is category-based and read from `InventoryItemParametersAttribute.WeaponCategory`: Arrows/Bow, Runes/Wand, Feathers+Oils/Sword. All equip, merge, swap, explicit unequip, and weapon-triggered auto-unequip paths use whole-stack transfers. Feathers are armor ammo consumed after a positive, non-dodged incoming hit; all other real ammo is damage ammo consumed after an outgoing hit. The last unit applies to the current hit before its bonus is removed.
- Treat health-potion restoration as server-persisted through `UpdateCharacterCommand`; at full health the potion is intentionally not consumed.
- Preserve API crafting recipe ordering by `Id` and the pooled client recipe-button sibling ordering.
- Keep the current API database reset behavior for developer work unless the user asks to prepare a non-destructive persistence flow.
- Do not replace the English client `pl.json` fallback unless the user explicitly asks for proper Polish client localization.
- Keep player locomotion code-driven with Animator root motion disabled unless the user explicitly asks for a root-motion movement model.
- Append new `TranslateKeyEnum` values at the end of the enum, and append matching API/client `en.json`/`pl.json` entries at the end in the same order; do not insert new translation keys in the middle of existing enum values or localization files.

## Next Steps
- For future implementation tasks, first refresh this file with the current working focus.
- Confirm product-level goals with the user when changes require design or gameplay decisions not already implied by code.
- Keep `progress.md` updated after meaningful changes.
- When changing startup/build behavior, update both `Client/Automation/run.ps1` and `Client/Assets/Editor/ProjectXDevAutomation.cs`.
- Add focused automated or Unity runtime coverage for per-hit ammo consumption, final-item stat removal, API persistence, and owner Gear UI refresh.
- Add focused tests for weapon-based damage stat selection, first ammo equip, same-type merge, type switch, unequip/reload, health-potion persistence, and crafting ordering; only translation-service unit tests are currently present.
- Before replacing the intentional database reset with persistent upgrades, create a data-migration plan for ammo IDs `1010`-`1012`, whose meanings were repurposed when tiered ammo was introduced.
- Fix or narrowly override the repo's `*.meta` ignore rule before adding more Unity assets/scripts so their GUIDs/import settings are versioned; the known affected project files include 13 ammo icon/template metas and three gameplay-script metas.
- Review `PlayerUI.SetPlayer()` before health UI work; it currently writes current health and then max health to the same text field.
- Full Polish client localization can be planned later; current English content in client `pl.json` is a deliberate temporary development fallback.
- Be cautious with API data while running locally because startup intentionally deletes and recreates the database for the current development workflow.

## Important Local Notes
- `git status` emits warnings about inaccessible global ignore config at `C:\Users\pc/.config/git/ignore`.
- Before this 2026-07-13 memory-bank edit, `git status -sb` showed clean `dev...origin/dev` at `8c954ff`; `.gitignore` was tracked and branch divergence was `0 0`.
