# Product Context

## Why This Exists
ProjectX appears to be a networked RPG/survival-style game prototype or project. The codebase combines real-time Unity gameplay with backend systems for account, character, inventory, quest, crafting, and progression data.

## Problems It Solves
- Lets the Unity client request and persist player/account state through an API.
- Separates game content and progression systems from local-only client state.
- Provides a foundation for server-authoritative or server-backed gameplay loops.
- Persists character health, max health, stats, and equipped gear through the character update API.
- Lets gear items apply stat bonuses and expose those bonuses in inventory, merchant, crafting, and gear UI previews.
- Applies early runtime stat behavior: Strength scales fireball damage, Dexterity provides dodge chance, Speed scales movement, and Armor reduces incoming damage.
- Exposes tiered ammo items through merchant stock and Blacksmithing/Alchemy recipes, while the final ammo equip/consumption loop remains unfinished.
- Supports multiple languages through i18n resources and translation services.

## User Experience Goals
- Players should be able to log in, control a character, interact with targets/NPCs/resources, manage inventory, equip gear, track health, progress quests, craft items, and gain experience.
- UI should expose character, gear/stat totals, inventory, item stat previews, quest, crafting, hover/target, cursor, quick-access, chat, and translation behavior through Unity scenes and prefabs.
- Client/server behavior should feel consistent across play sessions.

## Open Product Questions
- The exact genre, target platform, multiplayer session model, and release goals are not documented yet.
- The intended authority split between Unity host/server and the ASP.NET Core API needs clarification before large networking changes.
- The intended model for base stats versus gear-derived stats needs clarification; current gear use mutates persisted totals directly.
- The intended gameplay effect for Intellect is not documented or implemented yet.
- Ammo equip semantics now use explicit inventory/gear origins and whole-stack transfers. Per-attack ammo effects and consumption still need clarification and implementation.
- Health-potion behavior currently has a local/client-side health update path; persistence expectations should be confirmed before expanding consumable item behavior.
- Full Polish client localization remains a future product task; current English content in client `pl.json` is an intentional temporary fallback for development.
