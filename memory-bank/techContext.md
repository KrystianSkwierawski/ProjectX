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
- The NSwag MSBuild target sets `SkipDatabaseInitialization=true`, so OpenAPI regeneration no longer runs the destructive development database initializer. Normal API startup still initializes and recreates the development database unless that configuration flag is explicitly set.
- Current EF Core migration history is squashed to `20260810173356_INIT` plus `ApplicationDbContextModelSnapshot.cs`; application-owned date columns are created directly as `datetimeoffset`.

## Client
- Unity project under `Client/`.
- Unity editor version: `6000.1.15f1`.
- Main generated solutions include `Client/ProjectXClient.sln` and `Client/Client.sln`.
- Uses Unity Netcode for GameObjects 2.4.4, Unity Transport 2.5.3, Multiplayer Services 1.2.0 (Relay/Authentication/Core), Multiplayer Play Mode 1.6.2, Dedicated Server 1.6.2, Input System, URP, Shader Graph, TextMesh Pro/UGUI, Cinemachine, AI Navigation, UniTask, NuGetForUnity, and ParrelSync. MPS 1.2.0 is intentionally pinned because MPS 2.1.2 removes Multiplay editor types still required by Multiplayer Play Mode 1.6.x.

## Common Commands
- Backend build/test:
  - `dotnet build API/ProjectX.sln`
  - `dotnet test API/ProjectX.sln`
- Local dev stack automation:
  - `Client/Automation/run.bat` runs missing parts of the API, Unity dedicated server, and Unity client Play Mode stack. It reuses an API already listening on the configured port and skips both build and startup when the configured dedicated-server executable is already running.
  - `Client/Automation/run.bat -RestartExisting` explicitly restarts processes managed from the configured API/server executable paths before continuing.
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
- On 2026-07-27, after Loot -> Inventory drag-and-drop and correct Inventory/Gear source placeholders were added, `dotnet build Client/Assembly-CSharp.csproj --no-restore` completed with zero errors and the six existing warnings. Pointer-driven behavior still requires a Unity Play Mode smoke test.
- On 2026-08-05, after the login security review fixes, API and Unity runtime/editor projects compiled with zero errors, NSwag regenerated the login `200/400/401/429` contract without initializing the database, and all 215 API tests passed.
- On 2026-08-05, the full 16-operation NSwag contract was documented and regression-tested, application validation/not-found errors were normalized to ProblemDetails, API tests passed all 220 cases, and the generated Unity client project compiled with zero errors and its existing dependency/unused-field warnings.
- On 2026-08-05, after the global LoadingScene/loading-scope implementation, `dotnet build Client/Assembly-CSharp.csproj --no-restore` and `dotnet build Client/Assembly-CSharp-Editor.csproj --no-restore` completed with zero errors and the existing warnings. Static prefab/scene local-reference and GUID checks passed. A Unity batch import/Play Mode run was not possible because no valid headless Editor license was available.
- On 2026-08-10, strict one-hour JWT validation, automatic 55-minute client/server refresh, and a five-minute old/new overlap were added. The API now derives the authenticated token's UTC expiry from its `exp` claim and rejects refresh attempts before that final window. The earlier per-client JWT relay through the dedicated server was subsequently replaced by server-authenticated `PlayerSessionId` credentials, so client JWTs remain local to their process. Tests cover early refresh rejection, the 55/60-minute boundary, and legacy/overlong token rejection; a long-duration end-to-end runtime soak test is still pending.
- On 2026-08-10, after UTC `DateTimeOffset` domain fields, singleton `TimeProvider`, the EF auditable interceptor, and the regenerated initial migration were added, the API suite passed all 228 tests and the Unity runtime project compiled with zero errors. Current UI contracts expose no timestamps; future date presentation should call local-time conversion only in the UI layer.
- On 2026-08-10, migrations were regenerated as the single fresh `20260810173356_INIT` baseline, removing the unsafe legacy `datetime2` conversion path. The application columns are directly declared as `datetimeoffset`; remaining `datetime2` columns are framework-owned Identity/OpenIddict persistence fields.
- On 2026-08-10, terminal session-refresh handling was added: client 4xx responses log out immediately, transient failures retry only through the remaining five-minute window, client networking/scenes are torn down before returning to Bootstrap login, and dedicated-server builds exit for supervised restart. API and Unity runtime builds completed with zero errors, and all 228 API tests passed.
- On 2026-08-10, Relay/DTLS and ticket admission were added together with a 90-second UTC server lease/heartbeat and ticket throttling. The backend and regenerated 22-operation OpenAPI contract build with zero warnings/errors and all 259 API tests pass. An initial MPS 2.1.2 pin compiled the game assembly but broke the Multiplayer Play Mode editor assembly because required Multiplay types were removed; Unity now successfully resolves the compatible MPS 1.2.0 line, which retains those types and lower-level Relay APIs while supporting UTP 2.5.x. Multiplayer Play Mode and Dedicated Server are aligned at 1.6.2. A live Relay runtime test remains pending UGS project/build-profile linkage.
- The repo-level `.gitignore` is tracked.
- Git status commands emit permission warnings for `C:\Users\pc/.config/git/ignore`.

## Constraints And Preferences
- Preserve Unity `.meta` files when moving or adding Unity assets.
- The repo's tracked `.gitignore` does not currently exclude `*.meta`; keep required Unity metadata versioned with every new or moved asset/script.
- Avoid editing generated artifacts and caches such as `Client/Library`, `Client/obj`, `API/**/bin`, `API/**/obj`, and log files.
- Keep API contract changes synchronized with Unity client models and request code.
- Validate JSON localization files when editing i18n resources.
- `run.ps1` auto-builds `API/src/API/API.csproj` in `Debug` when `ProjectX.API.exe` is missing.
- Unity dedicated server builds are generated under `Client/Builds/Server/ProjectXServer.exe`.
- If `.claude/settings.local.json` reappears, treat it as local-only secret configuration and do not commit or quote its values.
- `.Codexrules` is the active memory-bank/agent instruction file; `CLAUDE.md` and `.claude/` were not present during the 2026-07-13 refresh.
- Client `pl.json` currently uses English item/UI text intentionally as a temporary development fallback; do not treat this as an accidental localization bug.
- Recent gameplay/UI changes compile through the generated client project but were not verified end-to-end in a running Unity client/dedicated server/API stack during the 2026-07-13 memory-bank refresh.
