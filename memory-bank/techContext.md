# Tech Context

## Backend
- .NET 10 / ASP.NET Core 10, targeting `net10.0` and default C# 14 through `API/Directory.Build.props`.
- `API/global.json` requests stable SDK `10.0.300` with `latestFeature` roll-forward; current local resolution is 10.0.302.
- `API/ProjectX.slnx` contains API, Application, Domain, Infrastructure, and Domain/Application unit, Infrastructure integration, Web acceptance, and Architecture test projects.
- Central packages are in `API/Directory.Packages.props`; shared test configuration is explicitly imported from `API/tests/TestProject.props`.
- Main dependencies: EF Core/ASP.NET Core 10.0.11, MediatR 14.2.0, FluentValidation 12.1.1, NSwag 14.7.1, Serilog 10 integrations, IdentityModel 8.22.0, xUnit 2.9.3, Moq, and coverlet 10.0.1.
- Repository-local `dotnet-ef` 10.0.11 is pinned in `API/.config/dotnet-tools.json`.

## Backend Runtime
- Migration `20260921161117_AddAccountStash` adds the account-keyed CharacterStashes table, 64-slot capacity default, inventory JSON, revision concurrency token and auditable timestamp.
- Default persistence is local SQL Server database `ProjectX`; Development may use the in-memory provider.
- Development startup intentionally deletes/recreates/seeds the database. Non-Development startup never initializes it; NSwag sets `SkipDatabaseInitialization=true`.
- Local HTTPS endpoint is `https://localhost:5001`; Swagger/root `/api` redirect are Development-only.
- JWT signing material is supplied through .NET User Secrets or external configuration and is never tracked.
- Current migrations are `20260811172103_Init`, `20260824173652_AddCharacterFriendships`, `20260904191036_AddCharacterInventoryTradeReceipts`, `20260912100007_AddCharacterSettings`, and the model snapshot.

## Unity Client And Server
- Dungeon scenes are included in global/client/dedicated build lists. `Prefabs/Locations/Resources/DungeonEnvironmentPrefab.prefab` is shared by the client scene and server runtime instances, with checked-in `DungeonNavMesh.asset`. Re-bake authored geometry via `ProjectX > Bake Dungeon Navigation`. EnvironmentScene now includes NPC/crafting and server spawners; TestScene/ServerScene were removed from assets/startup/build profiles. Batch Unity runs need the normal user context outside the sandbox for licensing; an isolated project successfully baked and validated dungeon navigation. Migration `20260914162935_AddCharacterTransformScene` adds the resumable scene to character transform history, with `EnvironmentScene` as the existing-row default.
- Unity `6000.1.15f1` under `Client/`; generated solutions include `ProjectXClient.sln` and `Client.sln`.
- Key packages: Netcode for GameObjects 2.4.4, Unity Transport 2.5.3, Multiplayer Services 1.2.0, Multiplayer Play Mode 1.6.2, Dedicated Server 1.6.2, Input System, URP, TextMesh Pro/UGUI, Cinemachine, AI Navigation, UniTask, NuGetForUnity, and ParrelSync.
- MPS stays at 1.2.0 because 2.1.2 removed Multiplay editor types required by Multiplayer Play Mode 1.6.x.
- Production networking defaults to Relay + DTLS; local automation opts into Direct transport through `PROJECTX_USE_DIRECT_TRANSPORT=true`.

## Common Workflows
- Backend: from `API/`, run `dotnet build ProjectX.slnx`, `dotnet test ProjectX.slnx`, and `dotnet format ProjectX.slnx --no-restore` when relevant.
- EF validation: `dotnet tool restore`, then `dotnet tool run dotnet-ef migrations has-pending-model-changes ...` with SQL Server configuration.
- Client compile checks use generated `Client/Assembly-CSharp.csproj`; networking changes should compile once with `UNITY_SERVER` and once without it.
- `Client/Automation/run.bat` starts missing API/server/client parts; `-RestartExisting` explicitly restarts managed processes, `-SkipServerBuild` reuses the build, and the full skip set is a safe wiring no-op.
- Unity menu `ProjectX > Run` uses `-SkipServerBuild`; `ProjectX > Build And Run` performs the full flow. Keep menu automation aligned with `Client/Automation/run.ps1`.
- Each editor/server run writes timestamped diagnostics under ignored `Client/Logs/Runtime`; the server mirrors logs through `PROJECTX_RUNTIME_LOG_PATH` without suppressing Unity console output.

## Validation And Tool Preferences
- Apply proportional engineering: this is a game, not a banking system. Not every rare edge case needs full handling. Prioritize normal gameplay and realistic player-impacting bugs; avoid adding complexity or blocking reviews for hypothetical scenarios whose cost outweighs their practical benefit.
- Prefer code inspection, builds, tests, logs, static prefab checks, and generated previews.
- Use `computer-use` only when direct interaction with a running UI is necessary or materially simplifies implementation or verification.
- Generated-project compilation does not prove pointer behavior, scene layout, networking, or gameplay timing; record required Play Mode/full-stack smoke tests explicitly.
- Latest trade baseline: 386 backend tests passed after durable receipt-resolution and atomic two-character Collect-progress coverage; API build/OpenAPI/format/EF checks passed. Unity import/Netcode processing and client/server-symbol builds passed; mutation draining, Collect-write ordering across participant despawn, server-lifetime commit coordination, versioned snapshot application, server-only receipt resolution, deferred credential revocation, raw-entry offer bounds, spacing, and Loot-collision changes pass both generated-project compile variants, with static prefab checks for layout. Live authenticated two-client trade remains untested.

## Repository Constraints
- Preserve Unity `.meta` files and synchronized API/client enums, DTOs, OpenAPI, and localization resources.
- Avoid generated/cache outputs (`bin`, `obj`, Unity `Library`, logs) unless validation specifically requires them.
- Validate modified JSON resources.
- Root `AGENTS.md` is the repository instruction file; Memory Bank is read on demand according to its routing table. `.claude/settings.local.json`, if it reappears, is secret local configuration and must not be committed or quoted.
- Git status emits a known permission warning for `C:/Users/pc/.config/git/ignore`.

- All prefab assets live under `Assets/Prefabs`: project modules include Character, Combat, Inventory, Professions, Spawning, Locations, Loading, Authentication, Quest, Merchant, Chat, Shared/UI, ActionBars, Friends, Party and Trade. Imported package prefabs are under ThirdParty with their original package hierarchy. Location environment prefabs remain in `Prefabs/Locations/Resources` so `Resources.Load` names are unchanged; the NavMesh baker uses the new asset path. Moves preserve prefab bytes and .meta GUIDs.

- Quest objectives validation (2026-10-28): 631 backend tests passed, including independent mixed-goal persistence across reload, regression after item loss, atomic multi-item consumption/rewards, forged finished-state rejection, unknown objective rejection and OpenAPI contract checks. That validation used command-line NuGetAudit=false because System.Security.Cryptography.Pkcs 9.0.11 audit findings blocked ordinary restore. The dependency is now explicitly pinned to patched 9.0.21 in Infrastructure through central package management; a fresh NuGet audit including transitive packages reports no vulnerable packages across the solution, with audit policy unchanged. Both generated Unity compile variants passed. Isolated Unity import/Netcode and QuestDetailsAuthoring.Validate cover visible icon tint/count fields, mixed goal rows, action states, PL/EN text/layout, narrow canvas bounds and matching section spacing; rendered panel previews are generated under the isolated project's Logs. SQL Server-configured has-pending-model-changes passed. The migration was not applied to a database; authenticated gameplay, actual pointer/scroll behavior, trade timing and reconnect during rewards remain untested.

- Quest/trade review fixes (2026-10-29): both generated Unity client/server define builds passed (0 errors, 10 existing warnings each). The InventoryRegression executable passed gear, ammo and TRADE_QUEST_REGRESSION_PASSED checks using production trade state for deferred admission, commit draining, credential retention and scope cleanup. Isolated Unity import/Netcode processing and QuestDetailsAuthoring.Validate passed, including description changes in both status directions and existing PL/EN layout checks. These probes do not verify authenticated two-client trade/party-kill timing or real disconnect/HTTP recovery.
