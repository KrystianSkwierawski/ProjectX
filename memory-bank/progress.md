# Progress

## Current Status
- Memory bank initialized on 2026-05-07.
- Repository contains an existing Unity client and ASP.NET Core backend with major gameplay support areas already scaffolded or implemented.

## What Works / Exists
- Backend solution structure is present.
- API startup configures Kestrel, Serilog, Swagger/NSwag, database initialization, HTTPS redirection, and endpoint mapping.
- Infrastructure configures EF Core, Identity, JWT bearer authentication, authorization policies, and database context services.
- Application layer configures MediatR, FluentValidation, logging and validation behaviors, and translation service.
- Domain entities exist for users, characters, transforms, inventory, quests, crafting recipes, and experience.
- API endpoints exist for users, application users, characters, transforms, inventories, quests, character quests, crafting recipes, character experience, and partial character updates.
- Unity client contains scenes, prefabs, networking scripts, UI scripts, shared managers, API models, and localization resources.
- Character health exists in backend entities/DTOs and Unity client models/UI.
- Character update flow can persist health, stats, and equipped helmet/chest/boots/weapon values.
- Server-side player attack handling reduces health, sends a targeted client RPC, updates player UI/audio/death response, and persists health through the API.
- Gear equip/unequip flow uses concrete usable item classes for helmet, chest, boots, and weapon slots and updates Gear UI plus backend character gear in server builds.
- Local one-command/dev-menu startup automation exists:
  - `Client/Automation/run.bat`
  - `Client/Automation/run.ps1`
  - Unity menu entries `ProjectX > Automation > Run And Build` and `ProjectX > Automation > Run`.
- Scene-backed quick-access bar exists for gear, inventory, character, and chat shortcuts using `QuickAccessUI` and `Resources/Icons/QuickAccess*.png`.

## What's Left / Unknown
- Exact gameplay roadmap and release target are not documented.
- Test coverage depth has not been reviewed yet.
- Current runtime health of the API and Unity client has not been verified during memory-bank initialization.
- Client/server contract drift risk should be checked before API or DTO changes.
- Consumable item persistence needs review: `HealthPotionUsableItem` updates health locally and has a TODO for API persistence, while normal inventory consumption still goes through the usable item base flow.

## Known Issues / Risks
- Secrets or development keys appear in `API/src/API/appsettings.json`; treat as local development values unless the user confirms otherwise.
- Generated Unity and .NET artifacts are present in the workspace, so future searches and edits should avoid build/cache directories.
- `.Codexrules` and `.gitignore` were untracked at initialization.
- Full runtime automation has not been end-to-end verified after the final rename to `run`; only no-op script wiring was smoke-tested.
- `.claude/settings.local.json` is currently untracked and contains local tool configuration with secret-like values; do not commit it or copy its contents into docs.
- `UpdateCharacterCommandHandler` currently resolves the current user's character and has the direct `CharacterId` filter commented out; confirm intended multi-character behavior before expanding character update endpoints.
- Git reports permission warnings for `C:\Users\pc/.config/git/ignore` during status checks.

## Evolution Notes
- 2026-05-07: Initial memory bank created from repo inspection.
- 2026-07-02: Added Unity/API dev stack run automation, moved scripts under `Client/Automation/`, renamed from `serve` to `run`, and exposed Unity menu entries for `Run And Build` and `Run`.
- 2026-07-02: Added a scene-backed bottom-right Unity quick-access bar that reuses `InventorySlot` hover previews, loads quick-access icons from `Resources/Icons`, and toggles gear, inventory, character, and chat UI by mouse click.
- 2026-07-02: Refreshed memory bank to document current health, character update, gear usable item, quick-access, automation, and local-secret status.
