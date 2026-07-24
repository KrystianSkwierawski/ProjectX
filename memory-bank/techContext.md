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
- API startup currently calls database initialization unconditionally; the initializer intentionally deletes and recreates the database before seeding development data as a temporary developer workflow.
- A Debug `dotnet build API/ProjectX.sln` runs the NSwag MSBuild target, which starts the API to generate its specification and therefore also runs the destructive development database initializer. Treat even backend builds as database-resetting operations in the current setup.
- Current EF Core migration history is squashed to `20260706184932_Init` plus `ApplicationDbContextModelSnapshot.cs`.

## Client
- Unity project under `Client/`.
- Unity editor version: `6000.1.15f1`.
- Main generated solutions include `Client/ProjectXClient.sln` and `Client/Client.sln`.
- Uses Unity Netcode for GameObjects, Unity Transport, Multiplayer Play Mode, Dedicated Server package, Input System, URP, Shader Graph, TextMesh Pro/UGUI, Cinemachine, AI Navigation, UniTask, NuGetForUnity, and ParrelSync.

## Common Commands
- Backend build/test:
  - `dotnet build API/ProjectX.sln`
  - `dotnet test API/ProjectX.sln`
- Local dev stack automation:
  - `Client/Automation/run.bat` runs the API, Unity dedicated server, and Unity client Play Mode.
  - `Client/Automation/run.bat -SkipServerBuild` runs using the existing server build.
  - `Client/Automation/run.bat -SkipApi -SkipServerBuild -SkipServerRun -SkipClientPlay` is a safe no-op smoke test for script wiring.
- Unity Editor menu:
  - `ProjectX > Run` invokes `Client/Automation/run.bat -SkipServerBuild`.
  - `ProjectX > Build And Run` invokes `Client/Automation/run.bat`.
- Client validation is expected through Unity Editor/test runner unless project-specific CLI commands are added later.
- The only API test source found during the 2026-07-13 review is `API/tests/UnitTests/Application/TranslateServiceTests.cs`; it currently expands to 182 passing test cases. No gameplay/stat/ammo/gear/potion/crafting tests or Unity test sources were found.

## Current Repo Notes
- At the start of the 2026-07-13 memory-bank refresh, branch `dev` was clean and exactly aligned with `origin/dev` at `8c954ff` (`0` ahead, `0` behind).
- On 2026-07-13, `dotnet build API/ProjectX.sln --no-restore` and `dotnet build Client/Assembly-CSharp.csproj --no-restore` both completed with zero errors. They emitted existing nullable/reference/version-conflict/unused-field warnings. `dotnet test API/ProjectX.sln --no-build --no-restore` then passed all 182 tests. This is build/unit-test validation, not Unity runtime or dedicated-server validation.
- On 2026-07-15, after combat ammo consumption was added, `dotnet build API/ProjectX.sln --no-restore -p:SkipNSwag=True` and `dotnet build Client/Assembly-CSharp.csproj --no-restore` completed with zero errors and existing warnings. `dotnet test API/ProjectX.sln --no-build --no-restore` passed all 190 tests. No full Unity client/dedicated-server/API runtime test was performed.
- On 2026-07-24, after inventory drag-and-drop was added, `dotnet build API/ProjectX.sln --no-restore -p:SkipNSwag=True` and `dotnet build Client/Assembly-CSharp.csproj --no-restore` completed with zero errors and existing warnings, the API specification JSON parsed successfully, and `dotnet test API/ProjectX.sln --no-build --no-restore` passed all 206 tests. Pointer-driven behavior still requires a Unity Play Mode smoke test.
- The repo-level `.gitignore` is tracked.
- Git status commands emit permission warnings for `C:\Users\pc/.config/git/ignore`.

## Constraints And Preferences
- Preserve Unity `.meta` files when moving or adding Unity assets.
- The repo's tracked `.gitignore` currently contains `*.meta`; the known affected project-owned files include 13 ammo icon/template metas and the metas for `CharacterStatsCalculator.cs`, `AmmoUsableItem.cs`, and `UsableItemFromEnum.cs`, while other ignored package/cache metas also exist locally. Correct/narrow that ignore behavior or force-add required metas when asset stability matters.
- Avoid editing generated artifacts and caches such as `Client/Library`, `Client/obj`, `API/**/bin`, `API/**/obj`, and log files.
- Keep API contract changes synchronized with Unity client models and request code.
- Validate JSON localization files when editing i18n resources.
- `run.ps1` auto-builds `API/src/API/API.csproj` in `Debug` when `ProjectX.API.exe` is missing.
- Unity dedicated server builds are generated under `Client/Builds/Server/ProjectXServer.exe`.
- If `.claude/settings.local.json` reappears, treat it as local-only secret configuration and do not commit or quote its values.
- `.Codexrules` is the active memory-bank/agent instruction file; `CLAUDE.md` and `.claude/` were not present during the 2026-07-13 refresh.
- Client `pl.json` currently uses English item/UI text intentionally as a temporary development fallback; do not treat this as an accidental localization bug.
- Recent gameplay/UI changes compile through the generated client project but were not verified end-to-end in a running Unity client/dedicated server/API stack during the 2026-07-13 memory-bank refresh.
