# Product Context

## Purpose
ProjectX provides a persistent multiplayer RPG gameplay loop: players authenticate, control a character, fight and interact, manage equipment and inventory, complete quests, craft/use items, socialize, form parties, and trade.

## Player Experience
- Inventory supports exact-position drag/drop, swapping, merging, stable empty slots, loot pickup, merchant transactions, and Inventory/Gear transfers with visible drag-source placeholders.
- A stack holds at most `1024`; additions fill partial stacks then free slots, and any transaction that cannot fully fit fails atomically with visible feedback.
- Gear exposes helmet, chest, boots, weapon, and ammo slots. Weapon/ammo compatibility is Arrow/Bow, Rune/Wand, Feather+Oil/Sword; whole stacks and bonuses survive equip, merge, swap, and unequip transitions.
- Health potions heal up to 20 without consumption at full health. Strength and Speed potions grant refreshable, non-stacking runtime `+20` buffs for 60 seconds and expose timers/previews.
- Parties are ephemeral server state with invitations, leadership, membership, live health/level updates, and shared enemy rewards. Eligible nearby members split Main EXP and independently receive loot and Kill-quest credit.
- Friends are persistent character relationships with invitations, online state, removal, and exact-name private whispers.
- Player trade shows bilateral offers, per-side lock, and two-party confirmation. It either atomically commits both inventories or cancels/fails without transferring anything; max-stack and capacity rules remain authoritative.

## Trade UI Contract
- Invitations are compact. Incoming invitations show Accept/Decline; outgoing waiting shows only Cancel.
- Active offers start empty and accept a complete clicked stack by right-click or drag/drop. Offered quantities disappear visually from their source inventory slot, including while awaiting acknowledgement, and return on withdrawal, rejection, cancellation, or closure.
- Own offer items can be returned to inventory by click/right-click or drag/drop. Offer fields align with their labels; items render left-to-right in exactly four columns with equal horizontal and vertical content padding plus a small inset around each icon. Incomplete rows stay left-aligned and content scrolls vertically.
- Trade remains separate from Inventory, Loot, and complete wrapping LogUI messages. Opening trade hides Crafting, Quest, Merchant, Gear, and Character; opening one of those cancels negotiation and opens it only after authoritative trade closure. An in-flight commit must finish definitively.

## Product Direction And Open Questions
- Prioritize systems/mechanics over new art. Defer price, recipe, and drop-rate balancing until systems can be exercised together.
- Do not add a real character selector until the user reintroduces that scope; keep temporary first-character selection isolated.
- Planned systems include centralized Escape/UI closure, visible quest rewards, Gear Score, Guilds, and an Auction House.
- The final genre/platform/session/release model and base-stat-versus-gear-stat model remain undecided.
- Combat behavior beyond current weapon stat selection and ammo bonuses remains open.
- Client `pl.json` intentionally contains English fallback text; complete Polish localization is future work.
