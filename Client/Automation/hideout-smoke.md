# Hideout smoke coverage

Implemented: private character-owned HideoutScene through DungeonTravel; ChamomileFarm costs 5 Chamomile and takes 5 seconds from the API catalog. Enter through HideoutPortal near (8, 1.4, 4) in EnvironmentScene. The catalog default is 5 seconds; migration SetHideoutBuildTimeToFiveSeconds updates existing catalog rows without changing accepted deadlines. Build via right-click within 5 units and the reused CraftingCanvas. Respawn interval is 5 seconds in Spawner Inspector.

Automated verification: API build/OpenAPI, all backend tests, SQL Server pending-model check, generated client and UNITY_SERVER compilation, isolated Unity import/Netcode processing, prefab/navigation and unbuilt/construction/completed view probes. View probes run in the editor and do not prove live networking, gathering or respawn timing.

Still required with a rebuilt API/server and two authenticated clients:

1. Enter as solo players and as party members. Verify distinct rooms, no other player's character or plants, and no change of ownership after joining/leaving a party.
2. Open construction at close range. Check materials, farm product icon, 5-second duration, hover cursor, close/Escape/range behavior and landscape/portrait layouts.
3. Try with 4 Chamomile, then 5. On success verify one record, exact material consumption with stable slots and Collect Finished -> Accepted regression. Reject distant, dead, travelling, trade-reserved and duplicate requests.
4. During construction check the transparent blockout, white translucent outlined countdown, disabled station and inactive spawner. Leave/re-enter while building; countdown must resume from the API deadline.
5. Leave until after completion and return. Verify opaque farm, hidden countdown, gathering and replacement Chamomile after 5 seconds. Another player's room must not receive or expose the plant.
6. Restart the dedicated server and reconnect; re-enter via the world portal. Repeat with the API backed by the same retained SQL database. Confirm timestamps and completion remain unchanged.
7. Disconnect or leave during an accepted build request. Re-enter and read the durable result before retrying. Verify only one charge/build; test overlapping inventory/Collect writes and trade locking.

The existing Development API startup intentionally deletes/recreates/seeds the entire database. It also deletes Hideout records. Persistence across API restarts must be tested with retained SQL data and database initialization disabled (or outside Development). Hideout does not change this global development policy. Apply migration `20260929150731_AddCharacterHideout` to an existing retained database; it inserts the initial farm definition. No migration was applied to the normal local database during implementation.

Follow-up verification: backend Release build and all 463 tests pass (the running Debug API locked its output files); SQL model check and generated Unity client/server builds pass. Isolated Unity import/Netcode plus slot geometry probes passed for 60px slots at section widths 385/240/120 and counts 0/5, 5/5, 1024/5. Farm selection is green and uses Icons/ChamomileFarm.png. Repeat live re-entry and gather/leave/wait/return to verify the snapshot timing and owner-connected room retention end to end. Existing sessions need rebuilt binaries and the updated API catalog; the running API was not stopped and migrations were not applied to its database.

Farm preview verification: editor pointer-enter/exit events show and hide the localized title and description; releasing and reusing the slot refreshes the tooltip correctly. These event probes passed despite editor-only DontDestroyOnLoad diagnostics from UIManager initialization; actual Play Mode pointer placement and tooltip visibility remain part of the runtime smoke.

Catalog definitions now come from HideoutBuildingParameters on each non-None HideoutBuildingEnum value; seed enumerates those definitions just like quests. The five-second migration updates only catalog BuildTime, preserving accepted construction deadlines.

Farm spawn height: ChamomileFarm overrides only its nested spawner's _transformY to 0.065 (world Y); the base ChamomileSpawner prefab retains 1.423. The server loads HideoutEnvironmentPrefab, so scene-only client overrides do not affect authoritative spawning. Rebuild/restart the dedicated server to load this asset change.
