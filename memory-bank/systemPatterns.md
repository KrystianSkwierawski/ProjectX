# System Patterns

## Repository Layout
- `Client/`: Unity project and generated C# solution files.
- `API/`: .NET solution with layered projects.
- `.Codexrules` and `CLAUDE.md`: memory bank and agent workflow instructions.

## Backend Architecture
- `API/src/API`: ASP.NET Core entrypoint, endpoint mapping, OpenAPI/Swagger, web services.
- `API/src/Application`: MediatR request handlers, validators, DTOs, application services, behaviors.
- `API/src/Domain`: entities, enums, constants, attributes.
- `API/src/Infrastructure`: Entity Framework Core persistence, Identity, JWT authentication, database initialization, migrations.
- `API/tests/UnitTests`: xUnit unit tests.

## Backend Patterns
- Minimal API endpoint classes under `API/src/API/Endpoints`.
- MediatR for application commands/queries.
- FluentValidation wired through a MediatR validation behavior.
- Logging behavior registered in the MediatR pipeline.
- Character state mutations use `UpdateCharacterCommand` for optional partial updates of health, stats, and equipped gear.
- Entity Framework Core through `ApplicationDbContext`, with SQL Server by default and in-memory database support through configuration.
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
  - `Subscriptions`: event/subscription handlers for gameplay state changes.
  - `Models`: DTOs and API command/query payloads mirrored from backend contracts.
  - `Mono`: Bootstrap, spawner, audio, quest NPC behavior.

## Cross-Cutting Patterns
- Client DTO/model names closely mirror backend DTOs and commands.
- Localization resources exist in both API and Unity client paths.
- Gear item use is modeled through `AbstractUsableItem` / `AbstractGearUsableItem` with concrete helmet, chest, boots, and weapon implementations; server builds update API character gear and inventory through subscriptions/API calls.
- Character health is synchronized from server-side attack handling through `AttackPlayerSubscription`, a targeted client RPC, and `UpdateCharacterCommand`.
- Bottom-right quick-access UI is scene-backed in `UIScene` and configured by `QuickAccessUI`, reusing existing slot/preview behavior and icon resources.
- Generated/build artifacts are present in the workspace; avoid touching `bin/`, `obj/`, Unity `Library/`, and log/cache outputs unless specifically needed.
- Dev startup flow:
  - PowerShell starts/restarts the API executable and auto-builds it if missing.
  - PowerShell builds or reuses the Unity dedicated server executable.
  - If Unity Editor is already open, PowerShell drops request files under `Client/Temp/ProjectXAutomation/`; `ProjectXDevAutomation` handles server build and client Play Mode requests from the open editor.
  - Dedicated server build scenes are read from `Assets/Settings/Build Profiles/DedicatedServer.asset`, with a fallback list matching the current profile order.
