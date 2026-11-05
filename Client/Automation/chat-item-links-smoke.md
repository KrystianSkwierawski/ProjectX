# Chat item links

While composing, Ctrl + left-click inserts an item at the caret or replaces the current selection. Links use small InventorySlot instances without quantities. Public messages and whispers support up to eight links within the existing 200-character body limit. Hover uses the standard description and the recipient's equipped comparison.

Icons reserve 30 units of text advance and render at 24x24, leaving three units on each side, with extra line spacing. The 36-unit input field has a 32-unit viewport and vertically centered text so draft icons fit without clipping. A shared static runtime font contains only the icon placeholder with its own transparent atlas/material, avoiding a visible dot from TMP's trailing caret-tracking glyph; ordinary text uses the original font as fallback. Consumed left-clicks suppress the button action and inventory drag callbacks. Recipe rows use the same ButtonUI interception.

## Automated validation

Run `ProjectX > Chat > Validate Item Links` in Unity Edit Mode (`ProjectX.Editor.ChatItemLinkValidation.Run`). The probe uses a temporary preview scene and restores temporary singleton references. It covers token validity and limits, deselect/pointer handoff, selection replacement, miniature artwork/count visibility, wrapping, excess pasted links, row reuse, tooltip layer mount/restore, standard item description and equipped-slot selection.

Verified 2026-11-04: Unity import and the focused Editor probe passed; generated client compilation with and without UNITY_SERVER passed; 410 localization tests passed. These checks do not establish real input timing or two-client delivery.

The Ctrl+left/larger-icon follow-up also passed the Editor probe for 24-unit geometry, wrapping, left-click action suppression, recipe prefab wiring, glyph lookup rebuild and font resource cleanup. PL/EN shortcut descriptions are mirrored. Real modified-key pointer/drag timing and the final in-game appearance still require the checks below.

The spacing/clipping follow-up passed the Editor probe for adjacent icon padding, vertical containment in a 32-unit input viewport and a transparent caret-tracking atlas. Client compilation passed. Final appearance after restarting Play Mode remains a manual check.

## Remaining in-game checks

1. Focus chat and Ctrl + left-click items in inventory, gear, merchant, crafting recipes/ingredients/results, stash, loot, trade, quest rewards, action bars and sent chat messages. Confirm insertion and that no buy/sell/equip/use/transfer action occurs. Verify normal right-click with chat unfocused remains unchanged.
2. Mix text and multiple links, replace a reversed selection, move the caret, delete/backspace links, paste, and continue typing after insertion. Check Enter, Escape and draft preservation after rejected messages. Test the eight-link and 200-character limits.
3. With two authenticated clients, send public messages and whispers. Check item identity and comparison against the receiving character's equipment, including no equipped item and ammunition.
4. Hover draft and received icons, scroll history, close/reopen chat, and receive enough messages to recycle rows. Check that no tooltip remains visible after its owner disappears. Repeat at 1280x720, 720x1280 and 360x800, including wrapped messages near viewport edges.

Rebuild the dedicated server before live verification so both ends enforce the new token validation.
