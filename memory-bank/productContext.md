# Product Context

## Why This Exists
ProjectX appears to be a networked RPG/survival-style game prototype or project. The codebase combines real-time Unity gameplay with backend systems for account, character, inventory, quest, crafting, and progression data.

## Problems It Solves
- Lets the Unity client request and persist player/account state through an API.
- Separates game content and progression systems from local-only client state.
- Provides a foundation for server-authoritative or server-backed gameplay loops.
- Persists character health, max health, stats, and equipped gear through the character update API.
- Lets gear items apply stat bonuses and expose those bonuses in inventory, merchant, crafting, and gear UI previews.
- Applies early runtime stat behavior: equipped Iron Sword/Wand/Bow select Strength/Intellect/Dexterity for fireball damage scaling, Dexterity provides dodge chance, Speed scales movement, and Armor reduces incoming damage.
- Exposes tiered ammo items through merchant stock and Blacksmithing/Alchemy recipes, with weapon-compatible equip rules, whole-stack transfers, and server-authoritative per-hit consumption synchronized to the API and owner UI.
- Lets health potions restore up to 20 health, persist the resulting health from dedicated-server builds, and remain unconsumed when the character is already at max health.
- Lets craftable Strength and Speed potions apply server-authoritative, refreshable temporary bonuses with visible upper-right timers and normal item-slot hover previews that state the exact bonus.
- Lets nearby members of an ephemeral server-memory Party share enemy rewards: Main EXP is divided evenly, while personal loot eligibility and Kill-quest credit are granted to every eligible member.
- Supports multiple languages through i18n resources and translation services.

## User Experience Goals
- Players should be able to log in, control a character, interact with targets/NPCs/resources, manage inventory, equip gear, track health, progress quests, craft items, and gain experience.
- Inventory management should support direct drag-and-drop organization, including exact empty-slot placement, swapping different items, merging matching stacks, and equipping/unequipping through Gear with clear visual feedback while dragging. Any Gear slot may receive an equippable item; the item type determines its actual equipment slot.
- Loot should be transferable into inventory by drag-and-drop as well as right-click. While an item is being dragged from Inventory, Gear, Merchant, or Loot, its source item/count should be replaced by the appropriate empty-slot appearance (black inventory-style background or the matching Gear template) so only the cursor-following preview represents the item; cancellation restores the source.
- Merchant interaction should support direct transactions: dragging an offer into inventory buys it, while dragging an inventory item onto the Merchant panel sells it, using the same prices, currency checks, and persistence as the existing click actions.
- Inventory additions should cap each stack at 1024, distribute overflow across matching/free slots, and fail atomically with a visible message when the complete transaction cannot fit.
- UI should expose character, gear/stat totals, inventory, item stat previews, quest, crafting, hover/target, cursor, quick-access, chat, and translation behavior through Unity scenes and prefabs.
- Client/server behavior should feel consistent across play sessions.

## Open Product Questions
- Near-term development is systems/mechanics-first for one programmer working without an artist; visual polish is deferred. The current roadmap also defers a real character selector and price/recipe/drop balancing.
- Planned systems include Gear Score, player-to-player trade, Guilds, an Auction House, consistent Escape-based UI closure, visible item/experience quest rewards, and later balancing/configuration of the implemented Party reward radius and EXP split.
- The exact genre, target platform, multiplayer session model, and release goals are not documented yet.
- The intended authority split between Unity host/server and the ASP.NET Core API needs clarification before large networking changes.
- The intended model for base stats versus gear-derived stats needs clarification; current gear use mutates persisted totals directly.
- Weapon-specific combat behavior beyond selecting the damage-scaling stat is not documented or implemented yet.
- Ammo equip semantics use explicit `Inventory`/`Gear` origins and weapon categories. First equip, same-type merge, different-type swap, weapon-triggered auto-unequip, and gear unequip transfer whole stacks and corresponding stat bonuses. Current combat consumption is one Feather on a non-dodged incoming hit or one Arrow/Rune/Oil on an outgoing hit; additional ammo-specific effects beyond their stat bonuses remain open product work.
- Broader consumable-item behavior beyond the current health, Strength, and Speed potions is not yet documented.
- Full Polish client localization remains a future product task; current English content in client `pl.json` is an intentional temporary fallback for development.
