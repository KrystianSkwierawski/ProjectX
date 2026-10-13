# UI readability and quest replacement

## Design decisions

- Panel dimensions follow the task, not a universal 16:9 or 9:16 ratio. Quest dialogue is a wider, shorter reading panel; equipment, merchant and stash keep useful list/grid space; crafting retains room for requirements and six farm slots.
- Preserve the existing screen docks, shared tooltip layer, 32px help/close targets, 24px titles, 36px actions and 20px visible content inset.
- Main gameplay panels use a consistent dark surface at 94% opacity, so terrain does not compete with text. Nested recipe/offer regions have less contrast than the main content.
- Chat uses 16px text. Quest/chat scroll content has 6px horizontal and 4px vertical padding; outer insets compensate so the visible content still aligns. TMP input text has a separate 4px inset for italic overhang.
- Friends and Party use the same surface treatment. Trade retains its bilateral offer layout and slot dimensions. Inventory shows three full rows before scrolling. Action bars, quick access, buff indicators, tooltips, player/target frames, loading and login retain their specialized structure; no universal window ratio is applied to them.
- Existing CanvasScaler Expand behavior remains. Narrow/portrait windows keep panels inside the viewport but shrink text; a readable mobile layout would require a separate arrangement of simultaneous HUD panels, not just changing panel ratios.

## Automated verification, 2026-10-12

- Generated client and UNITY_SERVER builds: zero errors, existing warnings.
- Isolated Unity import/Netcode processing and Play Mode: successful.
- Quest A -> completed quest B -> quest A replaces title, description, action label and displayed NPC; long descriptions restart at the top with stopped scroll momentum. Closing clears the displayed NPC.
- Actual TMP character quads fit horizontally within quest/chat masks at 1920x1080, 1280x720, 2560x1080, 720x1280 and 480x1280.
- Panel bounds, inventory/header separation, stable Friends placement, help preview enter/exit/close/reopen, six farm slots, stash header/scrolling, trade/inventory slot parity and primary action placement pass the isolated UI probe.
- Evidence: ignored `Client/Logs/ui-refinement-play.log`, marker `UI_REFINEMENT_PASS`; rendered previews under `Client/Logs/LocationValidation/Logs/UiRevision/`.

## Manual authenticated checks still required

1. Open quest A, then right-click another nearby available quest NPC B. Verify the existing window immediately shows B; accept B and verify only B is accepted.
2. Open A, hover B without clicking, then accept A. Hovering must not change the action source. Repeat with a finished quest and its Complete action.
3. Scroll a long quest to the bottom, switch quests and verify the new description starts at the top. Check Escape, close, walking out of range, and opening after trade cancellation.
4. Check Polish and English text, first/last letters, italic chat placeholder, multiline messages, whispers and long names over bright and dark world backgrounds.
5. Exercise chat scrolling/input, inventory/stash scrolling and dragging, merchant selection, crafting/farm, help previews, Friends/Party and Trade using a real pointer. Verify actual enabled/disabled button states with authenticated data.

The isolated Play Mode probe does not perform API quest mutations, networked trade, manual mouse interaction or validate mobile text readability.
