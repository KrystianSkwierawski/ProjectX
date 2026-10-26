using System;
using System.IO;
using System.Reflection;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectX.Editor
{
    public static class ItemComparisonValidation
    {
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var inventory = new GameObject("InventoryUI").AddComponent<InventoryUI>();
            InvokeAwake(inventory);
            var cursor = new GameObject("CursorUI").AddComponent<CursorUI>();
            InvokeAwake(cursor);

            var network = new GameObject("NetworkManager").AddComponent<NetworkManager>();
            typeof(NetworkManager).GetProperty("Singleton").SetValue(null, network);
            var character = new CharacterDto
            {
                HelmetType = InventoryItemEnum.IronHelmet,
                ChestType = InventoryItemEnum.IronChest,
                BootsType = InventoryItemEnum.IronBoots,
                WeaponType = InventoryItemEnum.IronBow,
                AmmoType = InventoryItemEnum.AmmoArrow1,
                AmmoCount = 20,
            };
            UserManager.Instance.Characters[network.LocalClientId] = character;

            Require(ItemComparisonUI.EquippedFor(InventoryItemEnum.ScoutHelmet, character) == character.HelmetType, "Helmet slot");
            Require(ItemComparisonUI.EquippedFor(InventoryItemEnum.GuardChest, character) == character.ChestType, "Chest slot");
            Require(ItemComparisonUI.EquippedFor(InventoryItemEnum.TrailBoots, character) == character.BootsType, "Boots slot");
            Require(ItemComparisonUI.EquippedFor(InventoryItemEnum.BalancedSword, character) == character.WeaponType, "Cross-category weapon slot");
            Require(ItemComparisonUI.EquippedFor(InventoryItemEnum.Fish, character) == InventoryItemEnum.None, "Non-gear exclusion");
            Require(ItemComparisonUI.EquippedFor(InventoryItemEnum.IronSword, null) == InventoryItemEnum.None, "Missing character");

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvas.transform;
            var slot = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/InventorySlot.prefab"), canvas.transform);
            var slotRect = (RectTransform)slot.transform;
            slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta = new Vector2(48, 48);
            slotRect.anchoredPosition = Vector2.zero;
            var preview = slot.transform.Find("Preview").gameObject;
            var item = new InventoryItemDto { Type = InventoryItemEnum.BalancedSword, Count = 1 };
            ItemComparisonUI.Bind(preview, () => item);
            var comparison = preview.GetComponent<ItemComparisonUI>();
            var hover = slot.GetComponent<HoverUI>();
            hover.Start();

            var key = slot.GetInstanceID().ToString();
            OnPointerEnterSubscription.Instance.Subscribe(key, _ => preview.SetActive(true));
            OnPointerExitSubscription.Instance.Subscribe(key, _ => preview.SetActive(false));

            foreach (var language in new[] { LanguageEnum.en, LanguageEnum.pl })
            {
                UserManager.Instance.SetCharacterLanguage(language);

                foreach (var size in new[] { new Vector2(1920, 1080), new Vector2(1280, 720), new Vector2(720, 1280), new Vector2(480, 900) })
                {
                    canvasRect.sizeDelta = size;
                    hover.OnPointerEnter(null);
                    comparison.Refresh();
                    Canvas.ForceUpdateCanvases();
                    var panel = (RectTransform)preview.transform.Find("EquippedComparison");
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, preview.transform);
                    Require(bounds.min.x >= canvasRect.rect.xMin - 1 && bounds.max.x <= canvasRect.rect.xMax + 1
                        && bounds.min.y >= canvasRect.rect.yMin - 1 && bounds.max.y <= canvasRect.rect.yMax + 1, $"Bounds {language} {size}: {bounds}");
                    Require(size.x < 532 ? panel.anchorMin == Vector2.zero : panel.anchorMin == Vector2.one, "Responsive pair layout");

                    var description = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>().text;
                    Require(description.Contains("<color=#65D98A>(+14)</color>") && description.Contains("<color=#FF7474>(-12)</color>"), "Positive and negative weapon stats");
                    Require(preview.GetComponentsInChildren<Graphic>().Length >= 6, "Both authored card contents exist");

                    foreach (var graphic in preview.GetComponentsInChildren<Graphic>())
                    {
                        Require(!graphic.raycastTarget, "Tooltip must not steal pointer");
                    }

                    hover.OnPointerExit(null);
                    Require(!panel.gameObject.activeInHierarchy, "Pointer exit hides companion");
                }
            }

            item.Type = InventoryItemEnum.IronBow;
            hover.OnPointerEnter(null);
            comparison.Refresh();
            Require(!preview.transform.Find("Description").GetComponent<TextMeshProUGUI>().text.Contains("<color=#65D98A>"), "Equal item has no false improvement");

            item.Type = InventoryItemEnum.IronHelmet;
            character.HelmetType = InventoryItemEnum.ScoutHelmet;
            comparison.Refresh();
            Require(preview.transform.Find("Description").GetComponent<TextMeshProUGUI>().text.Contains(": 0 <color=#FF7474>(-8)</color>"), "Lost stat is visible at zero");

            character.HelmetType = InventoryItemEnum.HelmetTemplate;
            comparison.Refresh();
            Require(!preview.transform.Find("EquippedComparison").gameObject.activeSelf, "Empty equipped slot hides companion");

            item.Type = InventoryItemEnum.Fish;
            comparison.Refresh();
            Require(!preview.transform.Find("EquippedComparison").gameObject.activeSelf, "Pool reuse for non-gear");
            item.Count = 0;
            comparison.Refresh();
            Require(!preview.activeSelf, "Empty hovered item hides preview");

            foreach (var type in new[] { InventoryItemEnum.ScoutHelmet, InventoryItemEnum.GuardChest, InventoryItemEnum.TrailBoots, InventoryItemEnum.BalancedSword })
            {
                Require(Resources.Load<Texture2D>($"Icons/{type}") != null, $"Icon {type}");
                Require(!string.IsNullOrWhiteSpace(TranslateManager.Instance.GetByKey($"{type}Description")), $"Description {type}");
                Require(type.IsActionBarItem() && type.GetGearSlot() != InventoryItemEnum.None, $"Usable gear {type}");
            }

            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                item.Type = InventoryItemEnum.GuardChest;
                item.Count = 1;
                canvasRect.sizeDelta = new Vector2(1280, 720);
                hover.OnPointerEnter(null);
                comparison.Refresh();
                Capture(canvasRect, preview.transform);
            }

            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
            Debug.Log("ITEM_COMPARISON_VALIDATED: slots, deltas, missing stats, pointer exit, pool reuse, PL/EN, four layouts and imported icons.");
        }

        private static void Capture(RectTransform canvas, Transform preview)
        {
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas, preview);
            var camera = new GameObject("ComparisonCapture").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y + 24, 190);
            camera.transform.position = canvas.TransformPoint(bounds.center) + Vector3.back * 100;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.13f);

            var target = new RenderTexture(1000, 600, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1000, 600, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1000, 600), 0, 0);
            image.Apply();
            File.WriteAllBytes("Logs/item-comparison-preview.png", image.EncodeToPNG());

            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }

        private static void InvokeAwake(MonoBehaviour component)
        {
            component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
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
