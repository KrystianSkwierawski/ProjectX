# Inventory icon set

The 2026-10-13 refresh replaces 44 older inventory/gear-slot/fallback icons with transparent painterly PNGs matching the 11 existing Farm crop/seed icons. All 55 InventoryItemEnum values resolve to an icon. Cursor, quick-access controls and the Farm building icon are outside this item refresh.

Assets live in `Assets/Resources/Icons`. JPG replacements retain their original Unity GUID via the moved `.meta` file; resource basenames remain unchanged. Generated source artwork retains its original resolution. Import limits are 256 pixels (including platform settings), bilinear filtering, clamp wrapping, alpha transparency, no mipmaps and no CPU readability. Existing compression is retained. The InventorySlot icon is approximately 43 UI pixels, so the import budget allows scaling without loading full-resolution source artwork.

Generation used the built-in image_gen tool. `item-icon-generation.json` records the shared prompt, per-item subject and original generated master path; master paths are local provenance, not runtime dependencies. The first Chamomile icon used the same farm references with a dedicated prompt asking for white petals, golden centers, short green stems and readability at 48 pixels. Style references: Mint.png and ChamomileSeed.png. All runtime assets are copied into the project.

Validation on 2026-10-13:

- Unity 6000.1.15f1 synchronous import and Resources.Load probe passed for all 55 enum values: alpha present, maximum 256x256, one mip level, CPU readability disabled.
- Editor profiler reported 7,281,872 bytes total for the 55 loaded textures (approximately 6.94 MiB; this is an editor measurement, not a player-build benchmark).
- All 44 replaced assets retained their original GUIDs.
- Browser visual review at 43px and 86px on light and dark backgrounds passed. Local evidence: `Logs/item-icons-preview.png`; Unity evidence: `Logs/item-icons-import.log`. These generated files are not committed.
- Actual authenticated gameplay UI/hover/drag interactions were not exercised. Browser review does not replace a Play Mode check of the final Unity-compressed appearance.
