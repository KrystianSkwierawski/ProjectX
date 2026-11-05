using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectX.Editor
{
    public static class ChatItemLinkValidation
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("ProjectX/Chat/Validate Item Links")]
        public static void Run()
        {
            Require(!Application.isPlaying && NetworkManager.Singleton == null, "Run the isolated probe in Edit Mode.");

            Require(ChatItemLink.TryEncode(InventoryItemEnum.IronSword, out var sword), "Sword encoding");
            Require(ChatItemLink.TryEncode(InventoryItemEnum.Fish, out var fish), "Fish encoding");
            Require(ChatItemLink.TryDecode(sword, out var decoded) && decoded == InventoryItemEnum.IronSword, "Item round trip");
            Require(!ChatItemLink.TryEncode(InventoryItemEnum.None, out _), "Empty slot rejection");
            Require(!ChatItemLink.TryEncode(InventoryItemEnum.AmmoTemplate, out _), "Template rejection");
            Require(!ChatItemLink.TryEncode((InventoryItemEnum)999, out _), "Unknown item rejection");
            Require(ChatItemLink.IsValidMessage($"Mam {fish} i {sword}"), "Mixed Unicode message");
            var maxLinks = ChatItemLink.MaximumItemLinks;
            Require(ChatItemLink.IsValidMessage(new string(sword, maxLinks) + new string('a', 200 - maxLinks)), "Two hundred mixed units");
            Require(!ChatItemLink.IsValidMessage(new string(sword, maxLinks + 1)), "Item link limit");
            Require(!ChatItemLink.IsValidMessage(new string('a', 201)), "Transport limit");
            Require(!ChatItemLink.IsValidMessage("Unknown \uE001"), "Reserved unknown character rejection");
            Require(!ChatItemLink.IsValidMessage($"Template {(char)(0xE000 + (int)InventoryItemEnum.AmmoTemplate)}"), "Reserved template rejection");

            var scene = EditorSceneManager.NewPreviewScene();

            try
            {
                ValidatePresentation(scene, sword, fish);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            Debug.Log("CHAT_ITEM_LINKS_VALIDATED: codec, 200-character limit, draft selection and pointer handoff, miniature slots, wrapping, hover layer and row reuse.");
        }

        private static void ValidatePresentation(Scene scene, char sword, char fish)
        {
            var canvasObject = new GameObject("ChatItemLinkProbe", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(360, 720);

            var fieldObject = new GameObject("Draft", typeof(RectTransform), typeof(ChatInputField));
            fieldObject.transform.SetParent(canvasObject.transform, false);
            var input = fieldObject.GetComponent<ChatInputField>();

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(fieldObject.transform, false);
            var mesh = textObject.GetComponent<TextMeshProUGUI>();
            mesh.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Futura PT Book SDF.asset");
            mesh.fontSize = 16;
            mesh.alignment = TextAlignmentOptions.Left;
            mesh.rectTransform.sizeDelta = new Vector2(260, 32);
            ((RectTransform)fieldObject.transform).sizeDelta = new Vector2(260, 32);

            input.textViewport = (RectTransform)fieldObject.transform;
            input.textComponent = mesh;

            var rendererType = typeof(ChatUI).Assembly.GetType("Assets.Scripts.Areas.Shared.UI.ChatItemText", throwOnError: true);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/InventorySlot.prefab");
            var textures = new Dictionary<InventoryItemEnum, Texture>
            {
                [InventoryItemEnum.IronSword] = Resources.Load<Texture>("Icons/IronSword"),
                [InventoryItemEnum.Fish] = Resources.Load<Texture>("Icons/Fish")
            };

            rendererType.GetMethod("Bind").Invoke(null, new object[] { mesh, prefab, textures, false });
            var renderer = textObject.GetComponent(rendererType);
            var sourceFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Futura PT Book SDF.asset");
            var sourceMaterial = sourceFont.material;
            var sourceAtlas = sourceFont.atlasTextures[0];
            var runtimeFont = mesh.font;
            Require(runtimeFont.atlasPopulationMode == AtlasPopulationMode.Static, "Runtime font does not repack the source atlas");
            runtimeFont.ReadFontAssetDefinition();
            Require(runtimeFont.characterLookupTable.ContainsKey(''), "Icon glyph survives a font lookup rebuild");
            Require(!sourceFont.characterLookupTable.ContainsKey(''), "Source font has no synthetic glyph");
            var textButton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Shared/UI/TextButton.prefab");
            Require(textButton.GetComponent<ButtonUI>() != null, "Recipe rows use the consuming button");

            var buttonObject = new GameObject("ClickProbe", typeof(RectTransform), typeof(ButtonUI));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            var button = buttonObject.GetComponent<ButtonUI>();
            var clicks = 0;
            var rightClicks = 0;
            button.onClick.AddListener(() => clicks++);
            button.OnRightClick.AddListener(() => rightClicks++);
            var consumed = new PointerEventData(null) { button = PointerEventData.InputButton.Left, pointerId = 5 };
            typeof(ButtonUI).GetField("_consumedPointer", PrivateInstance).SetValue(button, (int?)5);
            button.OnPointerClick(consumed);
            Require(consumed.used && clicks == 0, "Consumed left click does not invoke the normal action");
            var right = new PointerEventData(null) { button = PointerEventData.InputButton.Right, pointerId = 5 };
            button.OnPointerDown(right);
            Require(rightClicks == 1 && !right.used, "Right click retains its ordinary action");
            button.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left, pointerId = 5 });
            Require(clicks == 1, "Next ordinary left click is not consumed");

            var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
            var insert = typeof(ChatInputField).GetMethod("TryInsertItem", PrivateInstance);

            input.text = "mam stary miecz";
            input.selectionStringAnchorPosition = 9;
            input.selectionStringFocusPosition = 4;

            // Supply the Control modifier without injecting a device into the editor's input system.
            typeof(TMP_InputField).GetField("m_AllowInput", PrivateInstance).SetValue(input, true);
            typeof(ChatInputField).GetMethod("HandleDeselect", PrivateInstance).Invoke(input, new object[] { pointer, true });

            Require(!input.isFocused, "Deselect deactivates the draft before dispatch");

            var differentPointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
            Require(!(bool)insert.Invoke(input, new object[] { InventoryItemEnum.IronSword, differentPointer }), "Pointer identity");
            Require((bool)insert.Invoke(input, new object[] { InventoryItemEnum.IronSword, pointer }), "Pending focused draft insertion");
            Require(input.text == $"mam {sword} miecz", "Reversed selection replaced by one token");
            Require(input.selectionStringAnchorPosition == 5 && input.selectionStringFocusPosition == 5, "Caret follows the inserted token");
            Require(!(bool)insert.Invoke(input, new object[] { InventoryItemEnum.Fish, pointer }), "Pointer handoff consumed once");
            Require((bool)typeof(TMP_InputField).GetField("m_ShouldActivateNextUpdate", PrivateInstance).GetValue(input), "Input schedules refocus");
            Require(!input.onFocusSelectAll, "Refocus preserves the caret");

            RefreshMesh(mesh, renderer);
            RequireIcon(mesh, textures[InventoryItemEnum.IronSword]);
            Require(!mesh.transform.Find("ChatItemIcon").GetComponent<ButtonUI>().enabled, "Draft icons keep caret clicks available");

            var draftIcon = (RectTransform)mesh.transform.Find("ChatItemIcon");
            Require(draftIcon.localPosition.y + draftIcon.rect.yMax <= mesh.rectTransform.rect.yMax
                && draftIcon.localPosition.y + draftIcon.rect.yMin >= mesh.rectTransform.rect.yMin,
                "Draft icon fits vertically inside the authored 32-unit viewport");
            Require(runtimeFont.atlasTextures[0].GetPixel(0, 0).a == 0,
                "Caret tracking glyph cannot draw a dot from the placeholder atlas");

            input.text = $"{sword}{fish}";
            RefreshMesh(mesh, renderer);
            var adjacentIcons = mesh.GetComponentsInChildren<ButtonUI>();
            Require(adjacentIcons.Length == 2, "Adjacent item icons rendered");
            var firstRect = (RectTransform)adjacentIcons[0].transform;
            var secondRect = (RectTransform)adjacentIcons[1].transform;
            Require(secondRect.localPosition.x + secondRect.rect.xMin
                - firstRect.localPosition.x - firstRect.rect.xMax >= 5.9f,
                "Adjacent icons have three units of padding on each side");

            input.text = new string(sword, ChatItemLink.MaximumItemLinks + 1);
            RefreshMesh(mesh, renderer);
            var draftIcons = mesh.GetComponentsInChildren<ButtonUI>(true);
            Require(draftIcons.Length == ChatItemLink.MaximumItemLinks, $"Excess pasted links stay plain glyphs. Icon count: {draftIcons.Length}");

            // TMP_InputField enforces single-line layout on its own label.
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(canvasObject.transform, false);
            ((RectTransform)viewport.transform).sizeDelta = new Vector2(100, 200);

            var rowObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            rowObject.transform.SetParent(viewport.transform, false);
            var row = rowObject.GetComponent<TextMeshProUGUI>();
            row.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Futura PT Book SDF.asset");
            row.fontSize = mesh.fontSize;
            row.alignment = TextAlignmentOptions.TopLeft;
            row.textWrappingMode = TextWrappingModes.Normal;
            row.rectTransform.sizeDelta = new Vector2(75, 180);

            rendererType.GetMethod("Bind").Invoke(null, new object[] { row, prefab, textures, true });
            var rowRenderer = rowObject.GetComponent(rendererType);

            row.text = $"A {sword} a longer phrase {fish} <b>tekst</b>";
            RefreshMesh(row, rowRenderer);

            Require(row.textInfo.lineCount > 1, "Narrow message wraps");
            Require(row.textInfo.characterCount == row.text.Length, "Mixed text retains one character per token and literal markup");
            var secondIcon = row.GetComponentsInChildren<ButtonUI>(true)[1].transform;
            Require(secondIcon.gameObject.activeSelf, "Wrapped second item visible");
            Require(secondIcon.GetComponent<ButtonUI>().enabled, "Sent links can be linked again");
            Require(secondIcon.Find("Background").GetComponent<RawImage>().texture == textures[InventoryItemEnum.Fish], "Second item art");

            ValidateHover(row.transform.Find("ChatItemIcon"), canvasObject.transform);

            row.text = string.Empty;
            RefreshMesh(row, rowRenderer);

            Require(!secondIcon.gameObject.activeSelf && !row.transform.Find("ChatItemIcon").gameObject.activeSelf, "No icons survive an empty pooled row");

            row.text = $"Szept {fish}";
            RefreshMesh(row, rowRenderer);

            RequireIcon(row, textures[InventoryItemEnum.Fish]);
            Require(!secondIcon.gameObject.activeSelf, "Reused row only shows its current item");

            // Edit-mode preview components do not receive the normal Play Mode teardown callbacks.
            rendererType.GetMethod("OnDestroy", PrivateInstance).Invoke(rowRenderer, null);
            rendererType.GetMethod("OnDestroy", PrivateInstance).Invoke(renderer, null);

            UnityEngine.Object.DestroyImmediate(rowObject);
            UnityEngine.Object.DestroyImmediate(fieldObject);
            Require(runtimeFont == null, "Unused runtime font was destroyed");
            Require(sourceMaterial != null && sourceAtlas != null && sourceFont.material == sourceMaterial
                && sourceFont.atlasTextures[0] == sourceAtlas, "Destroying a renderer preserves the source material and atlas");
        }

        private static void ValidateHover(Transform icon, Transform canvas)
        {
            var tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(Canvas), typeof(TooltipLayerUI));
            tooltip.transform.SetParent(canvas, false);

            var inventory = new GameObject("Inventory", typeof(InventoryUI));
            inventory.transform.SetParent(canvas, false);

            var inventoryInstance = typeof(MonoSingleton<InventoryUI>).GetProperty("Instance");
            var tooltipInstance = typeof(MonoSingleton<TooltipLayerUI>).GetProperty("Instance");
            var previousInventory = InventoryUI.Instance;
            var previousTooltip = TooltipLayerUI.Instance;

            try
            {
                inventoryInstance.SetValue(null, inventory.GetComponent<InventoryUI>());
                tooltipInstance.SetValue(null, tooltip.GetComponent<TooltipLayerUI>());

                var hover = icon.GetComponent<HoverUI>();
                var preview = icon.Find("Preview");
                var comparison = preview.GetComponent<ItemComparisonUI>();
                hover.Start();
                hover.OnPointerEnter(null);
                comparison.Refresh();

                Require(preview.gameObject.activeInHierarchy && preview.parent == tooltip.transform, "Hover moves the preview outside the scroll mask");
                var title = preview.Find("Title").GetComponent<TextMeshProUGUI>().text;
                var description = preview.Find("Description").GetComponent<TextMeshProUGUI>().text;
                var translations = TranslateManager.Instance;
                Require(title == translations.GetByKey("IronSwordTitle") && !string.IsNullOrWhiteSpace(title), "Hover title matches the linked sword");
                Require(description.Contains(translations.GetByKey(TranslateKeyEnum.GearScore)), "Hover uses the standard item description");

                var character = new CharacterDto { WeaponType = InventoryItemEnum.IronBow };
                Require(ItemComparisonUI.EquippedFor(InventoryItemEnum.IronSword, character) == InventoryItemEnum.IronBow, "Linked gear uses its equipped comparison slot");

                hover.OnPointerExit(null);
                Require(preview.parent == icon && !preview.gameObject.activeSelf, "Pointer exit restores and hides the preview");
                hover.OnPointerEnter(null);
                hover.OnDisable();
                Require(!preview.gameObject.activeSelf, "Disabling a linked slot hides its external tooltip");
                hover.OnPointerExit(null);
            }
            finally
            {
                tooltipInstance.SetValue(null, previousTooltip);
                inventoryInstance.SetValue(null, previousInventory);
            }
        }

        private static void RefreshMesh(TextMeshProUGUI mesh, Component renderer)
        {
            mesh.ForceMeshUpdate();
            renderer.GetType().GetMethod("LateUpdate", PrivateInstance).Invoke(renderer, null);
        }

        private static void RequireIcon(TextMeshProUGUI mesh, Texture texture)
        {
            var icon = (RectTransform)mesh.transform.Find("ChatItemIcon");

            Require(icon != null && icon.gameObject.activeSelf, "Item overlay rendered");
            Require(icon.rect.width >= 23 && icon.rect.width <= 25, "Readable icon width");
            Require(!icon.Find("Text").gameObject.activeSelf, "No stack count");
            Require(texture != null && icon.Find("Background").GetComponent<RawImage>().texture == texture, "Matching inventory art");
            Require(icon.Find("Preview").GetComponent<ItemComparisonUI>() != null, "Standard item comparison binding");
            Require(!icon.Find("Preview").gameObject.activeSelf, "Preview initially hidden");
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
