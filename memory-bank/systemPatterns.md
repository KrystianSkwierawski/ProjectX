# System Patterns

## Repository Layout
- `Client/`: Unity project and generated C# solution files.
- `API/`: .NET solution with layered projects.
- `.Codexrules`: Memory bank rules for Codex sessions.

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
- Entity Framework Core through `ApplicationDbContext`, with SQL Server by default and in-memory database support through configuration.
- ASP.NET Core Identity with roles and JWT bearer authentication.
- Authorization policies for server/client roles.
- NSwag exposes API docs at `/api` when enabled.

## Client Architecture
- Unity scenes under `Client/Assets/Scenes`, including Bootstrap, Main, Server, UI, Environment, Audio, and Test scenes.
- Runtime scripts under `Client/Assets/Scripts` are grouped by concern:
  - `Network`: player, enemy, resource gathering, health, crafting, transforms, Netcode behavior.
  - `UI`: inventory, quests, crafting, character, target, cursor, hover, translation UI.
  - `Shared`: managers, singleton helpers, web request helper, grid layout.
  - `Subscriptions`: event/subscription handlers for gameplay state changes.
  - `Models`: DTOs and API command/query payloads mirrored from backend contracts.
  - `Mono`: Bootstrap, spawner, audio, quest NPC behavior.

## Cross-Cutting Patterns
- Client DTO/model names closely mirror backend DTOs and commands.
- Localization resources exist in both API and Unity client paths.
- Generated/build artifacts are present in the workspace; avoid touching `bin/`, `obj/`, Unity `Library/`, and log/cache outputs unless specifically needed.
