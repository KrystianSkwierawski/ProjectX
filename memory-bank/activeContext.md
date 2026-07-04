# Active Context

## Current Focus
- Memory bank refreshed on 2026-07-02 at user request.
- No feature implementation task is currently active after this documentation refresh.

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

## Active Decisions
- Treat the repository as two cooperating applications:
  - `Client/`: Unity 6000.1 project using Netcode for GameObjects.
  - `API/`: ASP.NET Core API using clean/layered architecture.
- Preserve existing project organization and naming patterns when making future changes.
- Read all memory bank files at the start of every task, per `.Codexrules` / `CLAUDE.md`.
- Local dev startup automation lives inside the Unity project under `Client/Automation/`, not at repository root.
- Unity menu entries under `ProjectX` should remain aligned with `Client/Automation/run.bat`.
- Treat `.claude/settings.local.json` as local-only secret configuration.

## Next Steps
- For future implementation tasks, first refresh this file with the current working focus.
- Confirm product-level goals with the user when changes require design or gameplay decisions not already implied by code.
- Keep `progress.md` updated after meaningful changes.
- When changing startup/build behavior, update both `Client/Automation/run.ps1` and `Client/Assets/Editor/ProjectXDevAutomation.cs`.
- Clarify whether consumable health restoration should persist through `UpdateCharacterCommand`; current `HealthPotionUsableItem` contains a TODO and updates health locally before normal inventory consumption.

## Important Local Notes
- Current `git status --short --untracked-files=all` shows only `.claude/settings.local.json` as untracked, plus Git warnings about inaccessible global ignore config at `C:\Users\pc/.config/git/ignore`.
