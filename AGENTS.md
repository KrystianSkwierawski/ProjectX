# ProjectX — Codex instructions

## Context on demand

This is the main repository instruction file. For simple/local changes, inspect the relevant code first; no Memory Bank read is required unless the task depends on project context. `memory-bank/` is long-term documentation, not an automatic startup checklist.

Read only the relevant files/sections:

| Task needs | Memory Bank files |
| --- | --- |
| Current state, ongoing work, known gaps, next steps | `memory-bank/activeContext.md` + `memory-bank/progress.md` |
| Architecture, invariants, networking, authentication/session lifecycle, persistence, inventory/trade/quest interactions | `memory-bank/systemPatterns.md` |
| Technology/package versions, builds, tests, tooling | `memory-bank/techContext.md` |
| UX, gameplay assumptions, product direction | `memory-bank/productContext.md` |
| Overall project scope | `memory-bank/projectbrief.md` |

For larger cross-cutting changes, combine the relevant files (e.g. persistence work needs system patterns, tooling and current verification gaps). Architecture changes may justify reading the whole Memory Bank; this is optional, not a ritual. Follow relevant code and documentation references as needed without loading unrelated files.

Current code and verified behavior take precedence over stale documentation about implementation. Preserve intentional business rules; an unexplained discrepancy may be a bug, not permission to discard an invariant. After a correct implementation, update affected documentation when decisions, behavior, verification status or known gaps materially change, or when requested. Do not update documentation after every small edit or reread all files merely to update one. Keep durable details in Memory Bank, concise working rules here, and chronology in Git. Preserve outstanding runtime tests until actually performed.

## Architecture and engineering

- `Client/` contains the Unity client and dedicated server. `API/` contains the .NET backend. Unity dedicated server is the gameplay authority; API is the durable/transactional persistence authority. Clients express intent, not trusted gameplay outcomes.
- Follow the existing Clean Architecture direction: Domain owns I/O-free business invariants; Application owns use cases, ports and validation; Infrastructure owns EF/Identity/JWT and adapters; API owns transport, authorization and composition. Inner layers/tests must not depend on outer layers. API persists trusted server-calculated character state instead of duplicating gameplay calculations.
- Use existing Minimal API/typed-result and MediatR conventions. `ApiExceptionHandler` is the exception-to-HTTP boundary. Keep secrets out of request `ToString()` and logs. Use injected `TimeProvider`, UTC `DateTimeOffset` and the auditable interceptor for backend timestamps.
- Keep engineering simple and proportional to realistic player impact. Reuse surrounding patterns; avoid speculative abstractions and exhaustive handling of implausible edge cases. Preserve transaction and authorization guarantees.
- New/modified API, client and server logic needs meaningful safe diagnostics at coordinating boundaries: relevant stages, state changes, rejections, retries and failures with entity/correlation IDs and outcomes. Reuse existing logging; avoid duplicate/per-frame noise and keep Domain I/O-free.

## Networking and persistence safeguards

- Authorization fails closed. Connection tickets bind a selected character to server-only `PlayerSessionId`; delegated gameplay must use that bound character. Never send JWTs/session credentials through gameplay RPCs or assume character ID `1`/one character per user. Preserve the bounded refresh and non-sliding session deadline.
- Spawn-owned NGO async work cancels on despawn/destroy; after every await, recheck the same network-spawn lifetime and `IsSpawned` before RPCs/state mutation. Tracked inventory/quest persistence and accepted trade commits instead use server lifetime so despawn cannot release the mutation barrier before persistence settles; RPC/UI continuations still use spawn lifetime.
- Inventory positions stay stable: `None` + count `0` is empty; removal clears without compacting. Maximum stack is `1024`. Transactions remove first, then fill partial stacks/free slots; if all additions cannot fit, return `InventoryFull` and persist nothing. Legacy normalization must not lose items. API is authoritative even when Unity preflights capacity.
- Compound transitions have one persistence owner. Collect completion consumes requirements, grants Main EXP and completes the quest atomically. Kill progress is additive; Collect progress reflects persisted inventory absolutely and may regress Finished -> Accepted. Preserve per-character quest serialization and participation in the inventory-mutation barrier.
- Trade locks block new inventory-dependent writes and commit drains earlier tracked writes. API atomically persists both inventories, active Collect progress and the idempotency receipt. Keep one immutable trade GUID/payload across uncertain retries; resolve durable receipts before treating an uncertain commit as failed. Participant despawn must not cancel accepted commits. Route authoritative snapshots by authenticated character ID and prevent older async loads from overwriting newer inventory state. Consult `systemPatterns.md` for changes to these interacting flows.

## Contracts and assets

- Synchronize API/Unity DTOs, enum values, localization and checked-in OpenAPI when affected. Append translation keys and mirrored entries in identical order; do not renumber persisted/network enum values. Client `pl.json` intentionally uses English fallback until full Polish localization is requested.
- Preserve Unity `.meta` identity when editing/moving assets; new assets need `.meta` files. Do not commit generated caches, builds or logs. Preserve unrelated working-tree changes.
- Use existing scene-backed UI/prefabs, translation and shared interaction paths. Modified UI must remain responsive in landscape and narrow/portrait layouts.
- Keep `Client/Automation/run.ps1` and `Client/Assets/Editor/ProjectXDevAutomation.cs` aligned when changing startup/build behavior.

## Validation proportional to the change

- Documentation-only: check diff, references and conflicting instructions; no gameplay/API build required.
- API code/config: from `API/`, run `dotnet build ProjectX.slnx` and relevant tests; use `dotnet test ProjectX.slnx` for cross-cutting backend changes. Check formatting of affected code. Contract changes require regenerated OpenAPI (Debug build runs NSwag) and contract tests; EF model/migration changes require the pending-model check against SQL Server configuration. See `techContext.md` for tooling details.
- Before API runtime verification, rebuild and restart the changed API. Development startup intentionally deletes/recreates/seeds the database; production and NSwag must never initialize it.
- Unity C#: compile affected generated projects; shared gameplay/networking or conditional server code must compile both with and without `UNITY_SERVER`. Use Unity import/Netcode processing for affected network components/assets; generated compilation alone cannot validate them.
- UI/assets/localization: validate affected JSON, mirrored keys/contracts, `.meta`/prefab references and relevant layout/interaction behavior. Networking, persistence and lifetime changes need appropriate runtime/full-stack smoke coverage (two authenticated clients for trade); report anything not run and why. Never present compile/static checks as Play Mode, pointer, timing or networking verification.
