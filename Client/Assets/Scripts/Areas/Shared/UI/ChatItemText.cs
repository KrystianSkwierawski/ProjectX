using System.Collections.Generic;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore;

namespace Assets.Scripts.Areas.Shared.UI
{
    internal sealed class ChatItemText : MonoBehaviour, ITextPreprocessor
    {
        // One private glyph preserves the token's text and caret indices while reserving icon space.
        private const char Placeholder = '\uF8FF';
        private const float IconSize = 24f;
        private const float IconPadding = 3f;
        private sealed class FontEntry
        {
            public TMP_FontAsset Font;
            public int Users;
        }

        private static readonly Dictionary<(TMP_FontAsset, float), FontEntry> Fonts = new();
        private (TMP_FontAsset, float) _fontKey;
        private TextMeshProUGUI _mesh;
        private GameObject _slotPrefab;
        private IDictionary<InventoryItemEnum, Texture> _textures;
        private readonly List<(RectTransform Rect, RawImage Image, Graphic[] Artwork, HoverUI Hover, InventoryItemDto Item)> _icons = new();
        private readonly List<(InventoryItemEnum Type, Vector2 Center, float Size)> _placements = new();
        private bool _linkSource;
        private bool _dirty;
        private string _preparedText;

        public static void Bind(TextMeshProUGUI mesh, GameObject slotPrefab, IDictionary<InventoryItemEnum, Texture> textures, bool linkSource)
        {
            var renderer = mesh.gameObject.AddComponent<ChatItemText>();
            renderer._mesh = mesh;
            renderer._slotPrefab = slotPrefab;
            renderer._textures = textures;
            renderer._linkSource = linkSource;

            // A tiny owned atlas reserves icon space; normal letters use the original font as fallback.
            renderer._fontKey = (mesh.font, mesh.fontSize);

            if (!Fonts.TryGetValue(renderer._fontKey, out var entry))
            {
                var font = Instantiate(mesh.font);
                font.hideFlags = HideFlags.HideAndDontSave;
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                font.fallbackFontAssetTable = new List<TMP_FontAsset> { mesh.font };

                var atlas = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                // TMP synthesizes zero-width caret characters in this atlas too.
                // Keep it transparent so their SDF padding cannot leave a dot.
                atlas.SetPixel(0, 0, Color.clear);
                atlas.Apply();
                font.atlasTextures = new[] { atlas };
                font.material = new Material(mesh.font.material) { hideFlags = HideFlags.HideAndDontSave };
                font.material.mainTexture = atlas;
                font.glyphTable.Clear();
                font.characterTable.Clear();

                var units = font.faceInfo.pointSize / mesh.fontSize / font.faceInfo.scale;
                var metrics = new GlyphMetrics(IconSize * units, IconSize * units, 0,
                    font.faceInfo.ascentLine, (IconSize + 2f * IconPadding) * units);
                var glyph = new Glyph(1, metrics, new GlyphRect(0, 0, 1, 1), 1f, 0);
                font.glyphTable.Add(glyph);
                font.characterTable.Add(new TMP_Character(Placeholder, font, glyph));
                font.ReadFontAssetDefinition();
                entry = new FontEntry { Font = font };
                Fonts.Add(renderer._fontKey, entry);
            }

            entry.Users++;
            mesh.font = entry.Font;
            mesh.lineSpacing += 10f;
            mesh.richText = false;
            mesh.parseCtrlCharacters = false;
            mesh.textPreprocessor = renderer;
            mesh.OnPreRenderText += renderer.PrepareIcons;
        }

        public string PreprocessText(string text)
        {
            char[] buffer = null;
            var linkCount = 0;

            for (var index = 0; index < text.Length; index++)
            {
                if (ChatItemLink.IsReserved(text[index]))
                {
                    buffer ??= text.ToCharArray();
                    buffer[index] = ChatItemLink.TryDecode(text[index], out _) && ++linkCount <= ChatItemLink.MaximumItemLinks
                        ? Placeholder
                        : '\uFFFD';
                }
            }

            return buffer == null ? text : new string(buffer);
        }

        private void PrepareIcons(TMP_TextInfo info)
        {
            _dirty = true;
            _placements.Clear();
            var text = _mesh.text;
            _preparedText = text;

            for (var index = 0; index < info.characterCount; index++)
            {
                var character = info.characterInfo[index];

                if (!character.isVisible || character.character != Placeholder || character.index >= text.Length
                    || !ChatItemLink.TryDecode(text[character.index], out var type))
                {
                    continue;
                }

                var width = character.xAdvance - character.origin;
                var height = character.ascender - character.descender;
                var size = Mathf.Max(1f, width - 2f * IconPadding);
                var center = new Vector2(character.origin + width * 0.5f, character.descender + height * 0.5f);

                _placements.Add((type, center, size));

                // Keep the original glyph's advance and caret/selection geometry.
                var colors = info.meshInfo[character.materialReferenceIndex].colors32;
                for (var vertex = character.vertexIndex; vertex < character.vertexIndex + 4; vertex++)
                {
                    colors[vertex].a = 0;
                }
            }
        }

        private void LateUpdate()
        {
            if (_preparedText != _mesh.text && _placements.Count > 0)
            {
                _placements.Clear();
                _dirty = true;
            }

            if (!_dirty)
            {
                return;
            }

            _dirty = false;

            for (var index = 0; index < _placements.Count; index++)
            {
                if (index == _icons.Count)
                {
                    _icons.Add(CreateIcon());
                }

                var icon = _icons[index];
                var placement = _placements[index];

                if (icon.Item.Type != placement.Type)
                {
                    icon.Hover.HideDirectPreview();
                    icon.Item.Type = placement.Type;
                    icon.Image.texture = _textures[placement.Type];
                    ItemRarityUI.ApplyColors(icon.Rect, placement.Type);

                    if (_linkSource)
                    {
                        ChatItemSource.Bind(icon.Rect, placement.Type);
                    }
                }

                icon.Rect.localPosition = placement.Center;
                icon.Rect.sizeDelta = Vector2.one * placement.Size;
                foreach (var graphic in icon.Artwork)
                {
                    graphic.rectTransform.localScale = Vector3.one * (placement.Size / 46f);
                }

                icon.Rect.gameObject.SetActive(true);
            }

            for (var index = _placements.Count; index < _icons.Count; index++)
            {
                _icons[index].Rect.gameObject.SetActive(false);
            }
        }

        private (RectTransform, RawImage, Graphic[], HoverUI, InventoryItemDto) CreateIcon()
        {
            var slot = Instantiate(_slotPrefab, _mesh.rectTransform, false);
            slot.name = "ChatItemIcon";
            slot.GetComponent<ButtonUI>().enabled = _linkSource;
            slot.transform.Find("Text").gameObject.SetActive(false);

            var rect = (RectTransform)slot.transform;
            rect.anchorMin = rect.anchorMax = _mesh.rectTransform.pivot;
            rect.pivot = Vector2.one * 0.5f;

            var image = slot.transform.Find("Background").GetComponent<RawImage>();
            var outline = slot.transform.Find("Outline").GetComponent<Graphic>();
            var backdrop = slot.transform.Find("IconBackdrop").GetComponent<Graphic>();
            image.color = Color.white;
            image.raycastTarget = true;
            outline.raycastTarget = false;
            backdrop.raycastTarget = false;

            var preview = (RectTransform)slot.transform.Find("Preview");
            preview.anchorMin = preview.anchorMax = Vector2.one;
            preview.pivot = new Vector2(0, 0);
            preview.anchoredPosition = new Vector2(4, 4);

            var item = new InventoryItemDto { Count = 1 };
            var hover = slot.GetComponent<HoverUI>();
            hover.UseDirectTooltip();
            ItemComparisonUI.Bind(preview.gameObject, () => item);

            return (rect, image, new[] { image, outline, backdrop }, hover, item);
        }

        private void OnEnable()
        {
            _mesh?.SetVerticesDirty();
        }

        private void OnDisable()
        {
            _dirty = true;
            _placements.Clear();

            foreach (var icon in _icons)
            {
                icon.Rect.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                _mesh.OnPreRenderText -= PrepareIcons;
            }

            if (Fonts.TryGetValue(_fontKey, out var entry) && --entry.Users == 0)
            {
                Fonts.Remove(_fontKey);

                if (Application.isPlaying)
                {
                    Destroy(entry.Font);
                }
                else
                {
                    DestroyImmediate(entry.Font);
                }
            }
        }
    }
}
