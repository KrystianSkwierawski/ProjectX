# Project Brief

## Project
ProjectX is a multiplayer RPG-style game with a Unity client, a Unity dedicated server, and an ASP.NET Core backend API.

## Scope
- `Client/`: Unity gameplay, UI, Netcode for GameObjects, local development automation, and dedicated-server builds.
- `API/`: persistent accounts, characters, inventories, quests, crafting, friendships, game sessions, and trade transactions, organized as API/Application/Domain/Infrastructure plus responsibility-aligned tests.
- Implemented gameplay areas include authentication/session refresh, character state and combat, stats, gear/ammo, inventory and loot, quests, crafting, consumables/buffs, friends/whispers, parties/reward sharing, and player-to-player trade.

## Goals
- Keep gameplay authoritative on the Unity server while persisting durable state through the API.
- Keep client, server, API contracts, inventory rules, and localization synchronized.
- Favor correct, testable systems that one programmer can maintain; defer art-heavy polish and economic balancing.

## Sources Of Truth
- Backend architecture follows applicable patterns from `jasontaylordev/CleanArchitecture`, adapted to ProjectX's Unity, authentication, persistence, and deployment needs.
- This Memory Bank records behavior and decisions; current code and verified behavior take precedence over stale implementation descriptions. Root `AGENTS.md` defines working instructions and documentation routing; detailed implementation history remains available in Git.
- Product decisions not recorded here should be confirmed when they materially affect implementation.
