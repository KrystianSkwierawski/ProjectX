# Progress

## Current Status
- Memory bank initialized on 2026-05-07.
- Memory bank reviewed and refreshed through repository HEAD `518aac4` on 2026-07-10.
- Repository contains an existing Unity client and ASP.NET Core backend with major gameplay support areas already scaffolded or implemented.
- Before this memory-bank edit, local branch `dev` was clean and exactly aligned with `origin/dev`.

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
- Runtime stat calculation exists: Strength scales fireball damage, Dexterity controls dodge chance, Speed scales controller movement, and Armor reduces incoming damage.
- Gear equip/unequip flow uses concrete usable item classes for helmet, chest, boots, and weapon slots and updates Gear UI plus backend character gear/stat totals in server builds.
- Ammo-slot data exists across API and Unity as `AmmoType` plus `AmmoCount`; the count is persisted/network-serialized, seeded, and shown in the ammo gear preview.
- Twelve tiered ammo items exist (Arrow/Rune/Feather/Oil I-III) with mirrored API/client enum values, icons, translations, merchant offers/prices, and +5/+10/+15 stat metadata.
- All 12 ammo items have crafting recipes: Arrow/Rune/Feather use Blacksmithing and Oil uses Alchemy. API results are ordered by recipe `Id`, and pooled client recipe buttons preserve that order.
- Gear stat bonuses are declared on Unity inventory enum values with `InventoryItemParametersAttribute`; inventory, merchant, crafting, and gear previews display those bonuses through `InventoryUI.PrepareDescription(InventoryItemDto)`.
- Shared item previews now accept a full `InventoryItemDto` and display count-aware sell prices in inventory, loot, gear, crafting, and merchant contexts.
- Merchant and crafting panels close on Escape or after the player moves out of interaction range.
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
- Automated coverage is minimal: only `TranslateServiceTests.cs` was found; stat, ammo, crafting, and Unity gameplay paths have no focused tests.
- Current runtime health of the API and Unity client was not verified during the 2026-07-10 memory-bank refresh.
- Client/server contract drift risk should be checked before API or DTO changes.
- Consumable item persistence needs review: `HealthPotionUsableItem` updates health locally and has a TODO for API persistence, while normal inventory consumption still goes through the usable item base flow.
- Base-stat versus gear-derived-stat ownership is not documented; current gear use mutates persisted totals directly.
- Ammo stack equip/unequip transitions, `AmmoCount` mutation/persistence, replaced-stack return, per-attack effects, and per-attack consumption remain to be implemented.
- Intellect is persisted, displayed, and granted by Rune ammo metadata, but has no runtime gameplay consumer.
- Full Polish client localization remains future work; the current English content in `Client/Assets/Resources/i18n/pl.json` is an intentional temporary development fallback.

## Known Issues / Risks
- Secrets or development keys appear in `API/src/API/appsettings.json`; treat as local development values unless the user confirms otherwise.
- Generated Unity and .NET artifacts are present in the workspace, so future searches and edits should avoid build/cache directories.
- A repo-level Visual Studio/.NET-style `.gitignore` is tracked, but generated Unity/.NET artifacts may still exist locally, so searches and edits should keep explicit exclusions.
- Full runtime automation has not been end-to-end verified after the final rename to `run`; only no-op script wiring was smoke-tested.
- `.claude/settings.local.json` was not present during the 2026-07-06 refresh; if it reappears, treat it as local-only secret configuration.
- `UpdateCharacterCommandHandler` currently resolves the current user's character and has the direct `CharacterId` filter commented out; confirm intended multi-character behavior before expanding character update endpoints.
- `AmmoUsableItem` applies/persists Strength, Intellect, and Armor but omits Dexterity, so Arrow I-III advertise Dexterity bonuses in previews without applying them.
- Switching directly between different equipped ammo types adds the new ammo bonus without subtracting the previous bonus and removes one new item without returning the previous ammo item; clicking the same ammo toggles it off and returns only one item.
- `AmmoCount` is not changed by client equip/unequip/attack code, and seeded characters start with `AmmoType = AmmoTemplate` plus `AmmoCount = 1`.
- Tiered ammo repurposed persisted IDs `1010`-`1012` from the former Rune/Feather/Oil meanings. The intentional database recreation masks this during development, but a non-destructive persistence flow will require explicit data migration.
- `.gitignore` ignores `*.meta`; the 13 ammo icon/template meta files exist locally but are untracked, so their Unity GUIDs/import settings are not versioned.
- Merchant offer tooltips use the shared sell-price description while the adjacent currency amount is the full purchase price, so the displayed `Price` inside the tooltip can be misleading.
- Hiding the crafting panel or walking out of range does not stop an in-progress craft; the timer can finish and invoke the server craft after the UI closes.
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
- 2026-07-08: Added percentage-based runtime Strength/Dexterity/Speed/Armor behavior, moved character state to a per-client-ID dictionary, and added initial ammo-slot API/client scaffolding. Documented the append-only translation-key ordering convention.
- 2026-07-09: Renamed character stats and equipped fields to the current Dexterity/Speed and `*Type` contract, removed Spirit, expanded ammo to 12 tiered variants, and added recipes for every ammo type with deterministic API/client display ordering.
- 2026-07-10: Refreshed inventory/loot/gear/crafting/merchant previews to use DTO/count-aware descriptions and sell prices; added merchant/crafting distance/Escape close behavior and substantial `UIScene` layout updates.
- 2026-07-10: Corrected memory-bank ammo claims after static inspection: `AmmoCount` exists in the contract but stack management and attack consumption are not implemented, and Arrow Dexterity bonuses are not applied by the current ammo usable-item path.
