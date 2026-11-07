# Quest journal verification

The journal lists accepted, ready-to-turn-in and completed character quests. J and the QuickAccess journal button open the same window. Selecting a title highlights it in green and displays its description, requirements, rewards and status.

Abandoning is allowed before rewards are claimed, including when every objective is finished. It removes that character quest's progress without removing inventory items or granting rewards. The quest becomes available at its NPC again; a new acceptance starts kill objectives over and derives collection progress from the items still owned. Completed quests remain visible and cannot be abandoned.

## Authenticated runtime checks

- Open and close with J, the QuickAccess button, X and Escape. Typing J in chat or another input must not toggle the journal.
- Check the upper-left help control and upper-right close control, selection highlighting, empty state, long titles/descriptions, requirement/reward tooltips and scrolling in Polish and English.
- Check landscape and narrow/portrait layouts, including the six QuickAccess buttons and their separation from the action bars.
- Keep the journal open while kill and collection progress changes. Confirm the selected quest and status update; remove a required item and confirm a ready quest returns to in-progress.
- Abandon an accepted quest and a ready-to-turn-in quest. Confirm no item/EXP change, tracker removal, immediate NPC availability and acceptance again without reloading the scene.
- Complete a quest at its NPC. Confirm it remains in the journal, has fulfilled requirements and offers no abandon action.
- Attempt abandonment during trade and after a rejected/failed request. Confirm the journal recovers, pending UI clears and authoritative state is retained.
- Reconnect after abandoning or handing in a quest. Confirm the journal, tracker and NPC agree with persisted state.

Builds and isolated editor probes do not establish these authenticated networking and pointer-interaction checks.

## Automated evidence (2026-10-29)

- API solution build passed; all 695 backend tests passed, including abandonment, reacceptance, stale-ID replay, completed/foreign records, concurrency rejection and OpenAPI coverage. Persistence tests use the existing in-memory harness, not a live SQL Server race.
- Unity imported the new scripts and processed Netcode in an isolated project. Generated client and server-symbol builds passed; InventoryRegression passed its gear, ammo and trade/quest checks with engine stubs.
- QuestJournalAuthoring.Validate covers PL/EN, three canvas sizes, selection, empty state, saved completed progress, reward quantities, tooltips and panel width. Rendered previews are stored under the isolated project's Logs directory; the selected preview is copied to Client/Logs/quest-journal-preview.png.
- The authenticated runtime checks above remain pending.

## UI follow-up (2026-10-30)

- Generated client and editor projects compiled. An isolated Unity editor validation of the current repository scene passed with rendering enabled for Polish and English at 1280×720, 720×1280 and 360×800. It checked the quick-access texture, header alignment, finished-quest abandon visibility, selection and three wrapped titles with row spacing.
- Representative captures are in `Client/Logs/quest-journal-landscape.png`, `quest-journal-portrait.png` and `quest-journal-long-titles.png`; the editor log is `Client/Logs/quest-journal-validate-render.log`. These ignored files are local evidence, not Play Mode or pointer-interaction tests.
