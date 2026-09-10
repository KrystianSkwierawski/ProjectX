# System Patterns

## Repository And Backend Architecture
- `Client/` is the Unity client/dedicated server; `API/` is the layered .NET backend; `.Codexrules` defines Memory Bank workflow.
- Follow applicable `jasontaylordev/CleanArchitecture` dependency direction: Domain is framework-free business state, Application owns use cases/ports/validation, Infrastructure owns EF/Identity/JWT/localization/game-session adapters, and API owns transport, authorization, OpenAPI, rate limiting, and composition.
- Backend tests mirror those boundaries: Domain/Application unit, Infrastructure integration, Web acceptance, and Architecture tests. Do not add outer-layer references to inner-layer tests.
- Minimal API handlers are public/static and use typed results. Await MediatR into a local result before constructing the response; attach concise summary/description, authorization, rate limits, and non-inferred errors in route mapping.
- Operation folders keep handler/request together; validators and operation-owned DTOs have separate files. Concrete MediatR requests must define safe `ToString()` output.
- Domain entities own I/O-free invariants; Application coordinates authorization, loading, domain calls, and persistence. The authenticated Unity server owns gameplay calculations, so API character updates persist trusted supplied state rather than duplicating all gameplay rules.
- `ApiExceptionHandler` is the sole exception-to-HTTP boundary: invalid credentials `401`, forbidden `403`, validation `400`, missing resources `404`; unexpected failures remain server errors.
- API application timestamps use injected singleton `TimeProvider.GetUtcNow()`, `DateTimeOffset`, and the EF auditable interceptor. Local-time conversion belongs only to UI.
- Development-only database initialization intentionally deletes/recreates/seeds. Production and NSwag must never invoke it.

## Authentication, Sessions, And Networking
- Authorization fails closed; only explicitly anonymous endpoints are public. JWT accepts HS256 only, requires a 32-byte secret, caps access tokens at one hour, and preserves a signed non-sliding 24-hour `session_started_at` deadline.
- Client/server refresh after 56 real-time minutes and retry transient errors only through the last four minutes. Terminal failure logs a client out after networking/scene teardown or exits the dedicated server for supervised restart. Tokens never enter logs or Netcode.
- Before connection, owned reads/tickets use explicit `CharacterId`; ticket redemption binds one selected character to opaque `PlayerSessionId`. After redemption, delegated gameplay contracts use that session-selected character and must not accept another character ID.
- Admission uses a 60-second random single-use ticket and NGO Connection Approval. The authenticated server registers one Direct/Relay game session with a renewable 90-second lease. Gameplay RPCs carry no JWT/session credential; server-to-API calls carry server JWT plus `PlayerSessionId`.
- In-memory session/ticket storage supports one API process; use a shared atomic store before horizontal scaling.
- Async work owned by a spawned `NetworkBehaviour` uses a token tied to that network-spawn lifetime, cancels on despawn/destroy, and rechecks the same lifetime plus `IsSpawned` after every await before RPC/state mutation.

## Persistence And Gameplay Invariants
- Never assume character ID `1` or one character per user. `UserManager.Characters` is keyed by Netcode client ID; temporary first-character selection stays isolated until a real selector exists.
- Inventory positions are stable: `None` with count `0` is an empty slot; full removal clears rather than compacts. Each stack caps at `1024`.
- Inventory transactions clone state, remove first, then fill matching partial stacks and free slots. If all additions cannot fit, return `InventoryFull` and persist nothing. Unity may preflight for UX, but API remains authoritative.
- Persisted oversized legacy stacks normalize without item loss, reusing empty slots before extending effective capacity.
- Compound gameplay transitions have one persistence owner. Collect-quest completion removes requirements and completes the quest in one API save; Unity only synchronizes the result.
- Kill quest progress is additive. Collect progress is the absolute persisted inventory count, initialized on acceptance and resynchronized after every add/remove; it may regress Finished -> Accepted.
- Quest accept/progress/complete operations serialize through one per-character lifetime-cancelable semaphore.
- Trade negotiation is ephemeral Unity-server state: one invitation/session per player, normalized bilateral offers, per-side lock, both-side confirmation, and an uneditable/uncancellable committing state. Lock/commit reserves inventory from other mutation paths.
- Trade commit sends one immutable snapshot to `CharacterInventories/Trade`. The trade GUID is an idempotency key; the API atomically saves both inventories and a payload-fingerprint receipt. It removes both outgoing sides before fitting incoming items with normal capacity rules. EF concurrency loss, invalid items, or capacity failure changes neither inventory; an exact retry returns `Applied` without duplication.
- Party state is ephemeral and friend-authorized. Leader rules and disconnect cleanup are server-owned. Enemy rewards use the dead enemy as center, always include the authenticated killer, include connected party members within the current 1000-unit radius, split Main EXP by integer division, and grant personal loot plus Kill credit to each eligible member.
- Crafting recipes are typed Domain definitions and returned/displayed by recipe ID. Active client crafting cancels on movement, actual damage, panel replacement/closure, Escape, or leaving station range; server-side recipe/material/station hardening remains open.

## Character, Gear, Ammo, And Consumables
- `UpdateCharacterCommand` persists optional health/max health, six stats, and equipment fields exactly as supplied by the authoritative server.
- Current stat mechanics are percentage based: sword/Strength, wand/Intellect, bow/Dexterity scale outgoing weapon damage; Dexterity is capped dodge, Speed scales movement, and Armor is capped reduction. Animator root motion remains disabled; `ThirdPersonController` drives movement.
- Gear behavior uses `AbstractUsableItem`/`AbstractGearUsableItem`; `UsableItemFromEnum.Inventory` or `.Gear` specifies origin. Metadata supplies stat bonuses and `InventoryItemEnum.IsWeapon()` classifies weapons.
- Ammo contract is `AmmoType + AmmoCount`; compatibility comes solely from `InventoryItemParametersAttribute.WeaponCategory`: Arrow/Bow, Rune/Wand, Feather+Oil/Sword. Equip/merge/swap/unequip and incompatible-weapon auto-unequip preserve whole stacks and bonuses.
- Feather is consumed after a positive non-dodged incoming hit; Arrow/Rune/Oil after outgoing damage calculation. The final unit affects that hit, then removes its bonus and resets the slot to template.
- Health potions heal at most 20 and are not consumed at full health. Strength/Speed potions apply server-authoritative runtime-only `+20` for 60 seconds; reuse refreshes duration and disconnect/expiry clears it.

## Unity UI And Interaction
- Prefer scene-backed reusable prefabs. `*UI.cs` resolves/caches owned hierarchy controls during startup; serialize only external assets/configuration. Dynamic repeated entries instantiate prefabs rather than constructing hierarchies ad hoc.
- Every modified UI must be responsive: explicit CanvasScaler, suitable anchors/layout groups, bounded preferred sizes, narrow-screen margins, and landscape plus portrait/narrow validation.
- Static prefab/scene text uses `TranslateUI`; dynamic/stateful/formatted text uses `TranslateManager`. Append `TranslateKeyEnum` and matching client/API localization entries only at the end and in identical order.
- A focused `TMP_InputField` exclusively owns keyboard input through `InputFocusUI`; gameplay controls and shortcuts remain neutral while typing.
- `LoadingScene` remains loaded on clients and owns nestable disposable `LoadingScreenUI` scopes. Bootstrap owns orchestration; scene-scoped `LoginUI` owns login view behavior. Dedicated builds remain Bootstrap-first.
- Shared inventory drag uses a non-raycastable cursor preview and source placeholder; cleanup restores source visuals. Inventory/Gear swaps and merges use existing authoritative paths; Loot and Merchant reuse the same drag lifecycle and their existing pickup/purchase/sale subscriptions.
- Hover/item descriptions use `InventoryUI.PrepareDescription`; preview Canvas order is 20, above normal gameplay UI. Quick Access and Buff UI are scene-backed and reuse `InventorySlot` presentation.
- Trade uses prefab-authored invitation/session/action rows, masked scroll views, and a `TradeOfferSlot` variant. Offers use exactly four left-aligned columns with symmetric padding and responsive square cells. Trade placement accounts for visible Inventory and Log bounds; Log messages wrap at preferred height on non-blocking sorting order 30.
- `TradeInventoryProjection` visually subtracts active/pending offered quantities from exact source slots without mutating authoritative DTOs. Rejection/removal/closure restores them; unrelated peer snapshots must not clear a pending offer. Local offer mutations are single-flight. Reserved source/target slots reject index moves.
- Own-offer drag reuses InventoryUI preview/restoration, forwards wheel scrolling, and clears `eligibleForClick` so a completed drag cannot also trigger click removal. Competing panels wait for authoritative trade closure; never optimistically cancel a committing transfer.

## Coding And Operational Conventions
- Keep code simple and consistent with surrounding style. Separate logical stages with blank lines; keep assignment targets with their expressions; extract locals instead of awkward wrapping.
- In LINQ, put independent filters in separate `.Where(...)` calls and use `x` for generic lambda parameters.
- Sensitive request `ToString()` implementations omit secrets entirely. MediatR pipeline logging handles request start/completion/timing/rejection/failure; use injected `ILogger<T>` only for meaningful business/audit events.
- Preserve Unity `.meta` files and avoid generated/cache outputs. Keep mirrored enums/contracts/localization synchronized.
- After API source/config/contract/migration changes, rebuild and restart the API before runtime verification; earlier binaries are not evidence for the current diff.
- Dev automation and Unity menu behavior must stay aligned. Persist per-process runtime logs under ignored `Client/Logs/Runtime`; keep build logs separate.
