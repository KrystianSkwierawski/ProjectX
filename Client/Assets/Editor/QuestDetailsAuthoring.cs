using System.Reflection;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Inventory;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Quest;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Quest.Mono;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using System;
using Assets.Scripts.Areas.Quest.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectX.Editor
{
    public static class QuestDetailsAuthoring
    {
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/UIScene.unity");
            var canvas = GameObject.Find("QuestCanvas");
            ((RectTransform)canvas.transform.Find("Quest")).sizeDelta = new Vector2(480, 500);
            var content = canvas.transform.Find("Quest/Description/Viewport/Content");
            var description = content.Find("Text").GetComponent<TextMeshProUGUI>();
            var layout = content.GetComponent<VerticalLayoutGroup>() ?? content.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 12;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var textFitter = description.GetComponent<ContentSizeFitter>();

            if (textFitter != null)
            {
                UnityEngine.Object.DestroyImmediate(textFitter);
            }

            description.rectTransform.sizeDelta = Vector2.zero;
            foreach (var name in new[] { "RequirementsTitle", "Objective", "Requirements", "RewardsTitle", "Rewards" })
            {
                var existing = content.Find(name);

                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
            }

            AddText(description, content, "RequirementsTitle");
            AddContainer(content, "Requirements");
            AddText(description, content, "RewardsTitle");
            AddContainer(content, "Rewards");

            var row = new GameObject("QuestItemRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            row.GetComponent<LayoutElement>().minHeight = 64;

            var slot = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/InventorySlot.prefab"));
            slot.name = "Slot";
            slot.transform.SetParent(row.transform, false);
            var slotLayout = slot.GetComponent<LayoutElement>() ?? slot.AddComponent<LayoutElement>();
            slotLayout.minWidth = slotLayout.preferredWidth = 64;
            slotLayout.minHeight = slotLayout.preferredHeight = 64;
            slotLayout.flexibleWidth = 0;
            var label = AddText(description, row.transform, "Label");
            label.fontSize = 20;
            label.margin = Vector4.zero;
            var labelLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minWidth = 0;
            labelLayout.flexibleWidth = 1;

            var prefab = PrefabUtility.SaveAsPrefabAsset(row, "Assets/Prefabs/Quest/QuestItemRow.prefab");
            UnityEngine.Object.DestroyImmediate(row);
            var ui = UnityEngine.Object.FindFirstObjectByType<QuestUI>();
            var serialized = new SerializedObject(ui);
            serialized.FindProperty("_itemRowPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("QUEST_DETAILS_AUTHORED");
        }

        public static void ValidateQuestRefresh()
        {
            var manager = new QuestManager();
            var applySnapshot = typeof(QuestManager).GetMethod("ApplyCharacterQuestSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
            var original = new CharacterQuestDto
            {
                Id = 42,
                QuestId = QuestEnum.FieldSupplies,
                Status = CharacterQuestStatusEnum.Accepted
            };
            applySnapshot.Invoke(manager, new object[] { 1, new[] { original } });

            var npcObject = new GameObject("QuestRefreshProbe");

            try
            {
                var npc = npcObject.AddComponent<QuestNpc>();
                npc.CharacterQuest = original;
                var previousStatus = original.Status;
                var finished = new CharacterQuestDto
                {
                    Id = 42,
                    QuestId = QuestEnum.FieldSupplies,
                    Status = CharacterQuestStatusEnum.Finished,
                    Progress = 5,
                    ObjectiveProgress = new System.Collections.Generic.Dictionary<string, int> { ["kill:Bean(Clone)"] = 2 }
                };

                applySnapshot.Invoke(manager, new object[] { 1, new[] { finished } });

                if (!ReferenceEquals(npc.CharacterQuest, manager.CharacterQuests[0])
                    || npc.CharacterQuest.Status != CharacterQuestStatusEnum.Finished
                    || npc.CharacterQuest.Progress != 5
                    || npc.CharacterQuest.ObjectiveProgress["kill:Bean(Clone)"] != 2
                    || previousStatus != CharacterQuestStatusEnum.Accepted)
                {
                    throw new InvalidOperationException("Quest refresh lost the NPC reference or progress transition.");
                }

                finished.Status = CharacterQuestStatusEnum.Accepted;
                finished.Progress = 2;
                applySnapshot.Invoke(manager, new object[] { 1, new[] { finished } });

                if (npc.CharacterQuest.Status != CharacterQuestStatusEnum.Accepted || npc.CharacterQuest.Progress != 2)
                {
                    throw new InvalidOperationException("NPC did not observe requirement regression.");
                }

                manager.CharacterQuests[0].Status = CharacterQuestStatusEnum.Finished;

                if (npc.CharacterQuest.Status != CharacterQuestStatusEnum.Finished)
                {
                    throw new InvalidOperationException("NPC did not observe progress after refresh.");
                }

                applySnapshot.Invoke(manager, new object[] { 2, new[] { finished } });

                if (ReferenceEquals(original, manager.CharacterQuests[0]))
                {
                    throw new InvalidOperationException("Quest identity leaked between characters.");
                }

                applySnapshot.Invoke(manager, new object[] { 2, Array.Empty<CharacterQuestDto>() });

                if (manager.CharacterQuests.Count != 0)
                {
                    throw new InvalidOperationException("Removed quests survived the snapshot.");
                }

                Debug.Log("QUEST_REFRESH_VALIDATED: NPC identity, status transitions, progress updates and character isolation.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(npcObject);
            }
        }

        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/UIScene.unity");
            var ui = UnityEngine.Object.FindFirstObjectByType<QuestUI>();
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryUI>();
            Invoke(ui, "Awake");
            Invoke(inventory, "Awake");
            Invoke(inventory, "InitTextures");
            typeof(QuestManager).GetProperty("CharacterQuests").SetValue(QuestManager.Instance, Array.Empty<CharacterQuestDto>());
            InventoryManager.Instance.Replace(new CharacterInventoryDto
            {
                Count = 15,
                Inventory = new InventoryDto { Items = new[] { new InventoryItemDto { Type = InventoryItemEnum.Can, Count = 1 }, new InventoryItemDto { Type = InventoryItemEnum.Fish, Count = 1 } } }
            });
            UserManager.Instance.SetCharacterLanguage(LanguageEnum.en);
            ui.Start();
            ui.Quest.SetActive(true);
            var npc = new GameObject("QuestValidationNpc").AddComponent<QuestNpc>();
            npc.Quest = new QuestDto
            {
                Id = QuestEnum.FieldSupplies,
                Title = "Field supplies",
                Description = "Gather the field supplies.",
                CompleteDescription = "The field supplies are ready.",
                Requirements = new[]
                {
                    new QuestRequirementDto { Id = "item:100", Type = QuestTypeEnum.Collect,
                        Target = nameof(InventoryItemEnum.Can), ItemType = InventoryItemEnum.Can, Count = 2, StatusText = "Can" },
                    new QuestRequirementDto { Id = "kill:Bean(Clone)", Type = QuestTypeEnum.Kill,
                        Target = "Bean(Clone)", Count = 2, StatusText = "Killed {0}/{1} beans" },
                    new QuestRequirementDto { Id = "item:200", Type = QuestTypeEnum.Collect,
                        Target = nameof(InventoryItemEnum.Fish), ItemType = InventoryItemEnum.Fish, Count = 1, StatusText = "Fish" }
                },
                Reward = 1500,
                StatusText = "Collect cans: {0}/{1}",
                ItemRewards = new[]
                {
                    new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 40 },
                    new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 3 }
                }
            };
            npc.CharacterQuest = new CharacterQuestDto { Status = CharacterQuestStatusEnum.Accepted, Progress = 1 };
            typeof(QuestUI).GetProperty("DisplayedNpc").SetValue(ui, npc);
            Invoke(ui, "ShowDetails", npc.Quest);

            var scroll = ui.Quest.transform.Find("Description").GetComponent<ScrollRect>();
            var content = scroll.content;
            Require(content.Find("Requirements").childCount == 3, "Mixed requirement rows");
            Require(content.Find("Rewards").childCount == 3, "EXP, currency and item slots");
            Require(!ui.QuestAcceptButton.interactable, "Accepted quest is read-only");
            Require(ui.QuestDescriptionText.text == npc.Quest.Description, "Accepted quest description");
            var logText = (string)typeof(QuestUI).GetMethod("FormatProgress", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { npc.Quest, npc.CharacterQuest });

            Require(logText.StartsWith("Field supplies\n- ") && logText.Split('\n').Length == 4,
                "Quest log title and objective lines");
            var label = content.Find("Requirements").GetChild(0).Find("Label").GetComponent<TextMeshProUGUI>();
            Require(label.text.EndsWith("1/2") && label.color == ColorUI.White, "Missing requirements");

            InventoryManager.Instance.Dto.Inventory.Items[0].Count = 3;
            npc.CharacterQuest.Status = CharacterQuestStatusEnum.Finished;
            npc.CharacterQuest.Progress = 4;
            npc.CharacterQuest.ObjectiveProgress["kill:Bean(Clone)"] = 2;
            Invoke(ui, "RefreshRequirements");
            Require(label.text.EndsWith("2/2") && label.color == ColorUI.Green, "Requirement refresh and clamped count");
            Require(ui.QuestAcceptButton.interactable, "Finished quest can be completed");
            Require(ui.QuestDescriptionText.text == npc.Quest.CompleteDescription, "Completion description refresh");

            npc.CharacterQuest.Status = CharacterQuestStatusEnum.Accepted;
            Invoke(ui, "RefreshRequirements");

            Require(ui.QuestDescriptionText.text == npc.Quest.Description, "Description restored after regression");
            Require(!ui.QuestAcceptButton.interactable, "Regressed quest cannot be completed");

            npc.CharacterQuest.Status = CharacterQuestStatusEnum.Finished;
            Invoke(ui, "RefreshRequirements");

            var canvas = ui.QuestCanvas.GetComponent<Canvas>();
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvas.transform;

            foreach (var language in new[] { LanguageEnum.en, LanguageEnum.pl })
            {
                UserManager.Instance.SetCharacterLanguage(language);
                npc.Quest.CompleteDescription = TranslateManager.Instance.GetByKey("Collect2CansDescription");
                Invoke(ui, "RefreshRequirements");

                var expectedAction = TranslateManager.Instance.GetByKey(TranslateKeyEnum.QuestObjectiveCollect);
                var expectedTarget = TranslateManager.Instance.GetByKey(TranslateKeyEnum.CanObjectiveTarget);
                Require(label.text == $"{expectedAction} {expectedTarget} 2/2", "Localized objective");

                foreach (var size in new[] { new Vector2(1920, 1080), new Vector2(1280, 720), new Vector2(720, 1280), new Vector2(480, 900) })
                {
                    canvasRect.sizeDelta = size;
                    Invoke(ui, "RefreshPanelBounds");
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ui.Quest.transform);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                    var requirements = content.Find("Requirements");
                    var collectLabel = (RectTransform)requirements.GetChild(0).Find("Label");
                    var killLabel = (RectTransform)requirements.GetChild(1).Find("Label");
                    var collectBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, collectLabel);
                    var killBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, killLabel);
                    var killSlot = requirements.GetChild(1).Find("Slot");

                    Require(Mathf.Abs(collectBounds.min.x - killBounds.min.x) < 1, "Objective labels align");
                    Require(killSlot.Find("Background").GetComponent<RawImage>().texture != null
                        && !killSlot.GetComponent<HoverUI>().enabled, "Kill objective icon is visible and non-interactive");
                    Require(content.rect.height > scroll.viewport.rect.height, "Details remain scrollable");
                    var panelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, ui.Quest.transform);
                    Require(panelBounds.min.x >= canvasRect.rect.xMin - 1 && panelBounds.max.x <= canvasRect.rect.xMax + 1,
                        "Panel stays inside narrow canvas");
                    var requirementsGap = SectionGap(content, "RequirementsTitle", "Requirements");
                    var rewardsGap = SectionGap(content, "RewardsTitle", "Rewards");
                    Require(Mathf.Abs(requirementsGap - rewardsGap) < 1, "Equal heading-to-content spacing");

                    foreach (Transform row in content.Find("Rewards"))
                    {
                        var slot = (RectTransform)row.Find("Slot");
                        var text = row.Find("Label").GetComponent<TextMeshProUGUI>();
                        Require(slot.rect.width >= 63 && slot.rect.height >= 63, "Readable slot size");
                        Require(text.rectTransform.rect.width > 80, "Responsive label width");
                        Require(text.preferredHeight <= ((RectTransform)row).rect.height + 1, "No clipped reward label");
                        var image = slot.Find("Background").GetComponent<RawImage>();
                        var amount = slot.Find("Text").GetComponent<TextMeshProUGUI>();

                        Require(image.texture != null && image.color == ColorUI.White, "Visible reward icon");
                        Require(amount.gameObject.activeInHierarchy && !string.IsNullOrEmpty(amount.text), "Prefab count is visible");
                        Require(!text.text.Contains("×") && !text.text.Contains(amount.text), "Name does not duplicate count");
                    }
                }
            }

            ValidateLootLayout(ui, inventory, canvasRect);

            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                UserManager.Instance.SetCharacterLanguage(LanguageEnum.en);
                ui.QuestTitleText.text = TranslateManager.Instance.GetByKey("FieldSuppliesTitle");
                ui.QuestDescriptionText.text = TranslateManager.Instance.GetByKey("FieldSuppliesDescription");
                canvasRect.sizeDelta = new Vector2(1280, 720);
                Invoke(ui, "RefreshPanelBounds");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                scroll.verticalNormalizedPosition = 1;
                Capture((RectTransform)ui.Quest.transform, "Logs/quest-panel-top.png");
                scroll.verticalNormalizedPosition = 0;
                Capture((RectTransform)ui.Quest.transform, "Logs/quest-panel-bottom.png");
            }

            Debug.Log("QUEST_DETAILS_VALIDATED: collect counts, rewards, button states, PL/EN, landscape and portrait layouts.");
        }

        private static void ValidateLootLayout(QuestUI ui, InventoryUI inventory, RectTransform canvas)
        {
            var lootProperty = typeof(InventoryUI).GetProperty("Loot");
            var originalLoot = inventory.Loot;
            var originalCanvasSize = canvas.sizeDelta;
            var lootCanvasObject = new GameObject("QuestLootCanvasProbe", typeof(RectTransform), typeof(Canvas));
            var lootCanvas = (RectTransform)lootCanvasObject.transform;
            var loot = new GameObject("QuestLootLayoutProbe", typeof(RectTransform));
            var lootRect = (RectTransform)loot.transform;
            var panel = (RectTransform)ui.Quest.transform;
            var panelCorners = new Vector3[4];
            var lootCorners = new Vector3[4];

            lootCanvas.SetParent(canvas.parent, false);
            lootCanvas.anchorMin = canvas.anchorMin;
            lootCanvas.anchorMax = canvas.anchorMax;
            lootCanvas.pivot = canvas.pivot;
            lootCanvas.localPosition = canvas.localPosition;
            lootCanvas.localRotation = canvas.localRotation;
            lootCanvas.localScale = canvas.localScale;
            lootCanvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            lootRect.SetParent(lootCanvas, false);
            lootRect.anchorMin = lootRect.anchorMax = Vector2.zero;
            lootRect.pivot = Vector2.zero;
            lootRect.sizeDelta = new Vector2(86, 216);
            lootProperty.SetValue(inventory, loot);

            try
            {
                foreach (var size in new[] { new Vector2(655, 917), new Vector2(480, 900), new Vector2(1280, 720), new Vector2(1920, 1080) })
                {
                    canvas.sizeDelta = size;
                    lootCanvas.sizeDelta = size;
                    lootRect.anchoredPosition = new Vector2(Mathf.Min(704, size.x - 102), 270);
                    loot.SetActive(true);
                    Canvas.ForceUpdateCanvases();
                    Invoke(ui, "RefreshPanelBounds");
                    Canvas.ForceUpdateCanvases();
                    panel.GetWorldCorners(panelCorners);
                    lootRect.GetWorldCorners(lootCorners);

                    var panelMin = canvas.InverseTransformPoint(panelCorners[0]);
                    var panelMax = canvas.InverseTransformPoint(panelCorners[2]);
                    var lootMin = canvas.InverseTransformPoint(lootCorners[0]);
                    var lootMax = canvas.InverseTransformPoint(lootCorners[2]);

                    Require(panel.rect.width > 0 && panel.rect.height > 0 && lootRect.rect.width > 0 && lootRect.rect.height > 0,
                        $"Positive panel and loot dimensions at {size}");
                    Require(panelMax.x <= lootMin.x - 7 || panelMin.x >= lootMax.x + 7
                        || panelMin.y >= lootMax.y + 7 || panelMax.y <= lootMin.y - 7,
                        $"Quest and loot do not overlap at {size}");

                    loot.SetActive(false);
                    Invoke(ui, "RefreshPanelBounds");
                    Require(Mathf.Abs(panel.rect.width - Mathf.Min(480, size.x - 32)) < 1
                        && Mathf.Abs(panel.rect.height - Mathf.Min(500, size.y - 32)) < 1,
                        $"Quest size is restored after hiding loot at {size}");
                }
            }
            finally
            {
                lootProperty.SetValue(inventory, originalLoot);
                UnityEngine.Object.DestroyImmediate(lootCanvasObject);
                canvas.sizeDelta = originalCanvasSize;
                Invoke(ui, "RefreshPanelBounds");
            }
        }

        private static float SectionGap(RectTransform content, string heading, string rows)
        {
            var headingBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, content.Find(heading));
            var rowBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, content.Find(rows).GetChild(0));

            return headingBounds.min.y - rowBounds.max.y;
        }

        private static void Capture(RectTransform panel, string path)
        {
            Canvas.ForceUpdateCanvases();
            var camera = new GameObject("QuestCapture").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = panel.rect.height / 2 + 16;
            camera.transform.position = panel.TransformPoint(panel.rect.center) + Vector3.back * 100;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorUI.Background;
            var target = new RenderTexture(800, 850, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(800, 850, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 800, 850), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());

            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }

        private static void Invoke(object target, string method, params object[] args)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static TextMeshProUGUI AddText(TextMeshProUGUI source, Transform parent, string name)
        {
            var text = UnityEngine.Object.Instantiate(source, parent);
            text.name = name;
            text.text = string.Empty;
            text.rectTransform.sizeDelta = Vector2.zero;
            text.raycastTarget = false;

            return text;
        }

        private static void AddContainer(Transform parent, string name)
        {
            var container = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            container.transform.SetParent(parent, false);
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }
    }
}
