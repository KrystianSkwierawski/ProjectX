# Progress

## Current Status
- Memory bank initialized on 2026-05-07.
- Memory bank refreshed on 2026-07-06 and amended on 2026-07-07 and 2026-07-08.
- Repository contains an existing Unity client and ASP.NET Core backend with major gameplay support areas already scaffolded or implemented.
- Current local branch is `dev`, one commit ahead of `origin/dev` before this memory-bank edit.

## What Works / Exists
- Backend solution structure is present.
- API startup configures Kestrel, Serilog, Swagger/NSwag, database initialization, HTTPS redirection, and endpoint mapping.
- Infrastructure configures EF Core, Identity, JWT bearer authentication, authorization policies, and database context services.
- Application layer configures MediatR, FluentValidation, logging and validation behaviors, and translation service.
- Domain entities exist for users, characters, transforms, inventory, quests, crafting recipes, and experience.
- API endpoints exist for users, application users, characters, transforms, inventories, quests, character quests, crafting recipes, character experience, and partial character updates.
- Unity client contains scenes, prefabs, networking scripts, UI scripts, shared managers, API models, and localization resources.
- Character health and max health exist in backend entities/DTOs and Unity client models/UI.
- Character update flow can persist health, max health, stats, armor, and equipped helmet/chest/boots/weapon values.
- Server-side player attack handling reduces health, sends a targeted client RPC, updates player UI/audio/death response, and persists health through the API.
- Gear equip/unequip flow uses concrete usable item classes for helmet, chest, boots, and weapon slots and updates Gear UI plus backend character gear/stat totals in server builds.
- Ammo/special gear slot support exists as an equip-only character slot across API and Unity client; ammo items can currently be equipped and sold by the merchant, but do not yet affect attacks or get consumed.
- Gear stat bonuses are declared on Unity inventory enum values with `InventoryItemParametersAttribute`; inventory, merchant, crafting, and gear previews display those bonuses through `InventoryUI.PrepareDescription()`.
- Character UI has a `RefreshDescription()` path used when opening the character panel and when level changes arrive from the server.
- API migrations are currently consolidated to a single `20260706184932_Init` migration plus the snapshot.
- Local one-command/dev-menu startup automation exists:
  - `Client/Automation/run.bat`
  - `Client/Automation/run.ps1`
  - Unity menu entries `ProjectX > Run` and `ProjectX > Build And Run`.
- Scene-backed quick-access bar exists for gear, inventory, character, and chat shortcuts using `QuickAccessUI` and `Resources/Icons/QuickAccess*.png`.
- Player locomotion is currently code-driven/root-motion OFF: `PlayerArmature.prefab` stores Animator `m_ApplyRootMotion: 0`, and movement is applied in `ThirdPersonController` through `CharacterController.Move(...)`.

## What's Left / Unknown
- Exact gameplay roadmap and release target are not documented.
- Test coverage depth has not been reviewed yet.
- Current runtime health of the API and Unity client has not been verified during memory-bank initialization.
- Client/server contract drift risk should be checked before API or DTO changes.
- Consumable item persistence needs review: `HealthPotionUsableItem` updates health locally and has a TODO for API persistence, while normal inventory consumption still goes through the usable item base flow.
- Base-stat versus gear-derived-stat ownership is not documented; current gear use mutates persisted totals directly.
- Ammo item attack effects and stack consumption are future work; current implementation only adds the gear slot and equip flow.
- Full Polish client localization remains future work; the current English content in `Client/Assets/Resources/i18n/pl.json` is an intentional temporary development fallback.

## Known Issues / Risks
- Secrets or development keys appear in `API/src/API/appsettings.json`; treat as local development values unless the user confirms otherwise.
- Generated Unity and .NET artifacts are present in the workspace, so future searches and edits should avoid build/cache directories.
- A repo-level `.gitignore` is staged as of the 2026-07-07 amendment and appears to use Visual Studio/.NET style ignore rules; generated Unity/.NET artifacts may still exist locally, so searches and edits should keep explicit exclusions.
- Full runtime automation has not been end-to-end verified after the final rename to `run`; only no-op script wiring was smoke-tested.
- `.claude/settings.local.json` was not present during the 2026-07-06 refresh; if it reappears, treat it as local-only secret configuration.
- `UpdateCharacterCommandHandler` currently resolves the current user's character and has the direct `CharacterId` filter commented out; confirm intended multi-character behavior before expanding character update endpoints.
- Git reports permission warnings for `C:\Users\pc/.config/git/ignore` during status checks.
- API startup currently calls `InitialiseDatabaseAsync()` unconditionally; the initializer uses `EnsureDeletedAsync()` and `EnsureCreatedAsync()`. This database reset is intentional and temporary for developer work, but should be revisited before preserving real data matters.
- `PlayerUI.SetPlayer()` currently calls `SetHealth()` and then `SetMaxHealth()` on the same text field, so the initial displayed value may be max health rather than current health.

## Evolution Notes
- 2026-05-07: Initial memory bank created from repo inspection.
- 2026-07-02: Added Unity/API dev stack run automation, moved scripts under `Client/Automation/`, renamed from `serve` to `run`, and exposed Unity menu entries for `Run` and `Build And Run`.
- 2026-07-02: Added a scene-backed bottom-right Unity quick-access bar that reuses `InventorySlot` hover previews, loads quick-access icons from `Resources/Icons`, and toggles gear, inventory, character, and chat UI by mouse click.
- 2026-07-02: Refreshed memory bank to document current health, character update, gear usable item, quick-access, automation, and local-secret status.
- 2026-07-06: Refreshed memory bank to document `.Codexrules`, consolidated migrations, max health, gear stat attributes/previews, character description refresh, database reset behavior, and current localization/UI risks.
- 2026-07-07: User clarified that database reset on startup and English text in client `pl.json` are intentional temporary development choices.
- 2026-07-08: Confirmed and documented that player animation/locomotion uses Animator root motion OFF, with movement driven by `ThirdPersonController`/`CharacterController`.
- 2026-07-08: Added equip-only ammo/special gear slot support and documented translate ordering convention: new `TranslateKeyEnum` values and matching `en.json`/`pl.json` entries should be appended at the end in enum order to avoid renumbering or localization-order drift.
