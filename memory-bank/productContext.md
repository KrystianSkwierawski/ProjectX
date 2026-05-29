# Product Context

## Why This Exists
ProjectX appears to be a networked RPG/survival-style game prototype or project. The codebase combines real-time Unity gameplay with backend systems for account, character, inventory, quest, crafting, and progression data.

## Problems It Solves
- Lets the Unity client request and persist player/account state through an API.
- Separates game content and progression systems from local-only client state.
- Provides a foundation for server-authoritative or server-backed gameplay loops.
- Supports multiple languages through i18n resources and translation services.

## User Experience Goals
- Players should be able to log in, control a character, interact with targets/NPCs/resources, manage inventory, progress quests, craft items, and gain experience.
- UI should expose character, inventory, quest, crafting, hover/target, cursor, and translation behavior through Unity scenes and prefabs.
- Client/server behavior should feel consistent across play sessions.

## Open Product Questions
- The exact genre, target platform, multiplayer session model, and release goals are not documented yet.
- The intended authority split between Unity host/server and the ASP.NET Core API needs clarification before large networking changes.
