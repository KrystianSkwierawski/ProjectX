# Active Context

## Current Focus
- Memory bank reviewed and refreshed on 2026-07-10 against repository HEAD `518aac4`.
- No feature implementation task is active after this documentation refresh.
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
- 2026-07-10: Iron Boots' Speed bonus changed from `15` to `80`; seeded characters now have `AmmoCount = 1`, and seeded inventory contains four health potions plus 9999 currency.

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
- Resolve character state through `UserManager.Characters[clientId]`; do not reintroduce a process-wide single `Character` reference.
- Treat current stat mechanics as percentage-based: Strength increases fireball damage, Dexterity is capped dodge chance, Speed increases movement, and Armor is capped damage reduction. Intellect remains data/UI-only.
- Treat the ammo gear contract as `AmmoType` plus `AmmoCount`, but do not assume stack behavior exists. The current usable-item path moves one item, toggles `AmmoType`, and does not update `AmmoCount` or consume ammo during attacks.
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
- Complete or redesign ammo equip transitions: update/persist `AmmoCount`, transfer whole stacks as intended, return replaced ammo, remove the old bonus before adding a new one, and define attack effects/consumption.
- Apply/persist Arrow Dexterity bonuses in `AmmoUsableItem` if the metadata is intended to drive equipped stats; currently Arrow previews advertise Dexterity but the equip code omits it.
- Define Intellect's gameplay effect and add focused tests for stat calculation, ammo transitions/counts, and crafting ordering; only translation-service unit tests are currently present.
- Before replacing the intentional database reset with persistent upgrades, create a data-migration plan for ammo IDs `1010`-`1012`, whose meanings were repurposed when tiered ammo was introduced.
- Fix or narrowly override the repo's `*.meta` ignore rule before adding more Unity assets so their GUIDs/import settings are versioned.
- Clarify whether consumable health restoration should persist through `UpdateCharacterCommand`; current `HealthPotionUsableItem` contains a TODO and updates health locally before normal inventory consumption.
- Review `PlayerUI.SetPlayer()` before health UI work; it currently writes current health and then max health to the same text field.
- Full Polish client localization can be planned later; current English content in client `pl.json` is a deliberate temporary development fallback.
- Be cautious with API data while running locally because startup intentionally deletes and recreates the database for the current development workflow.

## Important Local Notes
- `git status` emits warnings about inaccessible global ignore config at `C:\Users\pc/.config/git/ignore`.
- Before this 2026-07-10 memory-bank edit, `git status -sb` showed clean `dev...origin/dev`; `.gitignore` was tracked and branch divergence was `0 0`.
