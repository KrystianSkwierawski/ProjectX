# System Patterns

## Repository Layout
- `Client/`: Unity project and generated C# solution files.
- `API/`: .NET solution with layered projects.
- `.Codexrules`: memory bank and agent workflow instructions.
- `.gitignore`: tracked repo-level Visual Studio/.NET-style ignore file.
- `CLAUDE.md` and `.claude/` are not present as of the 2026-07-13 refresh.

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
- Logging behavior registered in the MediatR pipeline. It renders command/query payloads through their own `ToString()` implementation; concrete MediatR requests are records (or provide an equivalent explicit override), and a convention test enforces that every request declares `ToString()`. Types carrying credentials or tokens must override `ToString()` so those values are omitted entirely; there is no marker-based redaction mechanism.
- Every Minimal API operation declares summary, description, request/parameter/response documentation beside its route mapping. `OpenApiDocumentationOperationProcessor` adds the shared authorization responses and policy text, while `OpenApiSchemaDocumentationProcessor` supplies schema/property descriptions, examples, formats, and required-property metadata. Contract tests enforce complete metadata, stable operation IDs, bearer-JWT security, and an anonymous login operation across the generated specification.
- `ApiExceptionHandler` maps application validation and not-found exceptions to RFC 7807 responses. Application queries use `SingleOrNotFoundAsync` / `FirstOrNotFoundAsync` when absence is a normal `404`, while inconsistent duplicate data continues to surface as a server error.
- Character state mutations use `UpdateCharacterCommand` for optional partial updates of health, max health, Strength, Dexterity, Speed, Intellect, Armor, `HelmetType`, `ChestType`, `BootsType`, `WeaponType`, `AmmoType`, and `AmmoCount`. Ammo fields are persisted exactly as submitted; ammo-consumption and zero-count unequip rules belong to the Unity server rather than the API handler.
- Entity Framework Core through `ApplicationDbContext`, with SQL Server by default and in-memory database support through configuration.
- API startup calls `InitialiseDatabaseAsync()`, whose current implementation intentionally deletes and recreates the database with `EnsureDeletedAsync()` / `EnsureCreatedAsync()` and then seeds roles, users, inventory items, quests, and crafting recipes. This is a temporary developer workflow.
- Quests and crafting recipes are defined declaratively through enum attributes and synchronized into the database by `ApplicationDbContextInitialiser`.
- Crafting recipe queries filter active recipes by type and return them ordered by recipe `Id`.
- ASP.NET Core Identity with roles and JWT bearer authentication.
- Login command/response `ToString()` implementations omit the email, password, and token rather than redacting whole payloads through a marker interface. Login failures use Identity access-failure tracking with a five-attempt/five-minute lockout and the endpoint has a per-IP fixed-window rate limit of five requests per minute. The limiter partitions requests using `HttpContext.Connection.RemoteIpAddress`; deployments behind a reverse proxy, load balancer, or CDN must configure trusted forwarded headers before rate limiting so this value represents the real client IP rather than the shared proxy IP.
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
- Author UI structures as reusable prefabs placed in Unity scenes. Classes named `*UI.cs` should resolve their required scene/prefab elements from the transform hierarchy during startup and cache those references in fields, following `LoginUI`; do not expose every child control as a serialized Inspector field. Serialized fields remain appropriate for external prefab/configuration assets that cannot be obtained from the owned hierarchy. Avoid assembling UI hierarchies ad hoc in runtime code unless the UI is genuinely dynamic; repeated dynamic elements should normally be instantiated from prefabs.
- Responsive layout is required for every new or modified Unity UI. Use suitable `RectTransform` anchors/stretch and layout components inside the prefab, define the scene's `CanvasScaler` behavior explicitly, preserve usable margins on narrow screens, and validate representative landscape, portrait/narrow, and—where relevant—4:3 or ultrawide proportions. Treat fixed pixel dimensions as preferred or bounded sizes rather than the sole layout mechanism.
- Client and backend inventory enums must stay synchronized for persisted items; the client also has an `Xp` pseudo item for crafting/requirement UI. Treat numeric IDs as persisted data and plan migrations before repurposing them.
- `UserManager.Characters` stores `CharacterDto` values by Netcode client ID; owner/local-client lookups should use the appropriate dictionary key rather than a global single-character property.
- Localization resources exist in both API and Unity client paths; English content in client `pl.json` is currently an intentional temporary development fallback.
- New `TranslateKeyEnum` values are append-only: add them at the end of the enum, and keep matching API/client `en.json`/`pl.json` entries appended in the same order as the enum. The 2026-07-09 removal of `Spirit` shifted later client enum values once; do not repeat mid-enum insertion/removal.
- Gear item use is modeled through `AbstractUsableItem` / `AbstractGearUsableItem` with concrete helmet, chest, boots, weapon, and ammo implementations. `UsableItemFromEnum.Inventory` or `.Gear` is passed from inventory/gear UI through the use-item RPC. The base gear class removes the old item's six declared stat bonuses, applies the incoming bonuses, persists the full gear/stat state, and coordinates inventory transfers; concrete gear classes identify their current item, UI slot, template, and wear/unwear mutation.
- Runtime stat behavior is centralized in `CharacterStatsCalculator` as `CharacterDto` extensions: `ApplyWeaponDamage`, `IsAttackDodged`, `ApplySpeed`, and `ApplyArmor` are invoked as `character.Method(...)`. `ApplyWeaponDamage` adds 1% outgoing fireball damage per nonnegative point of the equipped weapon's mapped stat (Iron Sword Strength, Iron Wand Intellect, Iron Bow Dexterity), while empty/unknown weapon types use base damage. Each nonnegative Speed point adds 1% code-driven movement speed, Dexterity is dodge chance clamped to 0-100%, and Armor is damage reduction clamped to 0-100% before rounding to integer damage. Private `short` extensions implement the shared increase-multiplier and limited-percent calculations.
- The ammo slot contract is `Character.AmmoType` plus `Character.AmmoCount`. `InventoryItemParametersAttribute.WeaponCategory` is the single compatibility map for both a weapon's category and ammo's required category: Arrows require Bow, Runes require Wand, and Feathers/Oils require Sword. Compatibility checks compare those metadata values directly. Incompatible ammo equip returns without mutation. Equipping a different-category weapon or removing the weapon auto-unequips ammo, removes its stats, clears the Gear slot, returns the full stack, and persists weapon/ammo state in the same transition. First equip, same-type merge, type switch, and explicit unequip also preserve whole stacks and refresh Gear UI with the current DTO. Combat consumes one Feather after a positive, non-dodged incoming hit or one Arrow/Rune/Oil after outgoing damage calculation. The last unit contributes to that hit, then its item parameters are subtracted, `AmmoType` becomes `AmmoTemplate`, and the server synchronizes the resulting DTO to the owner and API.
- Ammo content has four tiered families (I/II/III): Arrow applies Dexterity +5/+10/+15, Rune Intellect, Feather Armor, and Oil Strength.
- Gear stat bonuses are defined on Unity `InventoryItemEnum` members via `InventoryItemParametersAttribute`, then reused by inventory, merchant, crafting, and gear previews.
- Weapon usable-item dispatch uses `InventoryItemEnum.IsWeapon()`, so future weapons should be added to that classifier instead of receiving another `CharacterInventory` switch arm. Current iron weapons all grant +20 through metadata: `IronSword` Strength, `IronWand` Intellect, and `IronBow` Dexterity. Their fireball damage scaling uses the same respective stat through `ApplyWeaponDamage`; attack behavior otherwise remains shared.
- Gear UI maintains a left panel for equipped slots and a right panel for max health, strength, dexterity, speed, intellect, and armor totals.
- `InventoryUI.PrepareDescription(InventoryItemDto)` is the shared item-preview path for inventory, loot, gear, crafting, and merchant UI; it adds count-aware sell prices for non-Currency/non-`Xp` items, renders declared stat bonuses, and shows the localized required weapon category for ammo.
- Inventory drag-and-drop is owner-optimistic and server/API-persisted. `InventoryUI` owns the shared drag state and instantiates the serialized, override-sorted `InventoryDragPreview` prefab as a non-raycastable cursor-following icon/count preview. Inventory-to-inventory drops submit source/target indices through `MoveInventorySubscription`: different types swap, identical types merge, and empty targets retain their exact index. `GearUI` registers its five slots with the same drag lifecycle; any Gear slot is a shared target for equippable items, and the existing usable-item dispatch selects the actual equipment slot. Inventory ↔ Gear drops reuse `UseItemSubscribtion` with the correct `UsableItemFromEnum` so gear swaps, ammo stacks, weapon compatibility, stat updates, and persistence remain authoritative. `InventoryItemEnum.None`/count `0` entries are stable empty-slot placeholders; full removal clears a slot, while add and split operations prefer existing empty slots.
- The shared drag lifecycle replaces the source item's `RawImage` texture/color and hides its count mesh only after the preview prefab is instantiated, without disabling the source GameObject or its raycasts. Inventory-style sources use a null texture with `ColorUI.Black`; each `GearSlot` stores its matching template type and uses that texture with `ColorUI.White`. Cleanup restores the original texture, color, and enabled states, so cancelled drops return the source visual and Unity can still deliver `EndDrag`.
- Pooled Loot slots register hover and drag callbacks once when created. They retain the latest item and loot client metadata while active; both right-click and a drop onto any inventory slot call the shared `TakeLoot` path, which invokes the existing `UpdateInventorySubscription` and releases the Loot slot.
- Merchant offer objects use the same drag lifecycle and preview. Only real offer slots enable their pooled `EventTrigger`; adjacent Currency price slots remain non-draggable. Offer → inventory drops call `MerchantUI.Purchase()` and `PurchaseItemSubscribtion`, preserving the existing currency check and purchase RPC. Inventory → Merchant drops invoke `UseItemSubscribtion`; while the Merchant panel is active, `CharacterInventory` ignores normal item use and the owner `Merchant` subscriber performs the existing sell/update RPC path.
- Ammo crafting recipes are mirrored by API/client `CraftingRecipeEnum`: Arrow, Rune, and Feather tiers use Blacksmithing; Oil tiers use Alchemy. Recipe buttons call `SetAsLastSibling()` when pooled so API `Id` ordering is retained visually.
- Merchant and crafting interaction panels close on Escape or after movement carries the player beyond the relevant interaction distance.
- Character health is synchronized from server-side attack handling through `AttackPlayerSubscription`, a targeted client RPC, and `UpdateCharacterCommand`; max health is part of the character DTO/update contract. Incoming armor-ammo consumption is persisted in the same character update as the resulting health, while outgoing damage-ammo consumption uses its own character update.
- Character description refresh now runs when opening Character UI and after level-up RPCs.
- Health-potion use restores up to 20 health capped at max, returns without consuming at full health, persists `Health` through `UpdateCharacterCommand` in dedicated-server builds, and then consumes one potion through the shared usable-item flow.
- Bottom-right quick-access UI is scene-backed in `UIScene` and configured by `QuickAccessUI`, reusing existing slot/preview behavior and icon resources.
- Player locomotion follows the Starter Assets code-driven pattern: `PlayerArmature.prefab` has Animator root motion disabled (`m_ApplyRootMotion: 0`), while `ThirdPersonController` applies movement via `CharacterController.Move(...)` and feeds Animator parameters such as `Speed`, `Grounded`, `Jump`, `FreeFall`, and `MotionSpeed`.
- `ThirdPersonController.Update()` gates local movement processing until `UserManager.Characters` contains the local client ID, because stat-scaled movement reads that character entry.
- Generated/build artifacts are present in the workspace; avoid touching `bin/`, `obj/`, Unity `Library/`, and log/cache outputs unless specifically needed.
- The tracked `.gitignore` currently ignores all `*.meta` files. The known affected project-owned files as of 2026-07-13 include 13 ammo icon/template metas plus `CharacterStatsCalculator.cs.meta`, `AmmoUsableItem.cs.meta`, and `UsableItemFromEnum.cs.meta`; other ignored package/cache metas also exist locally. Future Unity asset/script work must ensure required GUID/import-setting metas are explicitly versioned.
- Dev startup flow:
  - PowerShell starts/restarts the API executable and auto-builds it if missing.
  - PowerShell builds or reuses the Unity dedicated server executable.
  - If Unity Editor is already open, PowerShell drops request files under `Client/Temp/ProjectXAutomation/`; `ProjectXDevAutomation` handles server build and client Play Mode requests from the open editor.
  - Dedicated server build scenes are read from `Assets/Settings/Build Profiles/DedicatedServer.asset`, with a fallback list matching the current profile order.
