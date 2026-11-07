using System;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Combat;
using Assets.Scripts.Areas.Combat.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectX.Editor
{
    public static class DamageMeterAuthoring
    {
        private const string RowPath = "Assets/Prefabs/Shared/UI/DamageMeterRow.prefab";

        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/UIScene.unity");
            var canvas = GameObject.Find("QuestCanvas");
            var old = canvas.transform.Find("DamageMeter");

            if (old != null) Object.DestroyImmediate(old.gameObject);

            var source = canvas.transform.Find("Quest");
            var panel = Object.Instantiate(source.gameObject, canvas.transform);
            panel.name = "DamageMeter";
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1, 0);
            panelRect.anchoredPosition = new Vector2(-16, 16);
            panelRect.sizeDelta = new Vector2(440, 250);

            foreach (Transform child in panel.transform.Cast<Transform>().ToArray())
            {
                if (child.name != "Title" && child.name != "Help" && child.name != "Close" && child.name != "AcceptButton")
                    Object.DestroyImmediate(child.gameObject);
            }

            var title = panel.transform.Find("Title");
            title.gameObject.SetActive(true);
            Place((RectTransform)title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(52, -44), new Vector2(-52, -12));
            foreach (var translation in title.GetComponentsInChildren<TranslateUI>(true)) Object.DestroyImmediate(translation);
            var titleText = title.GetComponent<TMP_Text>();
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 14;
            titleText.fontSizeMax = 16;
            titleText.margin = Vector4.zero;
            var help = panel.transform.Find("Help");
            var close = panel.transform.Find("Close");
            ((RectTransform)help).anchoredPosition = new Vector2(28, -28);
            ((RectTransform)close).anchoredPosition = new Vector2(-28, -28);
            var helpData = new SerializedObject(help.GetComponent<WindowHelpUI>());
            helpData.FindProperty("_translationKey").stringValue = "HelpDamageMeter";
            helpData.ApplyModifiedPropertiesWithoutUndo();

            var reset = panel.transform.Find("AcceptButton");
            reset.name = "Reset";
            foreach (var translation in reset.GetComponentsInChildren<TranslateUI>(true)) Object.DestroyImmediate(translation);
            Place((RectTransform)reset, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-75, 12), new Vector2(75, 46));
            reset.gameObject.SetActive(true);

            var list = Object.Instantiate(GameObject.Find("CraftingCanvas").transform.Find("Crafting/Recipes").gameObject, panel.transform);
            list.name = "List";
            list.SetActive(true);
            Place((RectTransform)list.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(12, 58), new Vector2(-12, -56));
            Place((RectTransform)list.transform.Find("Viewport"), Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0));
            var content = list.transform.Find("Viewport/Content");
            var contentRect = (RectTransform)content;
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            list.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
            foreach (Transform child in content.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null) Object.DestroyImmediate(grid);
            var layout = content.GetComponent<VerticalLayoutGroup>() ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.spacing = 3;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var empty = Object.Instantiate(title.gameObject, panel.transform);
            empty.name = "Empty";
            empty.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
            Place((RectTransform)empty.transform, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(12, -20), new Vector2(-12, 20));

            var row = new GameObject("DamageMeterRow", typeof(RectTransform), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().preferredHeight = 28;
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(row.transform, false);
            Place((RectTransform)bar.transform, Vector2.zero, Vector2.one, new Vector2(33, 1), new Vector2(-1, -1));
            bar.GetComponent<Image>().color = ColorUI.Surface;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            Place((RectTransform)fill.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.GetComponent<Image>().color = ColorUI.Accent;
            var slot = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/InventorySlot.prefab"));
            slot.name = "Slot";
            slot.transform.SetParent(row.transform, false);
            Place((RectTransform)slot.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(4, -13), new Vector2(30, 13));
            foreach (var childName in new[] { "Background", "Outline", "IconBackdrop" })
            {
                Place((RectTransform)slot.transform.Find(childName), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            MakeIconSquare(slot.transform);
            slot.transform.Find("Preview").gameObject.SetActive(false);
            slot.transform.Find("Text").gameObject.SetActive(false);
            foreach (var behaviour in slot.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            foreach (var graphic in slot.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var name = Object.Instantiate(title.gameObject, row.transform);
            name.name = "Name";
            Place((RectTransform)name.transform, Vector2.zero, Vector2.one, new Vector2(38, 2), new Vector2(-135, -2));
            var nameText = name.GetComponent<TMP_Text>();
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.fontSizeMin = 11;
            nameText.fontSizeMax = 16;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            var value = Object.Instantiate(name, row.transform);
            value.name = "Value";
            Place((RectTransform)value.transform, new Vector2(1, 0), Vector2.one, new Vector2(-133, 2), new Vector2(-6, -2));
            value.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;
            var rowPrefab = PrefabUtility.SaveAsPrefabAsset(row, RowPath);
            Object.DestroyImmediate(row);
            panel.SetActive(false);
            var ui = canvas.GetComponent<DamageMeterUI>() ?? canvas.AddComponent<DamageMeterUI>();
            var data = new SerializedObject(ui);
            data.FindProperty("_rowPrefab").objectReferenceValue = rowPrefab;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ApplyUiFixes();
            Debug.Log("DAMAGE_METER_AUTHORED");
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static void ApplyUiFixes()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/UIScene.unity");
            var bar = GameObject.Find("QuickAccessCanvas").transform.Find("QuickAccessBar");
            var existing = bar.Find("DamageMeter");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var slot = Object.Instantiate(bar.Find("Journal").gameObject, bar);
            slot.name = "DamageMeter";
            slot.transform.Find("Text").GetComponent<TMP_Text>().text = "\uF080";
            foreach (var translation in slot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (translation == null) continue;

                var item = new SerializedObject(translation);
                var key = item.FindProperty("_key");
                if (translation is TranslateUI && key != null)
                {
                    key.intValue = (int)TranslateKeyEnum.QuickAccessDamageMeter;
                    item.ApplyModifiedPropertiesWithoutUndo();
                }

                var field = item.FindProperty("_translationKey");
                if (field != null && field.propertyType == SerializedPropertyType.String)
                {
                    field.stringValue = "QuickAccessDamageMeter";
                    item.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var layout = bar.GetComponent<GridLayoutGroup>();
            layout.constraintCount = 7;
            ((RectTransform)bar).sizeDelta = new Vector2(400, 50);

            var gear = GameObject.Find("GearCanvas").transform.Find("Gear/LeftPanel/Ammo/Text");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/InventorySlot.prefab").transform.Find("Text");
            EditorUtility.CopySerialized(source.GetComponent<TMP_Text>(), gear.GetComponent<TMP_Text>());
            var gearRect = (RectTransform)gear;
            var sourceRect = (RectTransform)source;
            gearRect.anchorMin = sourceRect.anchorMin;
            gearRect.anchorMax = sourceRect.anchorMax;
            gearRect.pivot = sourceRect.pivot;
            gearRect.anchoredPosition = sourceRect.anchoredPosition;
            gearRect.sizeDelta = sourceRect.sizeDelta;

            var row = PrefabUtility.LoadPrefabContents(RowPath);
            MakeIconSquare(row.transform.Find("Slot"));
            PrefabUtility.SaveAsPrefabAsset(row, RowPath);
            PrefabUtility.UnloadPrefabContents(row);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("DAMAGE_METER_UI_FIXES_AUTHORED");
        }

        private static void MakeIconSquare(Transform slot)
        {
            slot.Find("Outline").gameObject.SetActive(false);
            slot.Find("IconBackdrop").gameObject.SetActive(false);
            var background = slot.Find("Background");
            foreach (var component in background.GetComponents<MonoBehaviour>())
            {
                if (component != null && new SerializedObject(component).FindProperty("radius") != null)
                    component.enabled = false;
            }

            background.GetComponent<RawImage>().material = null;
        }

        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/UIScene.unity");
            var canvas = GameObject.Find("QuestCanvas");
            var panel = canvas.transform.Find("DamageMeter");
            var ui = canvas.GetComponent<DamageMeterUI>();
            var inventory = GameObject.Find("InventoryCanvas").transform.Find("Inventory");
            var chat = Object.FindFirstObjectByType<ChatUI>();
            var chatPanel = (RectTransform)GameObject.Find("ChatCanvas").transform.Find("Container");
            var bar = GameObject.Find("QuickAccessCanvas").transform.Find("QuickAccessBar");
            var slot = bar.Find("DamageMeter");
            if (panel == null || ui == null || slot == null || slot.Find("Text").GetComponent<TMP_Text>().text != "\uF080")
                throw new InvalidOperationException("Damage meter UI references are incomplete.");

            var icon = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath).transform.Find("Slot/Background");
            foreach (var component in icon.GetComponents<MonoBehaviour>())
            {
                if (component != null && new SerializedObject(component).FindProperty("radius") != null && component.enabled)
                    throw new InvalidOperationException("Damage meter icon rounding is enabled.");
            }

            Invoke(ui, "Start");
            Invoke(chat, "Start");
            ui.Toggle();
            Canvas.ForceUpdateCanvases();
            Invoke(ui, "LateUpdate");
            var panelCorners = new Vector3[4];
            var inventoryCorners = new Vector3[4];
            var chatCorners = new Vector3[4];
            var statusCorners = new Vector3[4];
            var playerStatus = (RectTransform)GameObject.Find("PlayerCanvas").transform.Find("Player");
            var help = (RectTransform)panel.Find("Help");
            var close = (RectTransform)panel.Find("Close");
            var inventoryHelp = (RectTransform)inventory.Find("Help");
            if (help.anchoredPosition != inventoryHelp.anchoredPosition || close.anchoredPosition != new Vector2(-28, -28))
                throw new InvalidOperationException("Damage meter header does not match inventory.");

            var listRect = (RectTransform)panel.Find("List");
            var viewportRect = (RectTransform)panel.Find("List/Viewport");
            var resetRect = (RectTransform)panel.Find("Reset");
            if (listRect.offsetMin != new Vector2(12, 58) || listRect.offsetMax != new Vector2(-12, -56)
                || viewportRect.offsetMin.x != 6 || viewportRect.offsetMax.x != -6
                || resetRect.offsetMin.y != 12)
                throw new InvalidOperationException("Damage meter content or footer margins are incorrect.");
            ((RectTransform)panel).GetWorldCorners(panelCorners);
            ((RectTransform)inventory).GetWorldCorners(inventoryCorners);
            if (Mathf.Abs(panelCorners[0].y - inventoryCorners[0].y) > .1f
                || Mathf.Abs(panelCorners[2].y - inventoryCorners[2].y) > .1f)
                throw new InvalidOperationException("Damage meter is not aligned with inventory.");

            var position = ((RectTransform)panel).anchoredPosition;
            inventory.gameObject.SetActive(true);
            Invoke(ui, "LateUpdate");
            if (((RectTransform)panel).anchoredPosition != position)
                throw new InvalidOperationException("Inventory visibility moved the damage meter.");
            var translated = slot.Find("Preview/Title").GetComponent<TranslateUI>();
            if (translated == null || new SerializedObject(translated).FindProperty("_key").intValue != (int)TranslateKeyEnum.QuickAccessDamageMeter)
                throw new InvalidOperationException("Damage meter shortcut tooltip translation is missing.");

            var inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
            Invoke(inventoryUI, "Awake");
            Invoke(inventoryUI, "InitTextures");
            DamageMeterState.Reset();
            DamageMeterState.Record(1, "First", InventoryItemEnum.IronSword, 7500);
            DamageMeterState.Record(2, "Second", InventoryItemEnum.IronBow, 2500);
            var content = panel.Find("List/Viewport/Content");
            if (content.childCount != 2 || content.GetChild(0).Find("Name").GetComponent<TMP_Text>().text != "First"
                || !Mathf.Approximately(((RectTransform)content.GetChild(0).Find("Bar/Fill")).anchorMax.x, .75f))
                throw new InvalidOperationException("Damage meter populated rows are incorrect.");

            for (var i = 3; i <= 12; i++)
                DamageMeterState.Record(i, "Player " + i, InventoryItemEnum.IronSword, 100);
            var scroll = panel.Find("List").GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0;
            ui.Refresh();
            Canvas.ForceUpdateCanvases();
            if (!scroll.vertical || content.childCount != 12 || scroll.verticalNormalizedPosition > .01f)
                throw new InvalidOperationException("Damage meter scrolling or refresh failed.");
            panel.Find("Reset").GetComponent<Button>().onClick.Invoke();
            if (DamageMeterState.Entries.Count != 0)
                throw new InvalidOperationException("Damage meter reset failed.");

            var ammoText = GameObject.Find("GearCanvas").transform.Find("Gear/LeftPanel/Ammo/Text").GetComponent<TMP_Text>();
            typeof(GearUI).GetMethod("ConfigureAmmoCount", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { ammoText });
            ammoText.text = "1024";
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(item => item.isRootCanvas)
                .ToArray();
            var camera = new GameObject("DamageMeterLayoutCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = Vector3.back * 100;
            foreach (var item in canvases)
            {
                var scaler = item.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = false;
                item.renderMode = RenderMode.WorldSpace;
                item.worldCamera = camera;
                item.transform.position = Vector3.zero;
                item.transform.localScale = Vector3.one;
            }

            DamageMeterState.Record(1, "First", InventoryItemEnum.IronSword, 7500);
            DamageMeterState.Record(2, "Second", InventoryItemEnum.IronBow, 2500);
            foreach (var size in new[] { new Vector2(1280, 720), new Vector2(720, 1280), new Vector2(360, 800) })
            {
                camera.orthographicSize = size.y / 2;
                camera.aspect = size.x / size.y;
                foreach (var item in canvases) ((RectTransform)item.transform).sizeDelta = size;
                Canvas.ForceUpdateCanvases();
                Invoke(ui, "LateUpdate");
                Invoke(chat, "LateUpdate");
                ((RectTransform)panel).GetWorldCorners(panelCorners);
                ((RectTransform)inventory).GetWorldCorners(inventoryCorners);
                chatPanel.GetWorldCorners(chatCorners);
                playerStatus.GetWorldCorners(statusCorners);
                if (chatCorners[0].x < -size.x / 2 + 16 - .1f
                    || (size.x != 360 && Mathf.Abs(chatCorners[0].x - (-size.x / 2 + 16)) > .1f)
                    || chatCorners[2].y > size.y / 2 - 16 + .1f)
                    throw new InvalidOperationException("Chat is outside its expected canvas margins at " + size);
                if (size.x == 1280 && (Mathf.Abs(chatCorners[0].y - inventoryCorners[0].y) > .1f
                    || Mathf.Abs(chatCorners[2].y - inventoryCorners[2].y) > .1f))
                    throw new InvalidOperationException("Chat is not aligned with inventory.");
                if (chatCorners[0].x < panelCorners[2].x && chatCorners[2].x > panelCorners[0].x
                    && chatCorners[0].y < panelCorners[2].y && chatCorners[2].y > panelCorners[0].y)
                    throw new InvalidOperationException("Chat overlaps damage meter at " + size);
                if (chatCorners[0].x < inventoryCorners[2].x && chatCorners[2].x > inventoryCorners[0].x
                    && chatCorners[0].y < inventoryCorners[2].y && chatCorners[2].y > inventoryCorners[0].y)
                    throw new InvalidOperationException("Chat overlaps inventory at " + size);
                if (chatCorners[0].x < statusCorners[2].x && chatCorners[2].x > statusCorners[0].x
                    && chatCorners[0].y < statusCorners[2].y && chatCorners[2].y > statusCorners[0].y)
                    throw new InvalidOperationException("Chat overlaps player status at " + size);
                if (panelCorners[0].x < -size.x / 2 - .1f || panelCorners[2].y > size.y / 2 + .1f)
                    throw new InvalidOperationException("Damage meter exceeds screen bounds at " + size);
                if (size.x == 360)
                {
                    if (Mathf.Abs(panelCorners[0].y - inventoryCorners[2].y - 10) > .1f)
                        throw new InvalidOperationException("Damage meter is not stacked above inventory.");
                }
                else if (Mathf.Abs(panelCorners[0].y - inventoryCorners[0].y) > .1f
                    || Mathf.Abs(panelCorners[2].y - inventoryCorners[2].y) > .1f)
                    throw new InvalidOperationException("Damage meter is not aligned with inventory.");
                if (size.x == 1280 && Mathf.Abs(inventoryCorners[0].x - panelCorners[2].x - 10) > .1f)
                    throw new InvalidOperationException("Damage meter gap is incorrect.");
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    CaptureDocking(camera, size, $"Logs/damage-meter-docking-{(int)size.x}x{(int)size.y}.png");
                if (size.x == 1280 && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    var gear = GameObject.Find("GearCanvas").transform.Find("Gear");
                    panel.gameObject.SetActive(false);
                    inventory.gameObject.SetActive(false);
                    gear.gameObject.SetActive(true);
                    ammoText.gameObject.SetActive(true);
                    ammoText.enabled = true;
                    ammoText.transform.parent.GetComponent<RawImage>().texture = inventoryUI.Textures[InventoryItemEnum.AmmoArrow3];
                    Canvas.ForceUpdateCanvases();
                    CaptureDocking(camera, size, "Logs/damage-meter-gear-ammo.png");
                    ammoText.ForceMeshUpdate();
                    if (!ammoText.enableAutoSizing || ammoText.textWrappingMode != TextWrappingModes.NoWrap
                        || ammoText.textBounds.size.x > ammoText.rectTransform.rect.width + .1f
                        || ammoText.textBounds.size.y > ammoText.rectTransform.rect.height + .1f)
                        throw new InvalidOperationException("Ammo count does not fit its slot.");

                    gear.gameObject.SetActive(false);
                    inventory.gameObject.SetActive(true);
                    panel.gameObject.SetActive(true);
                }
            }

            Debug.Log("DAMAGE_METER_UI_FIXES_VALIDATED");
        }

        private static void CaptureDocking(Camera camera, Vector2 size, string path)
        {
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture((int)size.x, (int)size.y, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousFlags = camera.clearFlags;
            var previousBackground = camera.backgroundColor;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorUI.Background;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D((int)size.x, (int)size.y, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previousActive;
            camera.targetTexture = previousTarget;
            camera.clearFlags = previousFlags;
            camera.backgroundColor = previousBackground;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }

        private static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
    }
}
