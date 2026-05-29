# Tech Context

## Backend
- .NET / ASP.NET Core solution: `API/ProjectX.sln`.
- Projects:
  - `API/src/API/API.csproj`
  - `API/src/Application/Application.csproj`
  - `API/src/Domain/Domain.csproj`
  - `API/src/Infrastructure/Infrastructure.csproj`
  - `API/tests/UnitTests/UnitTests.csproj`
- Central package management via `API/Directory.Packages.props`.
- Key packages include Entity Framework Core 9, ASP.NET Core Identity, MediatR 13, FluentValidation, NSwag, Serilog, JWT token libraries, xUnit, Moq, and coverlet.

## Backend Runtime Configuration
- Default connection string points to local SQL Server database `ProjectX`.
- `UseInMemoryDatabase` can switch persistence to an in-memory database.
- Kestrel HTTPS endpoint is configured for `https://localhost:5001`.
- Swagger/OpenAPI UI is enabled by `API:SwaggerEnabled` and mounted at `/api`.
- Serilog writes to console and daily rolling file logs.

## Client
- Unity project under `Client/`.
- Unity editor version: `6000.1.15f1`.
- Main generated solutions include `Client/ProjectXClient.sln` and `Client/Client.sln`.
- Uses Unity Netcode for GameObjects, Unity Transport, Multiplayer Play Mode, Dedicated Server package, Input System, URP, Shader Graph, TextMesh Pro/UGUI, Cinemachine, AI Navigation, UniTask, NuGetForUnity, and ParrelSync.

## Common Commands
- Backend build/test:
  - `dotnet build API/ProjectX.sln`
  - `dotnet test API/ProjectX.sln`
- Client validation is expected through Unity Editor/test runner unless project-specific CLI commands are added later.

## Constraints And Preferences
- Preserve Unity `.meta` files when moving or adding Unity assets.
- Avoid editing generated artifacts and caches such as `Client/Library`, `Client/obj`, `API/**/bin`, `API/**/obj`, and log files.
- Keep API contract changes synchronized with Unity client models and request code.
- Validate JSON localization files when editing i18n resources.
