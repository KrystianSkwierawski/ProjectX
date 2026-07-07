# Active Context

## Current Focus
- Memory bank refreshed on 2026-07-06 and amended on 2026-07-07 after user clarification.
- No feature implementation task is currently active after this documentation refresh.
- Current branch is `dev`; before this memory-bank edit it was clean and one commit ahead of `origin/dev` with `f8c25b2 add .codexrules`.

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
  - Backend `Character`, `CharacterDto`, and `UpdateCharacterCommand` persist health, max health, stats, armor, and equipped helmet/chest/boots/weapon values.
  - Unity `InventoryItemParametersAttribute` annotates gear enum values, currently including iron helmet/chest/boots/sword bonuses.
  - Inventory, merchant, crafting, and gear slot previews reuse `InventoryUI.PrepareDescription()` to show stat bonuses.
  - Helmet/chest/boots/weapon usable items mutate `UserManager.Instance.Character` stat totals and persist updated slot/stat fields in server builds.
- 2026-07-06: Character UI description refresh now runs before showing the Character panel and after level-up RPCs.
- 2026-07-06: API migrations were consolidated to `20260706184932_Init`; previous incremental migration files are no longer present.
- 2026-07-06: Verified API startup still calls `InitialiseDatabaseAsync()` unconditionally, and the initializer deletes/recreates the database before seeding development data.
- 2026-07-07: User clarified that the API startup database reset via `EnsureDeletedAsync()` / `EnsureCreatedAsync()` is an intentional, temporary development workflow.
- 2026-07-07: User clarified that English text in `Client/Assets/Resources/i18n/pl.json` is an intentional, temporary development fallback, not an accidental localization bug.
- 2026-07-07: Observed a staged repo-level `.gitignore` with Visual Studio/.NET style ignore rules; treat it as an existing working-tree/index change and do not modify it unless asked.

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
- Keep the current API database reset behavior for developer work unless the user asks to prepare a non-destructive persistence flow.
- Do not replace the English client `pl.json` fallback unless the user explicitly asks for proper Polish client localization.

## Next Steps
- For future implementation tasks, first refresh this file with the current working focus.
- Confirm product-level goals with the user when changes require design or gameplay decisions not already implied by code.
- Keep `progress.md` updated after meaningful changes.
- When changing startup/build behavior, update both `Client/Automation/run.ps1` and `Client/Assets/Editor/ProjectXDevAutomation.cs`.
- Clarify whether consumable health restoration should persist through `UpdateCharacterCommand`; current `HealthPotionUsableItem` contains a TODO and updates health locally before normal inventory consumption.
- Review `PlayerUI.SetPlayer()` before health UI work; it currently writes current health and then max health to the same text field.
- Full Polish client localization can be planned later; current English content in client `pl.json` is a deliberate temporary development fallback.
- Be cautious with API data while running locally because startup intentionally deletes and recreates the database for the current development workflow.

## Important Local Notes
- `git status` emits warnings about inaccessible global ignore config at `C:\Users\pc/.config/git/ignore`.
- Current `git status -sb` during the 2026-07-07 amendment shows branch `dev...origin/dev` with staged `.gitignore` and memory-bank file changes.
