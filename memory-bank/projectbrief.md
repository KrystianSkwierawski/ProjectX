# Project Brief

## Project
ProjectX is a multiplayer game project with a Unity client and an ASP.NET Core backend API.

## Current Scope
- Unity client under `Client/`.
- Backend API under `API/`, organized as separate API, Application, Domain, Infrastructure, and UnitTests projects.
- Gameplay domains currently represented in code include characters, character transforms, health/combat state, max health, character stats, equipment/gear, inventory, quests, crafting recipes, experience, users, and translation/i18n.
- Gear currently includes both equipped item slots and stat bonuses that affect persisted character totals.

## Core Goals
- Provide a playable Unity client backed by persistent API services.
- Keep game state and progression features synchronized between client, Unity dedicated server, and API.
- Support user login/authentication, character state, health, gear, quest progress, inventory, crafting, and localization.

## Source Of Truth
- This memory bank was initialized from repository inspection on 2026-05-07, refreshed from the current repository state on 2026-07-06, and amended with user clarification on 2026-07-07.
- Product-specific goals beyond the current code shape are not yet documented and should be confirmed with the user as the project evolves.
