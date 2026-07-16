# Project Brief

## Project
ProjectX is a multiplayer game project with a Unity client and an ASP.NET Core backend API.

## Current Scope
- Unity client under `Client/`.
- Backend API under `API/`, organized as separate API, Application, Domain, Infrastructure, and UnitTests projects.
- Gameplay domains currently represented in code include characters, character transforms, health/combat state, max health, character stats, equipment/gear, inventory, quests, crafting recipes, experience, users, and translation/i18n.
- Gear currently includes helmet, chest, boots, weapon, and ammo slots plus stat bonuses that affect persisted character totals.
- Tiered Arrow, Rune, Feather, and Oil ammo content is represented in inventory, merchant, localization, icon, and crafting data. Ammo equip, merge, swap, and unequip paths preserve whole stacks; Arrows require bows, Runes require wands, and Feathers/Oils require swords. Feather armor ammo is consumed on a non-dodged incoming hit, while Arrow/Rune/Oil damage ammo is consumed on an outgoing hit.

## Core Goals
- Provide a playable Unity client backed by persistent API services.
- Keep game state and progression features synchronized between client, Unity dedicated server, and API.
- Support user login/authentication, character state, health, stats, gear/ammo, quest progress, inventory, crafting, and localization.

## Source Of Truth
- This memory bank was initialized from repository inspection on 2026-05-07 and reviewed against repository HEAD `8c954ff` on 2026-07-13, incorporating the user clarifications recorded on 2026-07-07 and 2026-07-08.
- Product-specific goals beyond the current code shape are not yet documented and should be confirmed with the user as the project evolves.
