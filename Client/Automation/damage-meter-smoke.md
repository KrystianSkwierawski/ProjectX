# Damage meter

`]` or the Font Awesome chart-bar QuickAccess button toggles the damage meter. The localized QuickAccess tooltip includes the `]` shortcut. Input focus suppresses the shortcut. The window reuses the existing help/close controls, scroll styling, palette and a reduced InventorySlot with preview and interactions disabled. Its size follows inventory, with matching vertical alignment and a 10-unit gap to its left when space permits; the left-aligned title sits beside the help control. Rows are 28px high; square icons and bars share a visible height of 26px. Each row represents one character and weapon, sorted by damage, with nickname, numeric damage, percentage of the locally recorded total and a matching bar.

The server reports accepted damage capped at the target's remaining HP before lethal pooling. Weapon identity is captured when the cast starts and retained across projectile reuse and queued hit resolution. Events go to the attacker when solo, or all currently connected party members, independently of reward distance. Historical rows remain after membership changes. Recording continues while hidden; reset affects only the local client. Owner spawn/despawn clears the session's totals. Nothing is persisted by the API.

The meter stays docked relative to inventory even when inventory is hidden. Other windows no longer change its position. When a readable panel cannot fit to the left on a narrow screen, the meter docks above inventory with a gap and stays within the canvas. Weapon icons use square backgrounds without rounded masks. Gear ammunition count uses the inventory count layout.

The meter's help and close controls match inventory's header positions. Its list, viewport and reset footer use consistent insets and spacing. Chat follows inventory's height and vertical position, with its left edge aligned to player status; when visible panels would overlap it on a narrower screen, chat moves above them and its height is limited to the available space. If status would leave too little vertical room and there is sufficient width, chat moves to its right. Its log and input have matching side insets and footer spacing.

## Automated checks

- `ProjectX.Editor.DamageMeterAuthoring.Build` authors the scene panel and row prefab; `Validate` checks populated ordering, percentages, small noninteractive icons, scrolling geometry, reset and PL/EN layouts at 1280x720, 720x1280 and 360x800. Rendered previews are written to ignored `Logs/damage-meter-*.png` files.
- `ProjectX.Editor.DamageMeterGameplayValidation.Run` checks actual accumulator grouping, stable ties, invalid inputs, reset, overkill calculation and current solo/party routing using the production party state. These are isolated editor probes, not authenticated networking tests.
- Compile Unity's generated project with and without `UNITY_SERVER`, and run Unity import/Netcode processing. API localization changes require the API build and `JsonFileTranslateServiceTests`.

## Fix verification (2026-11-02)

- Corrected recipient lookup: NGO's `PlayerObject` is the stationary root; the real `Player` is on its moving child. Looking only on the root silently skipped solo and party delivery. The descendant lookup retains spawned/owner checks.
- `ProjectX.Editor.DamageMeterGameplayValidation.RunPlayMode` passed with a real NGO host and production Health/Player/RPC code, matching the checked-in prefab's root/child arrangement. It verifies accepted solo hits, authoritative identity, weapon grouping, lethal overkill, dead-target rejection and despawn reset. Marker: `DAMAGE_METER_SOLO_DELIVERY_VALIDATED`. This probe bypasses API authentication and does not prove remote-client transport.
- `DamageMeterAuthoring.Validate` previously passed original overlay/CanvasScaler bounds and overlap checks at the editor's actual screen size, plus PL/EN world-space fixtures at 1280x720, 720x1280 and 360x800. Inventory toggles, visible bottom bars/chat, a cast bar inactive at startup and crowded-screen fallback are covered. Full layout captures: ignored `Logs/damage-meter-docking-*.png`. These fixtures are not manual pointer interaction.
- Client and server-symbol generated builds passed with existing warnings. Unity import/Netcode processing and a full dedicated-server build passed; the local `Client/Builds/Server/ProjectXServer.exe` output now includes the delivery fix. No server was started or API database reinitialized by these checks.

## UI follow-up verification (2026-11-02)

- A later isolated Unity editor validation passed after the header, content and chat alignment changes. It checked desktop and portrait bounds, matching header and footer insets, chat/inventory alignment at 1280x720, and chat separation from inventory, damage meter and player status at 1280x720, 720x1280 and 360x800. Captures are in ignored `Client/Logs/ui-alignment-damage-meter-docking-*.png`; the fixture's empty inventory and black bar are not a gameplay rendering check. Physical pointer and keyboard interaction remain untested.
- Isolated Unity import/compilation and `DamageMeterAuthoring.Validate` passed: stable inventory-relative placement, left gap, narrow-screen stacking, 1280x720 / 720x1280 / 360x800 layouts, populated rows/percentages, scrolling/reset, square icons, tooltip key and ammunition count 1024. Rendered desktop, portrait and ammunition previews were inspected. Evidence: ignored `Client/Logs/damage-meter-ui-validation.log` and `damage-meter-docking-*.png` / `damage-meter-gear-ammo.png`.
- API Release build passed and 410 localization tests passed. Debug build was blocked by files held by the running API; the process was not stopped. Physical pointer/keyboard interaction was not run.

## Authenticated runtime checks still required

- Toggle with `]` and the QuickAccess button; verify its icon and localized shortcut tooltip. Type `]` in chat without opening the meter. Open/close inventory and other panels and verify stable docking. Check gear ammunition counts through 1024 stay within the slot. Check help, X, reset and mouse-wheel scrolling in Polish and English, in landscape and portrait.
- Damage targets with each weapon solo. Confirm rejected hits add nothing and a lethal hit counts only remaining HP. Switch weapons while a projectile is in flight and while hit resolution waits for equipment persistence.
- With two authenticated party clients, confirm both receive each member's hits exactly once, including distant members. Reset one client and confirm the other retains its totals. Keep the window closed during combat, then reopen it.
- Join/leave a party: old rows remain, while subsequent events follow current membership. Disconnect/reconnect: a new local session starts empty.

Static/build/editor checks do not establish physical pointer behavior, authenticated RPC delivery or projectile timing.
