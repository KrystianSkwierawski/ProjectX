# Active Context

## Current Focus
- Memory bank initialized on 2026-05-07 at user request.
- No feature implementation task is currently active.

## Recent Changes
- Created the required memory bank structure:
  - `projectbrief.md`
  - `productContext.md`
  - `activeContext.md`
  - `systemPatterns.md`
  - `techContext.md`
  - `progress.md`

## Active Decisions
- Treat the repository as two cooperating applications:
  - `Client/`: Unity 6000.1 project using Netcode for GameObjects.
  - `API/`: ASP.NET Core API using clean/layered architecture.
- Preserve existing project organization and naming patterns when making future changes.
- Read all memory bank files at the start of every task, per `.Codexrules`.

## Next Steps
- For future implementation tasks, first refresh this file with the current working focus.
- Confirm product-level goals with the user when changes require design or gameplay decisions not already implied by code.
- Keep `progress.md` updated after meaningful changes.

## Important Local Notes
- `git status` at initialization showed `.Codexrules` and `.gitignore` as untracked; these appear pre-existing and were not modified by this initialization.
