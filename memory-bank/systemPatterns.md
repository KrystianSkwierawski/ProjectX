# System Patterns

## Repository Layout
- `Client/`: Unity project and generated C# solution files.
- `API/`: .NET solution with layered projects.
- `.Codexrules`: memory bank and agent workflow instructions.
- `.gitignore`: tracked repo-level Visual Studio/.NET-style ignore file.
- `CLAUDE.md` and `.claude/` are not present as of the 2026-07-10 refresh.

## Backend Architecture
- `API/src/API`: ASP.NET Core entrypoint, endpoint mapping, OpenAPI/Swagger, web services.
- `API/src/Application`: MediatR request handlers, validators, DTOs, application services, behaviors.
- `API/src/Domain`: entities, enums, constants, attributes.
- `API/src/Infrastructure`: Entity Framework Core persistence, Identity, JWT authentication, database initialization, migrations.
- `API/tests/UnitTests`: xUnit unit tests.
- EF Core migrations are currently consolidated into `API/src/Infrastructure/Migrations/20260706184932_Init.cs` plus the model snapshot.

## Backend Patterns
- Minimal API endpoint classes under `API/src/API/Endpoints`.
- MediatR for application commands/queries.
- FluentValidation wired through a MediatR validation behavior.
- Logging behavior registered in the MediatR pipeline.
- Character state mutations use `UpdateCharacterCommand` for optional partial updates of health, max health, Strength, Dexterity, Speed, Intellect, Armor, `HelmetType`, `ChestType`, `BootsType`, `WeaponType`, `AmmoType`, and `AmmoCount`.
- Entity Framework Core through `ApplicationDbContext`, with SQL Server by default and in-memory database support through configuration.
- API startup calls `InitialiseDatabaseAsync()`, whose current implementation intentionally deletes and recreates the database with `EnsureDeletedAsync()` / `EnsureCreatedAsync()` and then seeds roles, users, inventory items, quests, and crafting recipes. This is a temporary developer workflow.
- Quests and crafting recipes are defined declaratively through enum attributes and synchronized into the database by `ApplicationDbContextInitialiser`.
- Crafting recipe queries filter active recipes by type and return them ordered by recipe `Id`.
- ASP.NET Core Identity with roles and JWT bearer authentication.
- Authorization policies for server/client roles.
- NSwag exposes API docs at `/api` when enabled.

## Client Architecture
- Unity scenes under `Client/Assets/Scenes`, including Bootstrap, Main, Server, UI, Environment, Audio, and Test scenes.
- Unity Editor automation lives in `Client/Assets/Editor/ProjectXDevAutomation.cs`.
- Local run scripts live in `Client/Automation/` so they are grouped with the Unity client without being imported as Unity assets.
- Runtime scripts under `Client/Assets/Scripts/Areas` are grouped by domain:
  - `Character`: per-client character state, DTOs, runtime stat calculation, player/combat Netcode behavior, and character/gear UI.
  - `Inventory`: inventory DTOs/enums, loot and inventory Netcode behavior, usable items, UI, and subscriptions.
  - `Professions`: crafting/gathering enums, DTOs, managers, Netcode behavior, and UI.
  - `Quest`: quest DTOs, managers, subscriptions, NPC behavior, and UI.
  - `Shared`: cross-domain attributes/enums, managers/singletons, web requests, subscriptions, common MonoBehaviours, and UI helpers.

## Cross-Cutting Patterns
- Client DTO/model names closely mirror backend DTOs and commands.
- Client and backend inventory enums must stay synchronized for persisted items; the client also has an `Xp` pseudo item for crafting/requirement UI. Treat numeric IDs as persisted data and plan migrations before repurposing them.
- `UserManager.Characters` stores `CharacterDto` values by Netcode client ID; owner/local-client lookups should use the appropriate dictionary key rather than a global single-character property.
- Localization resources exist in both API and Unity client paths; English content in client `pl.json` is currently an intentional temporary development fallback.
- New `TranslateKeyEnum` values are append-only: add them at the end of the enum, and keep matching API/client `en.json`/`pl.json` entries appended in the same order as the enum. The 2026-07-09 removal of `Spirit` shifted later client enum values once; do not repeat mid-enum insertion/removal.
- Gear item use is modeled through `AbstractUsableItem` / `AbstractGearUsableItem` with concrete helmet, chest, boots, weapon, and ammo implementations. `UsableItemFromEnum` is passed from inventory/gear UI through the use-item RPC, so `FromInventory` equips/removes inventory items and `FromGear` unequips/returns them; server builds update API `*Type` fields, gear-derived stat totals, and inventory through subscriptions/API calls.
- Runtime stat behavior is centralized in `CharacterStatsCalculator`: each nonnegative Strength point adds 1% fireball damage, each nonnegative Speed point adds 1% code-driven movement speed, Dexterity is dodge chance clamped to 0-100%, and Armor is damage reduction clamped to 0-100% before rounding to integer damage. Intellect is persisted/displayed but has no runtime consumer yet.
- The ammo slot contract is `Character.AmmoType` plus `Character.AmmoCount`. Inventory use moves the whole selected ammo stack into gear, repeated use of the equipped type adds to `AmmoCount` without reapplying its stat bonus, switching types returns the previous whole stack and replaces its bonus, and gear use returns the whole equipped stack to inventory. Ammo is not yet consumed during attacks.
- Ammo content has four tiered families (I/II/III): Arrow applies Dexterity +5/+10/+15, Rune Intellect, Feather Armor, and Oil Strength.
- Gear stat bonuses are defined on Unity `InventoryItemEnum` members via `InventoryItemParametersAttribute`, then reused by inventory, merchant, crafting, and gear previews.
- Gear UI maintains a left panel for equipped slots and a right panel for max health, strength, dexterity, speed, intellect, and armor totals.
- `InventoryUI.PrepareDescription(InventoryItemDto)` is the shared item-preview path for inventory, loot, gear, crafting, and merchant UI; it adds count-aware sell prices for non-Currency/non-`Xp` items and renders declared stat bonuses.
- Ammo crafting recipes are mirrored by API/client `CraftingRecipeEnum`: Arrow, Rune, and Feather tiers use Blacksmithing; Oil tiers use Alchemy. Recipe buttons call `SetAsLastSibling()` when pooled so API `Id` ordering is retained visually.
- Merchant and crafting interaction panels close on Escape or after movement carries the player beyond the relevant interaction distance.
- Character health is synchronized from server-side attack handling through `AttackPlayerSubscription`, a targeted client RPC, and `UpdateCharacterCommand`; max health is part of the character DTO/update contract.
- Character description refresh now runs when opening Character UI and after level-up RPCs.
- Bottom-right quick-access UI is scene-backed in `UIScene` and configured by `QuickAccessUI`, reusing existing slot/preview behavior and icon resources.
- Player locomotion follows the Starter Assets code-driven pattern: `PlayerArmature.prefab` has Animator root motion disabled (`m_ApplyRootMotion: 0`), while `ThirdPersonController` applies movement via `CharacterController.Move(...)` and feeds Animator parameters such as `Speed`, `Grounded`, `Jump`, `FreeFall`, and `MotionSpeed`.
- Generated/build artifacts are present in the workspace; avoid touching `bin/`, `obj/`, Unity `Library/`, and log/cache outputs unless specifically needed.
- The tracked `.gitignore` currently ignores all `*.meta` files. Existing ammo icon metas are present locally but untracked, so future Unity asset work must ensure required GUID/import-setting metas are explicitly versioned.
- Dev startup flow:
  - PowerShell starts/restarts the API executable and auto-builds it if missing.
  - PowerShell builds or reuses the Unity dedicated server executable.
  - If Unity Editor is already open, PowerShell drops request files under `Client/Temp/ProjectXAutomation/`; `ProjectXDevAutomation` handles server build and client Play Mode requests from the open editor.
  - Dedicated server build scenes are read from `Assets/Settings/Build Profiles/DedicatedServer.asset`, with a fallback list matching the current profile order.
