# System Patterns

## Repository Layout
- `Client/`: Unity project and generated C# solution files.
- `API/`: .NET solution with layered projects.
- `.Codexrules`: memory bank and agent workflow instructions.
- `.gitignore`: repo-level ignore file is present/staged as of 2026-07-07 with Visual Studio/.NET style rules.
- `CLAUDE.md` is not present as of the 2026-07-06 refresh.

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
- Character state mutations use `UpdateCharacterCommand` for optional partial updates of health, max health, stats, equipped gear, and ammo stack count.
- Entity Framework Core through `ApplicationDbContext`, with SQL Server by default and in-memory database support through configuration.
- API startup calls `InitialiseDatabaseAsync()`, whose current implementation intentionally deletes and recreates the database with `EnsureDeletedAsync()` / `EnsureCreatedAsync()` and then seeds roles, users, inventory items, quests, and crafting recipes. This is a temporary developer workflow.
- ASP.NET Core Identity with roles and JWT bearer authentication.
- Authorization policies for server/client roles.
- NSwag exposes API docs at `/api` when enabled.

## Client Architecture
- Unity scenes under `Client/Assets/Scenes`, including Bootstrap, Main, Server, UI, Environment, Audio, and Test scenes.
- Unity Editor automation lives in `Client/Assets/Editor/ProjectXDevAutomation.cs`.
- Local run scripts live in `Client/Automation/` so they are grouped with the Unity client without being imported as Unity assets.
- Runtime scripts under `Client/Assets/Scripts` are grouped by concern:
  - `Network`: player, enemy, resource gathering, health, crafting, transforms, Netcode behavior.
  - `UI`: inventory, gear, quests, crafting, character, quick access, chat, target, cursor, hover, translation UI.
  - `Shared`: managers, singleton helpers, web request helper, grid layout.
  - `Shared/Attributes`: enum metadata such as `InventoryItemParametersAttribute` for gear stat bonuses.
  - `Subscriptions`: event/subscription handlers for gameplay state changes.
  - `Models`: DTOs and API command/query payloads mirrored from backend contracts.
  - `Mono`: Bootstrap, spawner, audio, quest NPC behavior.

## Cross-Cutting Patterns
- Client DTO/model names closely mirror backend DTOs and commands.
- Client and backend inventory enums must stay synchronized for persisted items; the client also has an `Xp` pseudo item for crafting/requirement UI.
- Localization resources exist in both API and Unity client paths; English content in client `pl.json` is currently an intentional temporary development fallback.
- New `TranslateKeyEnum` values are append-only: add them at the end of the enum, and keep matching API/client `en.json`/`pl.json` entries appended in the same order as the enum.
- Gear item use is modeled through `AbstractUsableItem` / `AbstractGearUsableItem` with concrete helmet, chest, boots, weapon, and ammo implementations; server builds update API character gear, gear-derived stat totals, ammo count, and inventory through subscriptions/API calls.
- The ammo gear slot is represented by `Character.Ammo` plus `Character.AmmoCount`; right-clicking ammo in inventory moves the clicked stack into the slot, adds to the slot when the type matches, and returns the previous ammo stack to inventory when switching ammo type.
- Gear stat bonuses are defined on Unity `InventoryItemEnum` members via `InventoryItemParametersAttribute`, then reused by inventory, merchant, crafting, and gear previews.
- Gear UI maintains a left panel for equipped slots and a right panel for max health, strength, dexterity, speed, intellect, and armor totals.
- Character health is synchronized from server-side attack handling through `AttackPlayerSubscription`, a targeted client RPC, and `UpdateCharacterCommand`; max health is part of the character DTO/update contract.
- Character description refresh now runs when opening Character UI and after level-up RPCs.
- Bottom-right quick-access UI is scene-backed in `UIScene` and configured by `QuickAccessUI`, reusing existing slot/preview behavior and icon resources.
- Player locomotion follows the Starter Assets code-driven pattern: `PlayerArmature.prefab` has Animator root motion disabled (`m_ApplyRootMotion: 0`), while `ThirdPersonController` applies movement via `CharacterController.Move(...)` and feeds Animator parameters such as `Speed`, `Grounded`, `Jump`, `FreeFall`, and `MotionSpeed`.
- Generated/build artifacts are present in the workspace; avoid touching `bin/`, `obj/`, Unity `Library/`, and log/cache outputs unless specifically needed.
- Dev startup flow:
  - PowerShell starts/restarts the API executable and auto-builds it if missing.
  - PowerShell builds or reuses the Unity dedicated server executable.
  - If Unity Editor is already open, PowerShell drops request files under `Client/Temp/ProjectXAutomation/`; `ProjectXDevAutomation` handles server build and client Play Mode requests from the open editor.
  - Dedicated server build scenes are read from `Assets/Settings/Build Profiles/DedicatedServer.asset`, with a fallback list matching the current profile order.
