using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Quest;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Enums;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectX.Editor
{
    public static class QuestJournalAuthoring
    {
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/UIScene.unity");
            var canvas = GameObject.Find("QuestCanvas");
            var old = canvas.transform.Find("Journal");

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var journal = Object.Instantiate(canvas.transform.Find("Quest").gameObject, canvas.transform);
            journal.name = "Journal";
            var panel = (RectTransform)journal.transform;
            panel.anchorMin = panel.anchorMax = new Vector2(0, 1);
            panel.pivot = new Vector2(0, 1);
            panel.sizeDelta = new Vector2(880, 560);
            journal.transform.Find("Title").gameObject.SetActive(false);
            foreach (var translation in journal.transform.Find("AcceptButton").GetComponentsInChildren<TranslateUI>(true))
            {
                Object.DestroyImmediate(translation);
            }

            var help = journal.transform.Find("Help");
            var helpRect = (RectTransform)help;
            helpRect.anchoredPosition = new Vector2(helpRect.anchoredPosition.x, -24);
            var closeRect = (RectTransform)journal.transform.Find("Close");
            closeRect.anchoredPosition = new Vector2(closeRect.anchoredPosition.x, -24);

            var helpSerialized = new SerializedObject(help.GetComponent<WindowHelpUI>());
            helpSerialized.FindProperty("_translationKey").stringValue = "HelpJournal";
            helpSerialized.ApplyModifiedPropertiesWithoutUndo();

            var sourceList = GameObject.Find("CraftingCanvas").transform.Find("Crafting/Recipes");
            var list = Object.Instantiate(sourceList.gameObject, journal.transform);
            list.name = "List";
            list.SetActive(true);
            var listContent = list.transform.Find("Viewport/Content");

            foreach (Transform child in listContent)
            {
                Object.DestroyImmediate(child.gameObject);
            }

            Object.DestroyImmediate(listContent.GetComponent<GridLayoutGroup>());
            listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            listContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var status = Object.Instantiate(journal.transform.Find("Title").gameObject, journal.transform);
            status.name = "Status";
            var empty = Object.Instantiate(journal.transform.Find("Title").gameObject, journal.transform);
            empty.name = "Empty";
            empty.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            journal.SetActive(false);

            var ui = canvas.GetComponent<QuestJournalUI>() ?? canvas.AddComponent<QuestJournalUI>();
            var serialized = new SerializedObject(ui);
            serialized.FindProperty("_textButtonPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Shared/UI/TextButton.prefab");
            serialized.FindProperty("_itemRowPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestItemRow.prefab");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var bar = GameObject.Find("QuickAccessCanvas").transform.Find("QuickAccessBar");
            var existing = bar.Find("Journal");

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var slot = Object.Instantiate(bar.Find("Inventory").gameObject, bar);
            slot.name = "Journal";
            var background = slot.transform.Find("Background");
            background.GetComponent<RawImage>().texture = null;
            background.gameObject.SetActive(false);

            var icon = slot.transform.Find("Text").GetComponent<TMP_Text>();
            icon.text = "\uF02D";
            icon.color = ColorUI.White;
            icon.gameObject.SetActive(true);
            slot.transform.Find("Preview/Title").GetComponent<TMP_Text>().text = "";
            var translations = slot.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (var translation in translations)
            {
                if (translation == null)
                {
                    continue;
                }

                var item = new SerializedObject(translation);
                var key = item.FindProperty("_key");

                if (translation is TranslateUI && key != null)
                {
                    key.intValue = (int)TranslateKeyEnum.QuickAccessJournal;
                    item.ApplyModifiedPropertiesWithoutUndo();
                }

                var field = item.FindProperty("_translationKey");

                if (field != null && field.propertyType == SerializedPropertyType.String)
                {
                    field.stringValue = "QuickAccessJournal";
                    item.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var layout = bar.GetComponent<GridLayoutGroup>();
            layout.constraintCount = 6;
            ((RectTransform)bar).sizeDelta = new Vector2(340, 50);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("QUEST_JOURNAL_AUTHORED");
        }

        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/UIScene.unity");
            var canvas = GameObject.Find("QuestCanvas");
            var journal = canvas.transform.Find("Journal");
            var bar = GameObject.Find("QuickAccessCanvas").transform.Find("QuickAccessBar");

            if (journal == null || canvas.GetComponent<QuestJournalUI>() == null || journal.Find("Help") == null
                || journal.Find("Close") == null || journal.Find("List/Viewport/Content") == null
                || journal.Find("Description/Viewport/Content/Requirements") == null
                || journal.Find("Description/Viewport/Content/Rewards") == null || bar.Find("Journal") == null)
            {
                throw new System.InvalidOperationException("Journal scene references are incomplete.");
            }

            var icon = bar.Find("Journal/Text").GetComponent<TMP_Text>();
            var inventoryIcon = bar.Find("Inventory/Text").GetComponent<TMP_Text>();
            Require(icon.gameObject.activeInHierarchy && !bar.Find("Journal/Background").gameObject.activeSelf
                && icon.text == "\uF02D" && icon.font == inventoryIcon.font && icon.color == ColorUI.White,
                "Journal quick access uses the white Font Awesome book icon");
            var journalHelp = (RectTransform)journal.Find("Help");
            var journalClose = (RectTransform)journal.Find("Close");
            Require(Mathf.Approximately(journalHelp.anchoredPosition.y, -24)
                && Mathf.Approximately(journalClose.anchoredPosition.y, -24),
                "Help and close are aligned near the top of the journal");

            var inventory = Object.FindFirstObjectByType<InventoryUI>();
            var ui = canvas.GetComponent<QuestJournalUI>();
            Invoke(inventory, "Awake");
            Invoke(inventory, "InitTextures");
            Invoke(ui, "Awake");
            UserManager.Instance.SetCharacterLanguage(LanguageEnum.en);
            var quest = new QuestDto
            {
                Id = QuestEnum.FieldSupplies,
                Title = "Field supplies",
                Description = "Gather supplies.",
                CompleteDescription = "Ready to turn in.",
                Requirements = new[]
                {
                    new QuestRequirementDto { Id = "item:100", Type = QuestTypeEnum.Collect, ItemType = InventoryItemEnum.Can, Count = 2 },
                    new QuestRequirementDto { Id = "kill:Bean(Clone)", Type = QuestTypeEnum.Kill, Target = "Bean(Clone)", Count = 2 }
                },
                Reward = 100,
                ItemRewards = new[] { new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 4 } }
            };
            var characterQuest = new CharacterQuestDto
            {
                Id = 5,
                QuestId = quest.Id,
                Status = CharacterQuestStatusEnum.Finished,
                ObjectiveProgress = new Dictionary<string, int> { ["item:100"] = 2, ["kill:Bean(Clone)"] = 2 }
            };
            typeof(QuestManager).GetProperty("Quests").SetValue(QuestManager.Instance, new[] { quest });
            typeof(QuestManager).GetProperty("CharacterQuests").SetValue(QuestManager.Instance, new List<CharacterQuestDto> { characterQuest });
            Invoke(ui, "Start");
            journal.gameObject.SetActive(true);
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.GetComponent<CanvasScaler>().enabled = false;
            canvasComponent.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvas.transform;

            foreach (var language in new[] { LanguageEnum.en, LanguageEnum.pl })
            {
                UserManager.Instance.SetCharacterLanguage(language);
                quest.Title = TranslateManager.Instance.GetByKey(TranslateKeyEnum.FieldSuppliesTitle);
                quest.Description = TranslateManager.Instance.GetByKey(TranslateKeyEnum.FieldSuppliesDescription);
                quest.CompleteDescription = TranslateManager.Instance.GetByKey(TranslateKeyEnum.FieldSuppliesCompleteDescription);

                foreach (var size in new[] { new Vector2(1280, 720), new Vector2(720, 1280), new Vector2(360, 800) })
                {
                    canvasRect.sizeDelta = size;
                    Invoke(ui, "UpdateLayout", size);
                    ui.Refresh();
                    Canvas.ForceUpdateCanvases();
                    var list = journal.Find("List/Viewport/Content");
                    var label = list.GetChild(list.childCount - 1).GetComponent<TMP_Text>();
                    var details = journal.Find("Description/Viewport/Content");
                    var panel = (RectTransform)journal;
                    var corners = new Vector3[4];
                    panel.GetWorldCorners(corners);
                    var left = canvasRect.InverseTransformPoint(corners[0]).x;
                    var right = canvasRect.InverseTransformPoint(corners[2]).x;
                    Require(label.text == quest.Title && label.color == ColorUI.Green, "Selected quest title and green selection");
                    var title = journal.Find("Title").GetComponent<TMP_Text>();
                    Require(!title.gameObject.activeSelf, "Redundant header title is hidden");
                    Require(label.textWrappingMode == TextWrappingModes.Normal && label.margin.x > 0,
                        "Quest list titles wrap with margins");
                    Require(journal.Find("AcceptButton").gameObject.activeSelf, "Finished quest shows abandon action");
                    Require(journal.Find("Status").GetComponent<TMP_Text>().text.Contains(TranslateManager.Instance.GetByKey(TranslateKeyEnum.JournalStatusReady)), "Localized status");
                    Require(details.Find("Requirements").childCount == 2 && details.Find("Rewards").childCount == 2, "Requirement and reward rows");

                    foreach (Transform reward in details.Find("Rewards"))
                    {
                        var count = reward.Find("Slot/Text").GetComponent<TMP_Text>();
                        Require(count.gameObject.activeSelf && !string.IsNullOrEmpty(count.text), "Reward count is visible");
                    }

                    Require(details.Find("Rewards").GetChild(0).Find("Slot/Text").GetComponent<TMP_Text>().text == "100"
                        && details.Find("Rewards").GetChild(1).Find("Slot/Text").GetComponent<TMP_Text>().text == "4", "Reward counts match quest");
                    Require(details.Find("Requirements").GetChild(0).Find("Slot").GetComponent<HoverUI>().enabled, "Collect item tooltip");
                    Require(!details.Find("Requirements").GetChild(1).Find("Slot").GetComponent<HoverUI>().enabled, "Kill icon is not interactive");
                    Require(left >= canvasRect.rect.xMin - 1 && right <= canvasRect.rect.xMax + 1, "Panel fits canvas width");

                    if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    {
                        Capture(panel, $"Logs/quest-journal-{language}-{(int)size.x}x{(int)size.y}.png");
                    }
                }
            }

            characterQuest.Status = CharacterQuestStatusEnum.Completed;
            ui.Refresh();
            Require(!journal.Find("AcceptButton").gameObject.activeSelf, "Completed quest cannot be abandoned");
            Require(journal.Find("Description/Viewport/Content/Requirements").GetChild(0).Find("Label").GetComponent<TMP_Text>().text.EndsWith("2/2"), "Completed quest retains objective progress");
            var secondQuest = new QuestDto { Id = QuestEnum.Kill2Beans, Title = "Second quest", Description = "Another quest" };
            var secondCharacterQuest = new CharacterQuestDto { Id = 6, QuestId = secondQuest.Id, Status = CharacterQuestStatusEnum.Accepted };
            typeof(QuestManager).GetProperty("Quests").SetValue(QuestManager.Instance, new[] { quest, secondQuest });
            QuestManager.Instance.CharacterQuests.Add(secondCharacterQuest);
            ui.Refresh();
            var questList = journal.Find("List/Viewport/Content");
            Require(questList.childCount == 2, "Two quests listed");
            questList.GetChild(1).GetComponent<Button>().onClick.Invoke();
            Require(journal.Find("Description/Viewport/Content/Text").GetComponent<TMP_Text>().text == secondQuest.Description, "Selecting another quest updates details");
            Require(questList.GetChild(0).GetComponent<TMP_Text>().color == ColorUI.White
                && questList.GetChild(1).GetComponent<TMP_Text>().color == ColorUI.Green, "Only selected quest is green");

            quest.Title = "A very long quest title that must wrap over several lines in the journal list";
            secondQuest.Title = "Another long quest title that should remain separate from its neighbours";
            var thirdQuest = new QuestDto { Id = QuestEnum.Collect2Cans, Title = "A third long quest title for checking list spacing" };
            typeof(QuestManager).GetProperty("Quests").SetValue(QuestManager.Instance, new[] { quest, secondQuest, thirdQuest });
            QuestManager.Instance.CharacterQuests.Add(new CharacterQuestDto { Id = 7, QuestId = thirdQuest.Id, Status = CharacterQuestStatusEnum.Accepted });
            ui.Refresh();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)questList);

            for (var i = 0; i < questList.childCount; i++)
            {
                var row = (RectTransform)questList.GetChild(i);
                var text = row.GetComponent<TMP_Text>();
                var preferredHeight = text.GetPreferredValues(text.text, row.rect.width - text.margin.x - text.margin.z, 0).y;
                Require(row.rect.height >= preferredHeight,
                    $"Wrapped title fits row height: actual={row.rect.height}, preferred={preferredHeight}, width={row.rect.width}");

                if (i > 0)
                {
                    var previous = (RectTransform)questList.GetChild(i - 1);
                    Require(previous.localPosition.y - previous.rect.height / 2 > row.localPosition.y + row.rect.height / 2,
                        "Quest rows have vertical spacing");
                }
            }

            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Capture((RectTransform)journal, "Logs/quest-journal-long-titles.png");
            }

            QuestManager.Instance.CharacterQuests.Clear();
            ui.Refresh();
            Require(journal.Find("Empty").gameObject.activeSelf && !journal.Find("Description").gameObject.activeSelf
                && !journal.Find("AcceptButton").gameObject.activeSelf, "Empty journal hides details and action");
            Debug.Log("QUEST_JOURNAL_VALIDATED: PL/EN, selection, empty state, requirements, rewards, completed state and canvas bounds.");
        }

        private static void Capture(RectTransform panel, string path)
        {
            Canvas.ForceUpdateCanvases();
            var camera = new GameObject("JournalCapture").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = panel.rect.height / 2 + 16;
            camera.transform.position = panel.TransformPoint(panel.rect.center) + Vector3.back * 100;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorUI.Background;
            const int width = 1000;
            var height = Mathf.Max(1, Mathf.RoundToInt(width * (panel.rect.height + 32) / (panel.rect.width + 32)));
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
        }

        private static void Invoke(object target, string name, params object[] arguments)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            method.Invoke(target, arguments);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
