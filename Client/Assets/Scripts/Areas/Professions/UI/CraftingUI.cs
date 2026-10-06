using Assets.Scripts.Areas.Inventory.Mono;
using Assets.Scripts.Areas.Hideout;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Professions.Enums;
using Assets.Scripts.Areas.Professions.Models;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Trade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Professions.UI
{
    public class CraftingUI : MonoSingleton<CraftingUI>
    {
        #region Prefab

        [SerializeField] private GameObject _inventorySlotPrefab;
        [SerializeField] private GameObject _textButtonPrefab;

        #endregion

        #region GameObject

        public GameObject CraftingCanvas { get; private set; }

        public GameObject Crafting { get; private set; }

        public GameObject Recipes { get; private set; }

        public GameObject RecipesContent { get; private set; }

        public GameObject Recipe { get; private set; }

        public GameObject Reward { get; private set; }

        public GameObject RewardText { get; private set; }

        public GameObject Requirements { get; private set; }

        public FlexibleGridLayout RequirementsFlexibleGridLayout { get; private set; }

        public GameObject RequirementsText { get; private set; }

        #endregion

        #region Button

        public Button CraftButton { get; private set; }

        public Button ExitButton { get; private set; }

        #endregion

        public CraftingRecipeDto CurrentRecipe { get; private set; }

        public CraftingRecipeTypeEnum CurrentType { get; private set; }

        public System.Action BuildHideout { get; private set; }

        private string _craftButtonText;
        private HideoutBuildingDto _hideout;
        private readonly Dictionary<RectTransform, (Vector2 Min, Vector2 Max, Vector2 Position, Vector2 Size)> _hideoutLayout
            = new Dictionary<RectTransform, (Vector2, Vector2, Vector2, Vector2)>();
        private readonly List<GameObject> _farmSlots = new List<GameObject>();

        public bool IsFarmOpen => BuildHideout != null && _hideout?.Farm != null && Crafting.activeSelf
            && _hideout.BuildEndsAt <= CharacterHideout.Local.SelectedBuilding.CurrentTime;

        private bool CanUpgradeHideout => _hideout == null || !_hideout.BuildEndsAt.HasValue
            || _hideout.Farm != null && _hideout.Farm.Level < 3 && !_hideout.Farm.UpgradeEndsAt.HasValue
            && _hideout.BuildEndsAt <= CharacterHideout.Local.SelectedBuilding.CurrentTime;

        private ObjectPool<RecipesPoolObject> _recipesObjectPool;

        private IDictionary<InventoryItemEnum, RecipesPoolObject> _recipesPoolObjects = new Dictionary<InventoryItemEnum, RecipesPoolObject>();

        private ObjectPool<RecipePoolObject> _recipeObjectPool;

        private IDictionary<InventoryItemEnum, RecipePoolObject> _recipeObjects = new Dictionary<InventoryItemEnum, RecipePoolObject>();

        public bool HasAllRequirements => HasRequiredItems && HasRequiredLevel && CanUpgradeHideout;

        public bool HasRequiredItems => CurrentRecipe.Requirement.Items.All(x =>
        {
            var count = InventoryManager.Instance.Dto.Inventory.Items
                .Where(i => i.Type == x.Type)
                .Sum(i => i.Count);

            return count >= x.Count;
        });

        public bool HasRequiredLevel => BuildHideout != null || UserManager.Instance.GetLevelByRecipeType(CurrentType) >= CurrentRecipe.Requirement.Level;

        public void ShowHideout(Assets.Scripts.Areas.Hideout.HideoutBuildingDto definition, System.Action build)
        {
            InventoryUI.Instance.CancelDrag();
            Hide();
            ClearRecipes();
            ClearRecipe();
            CurrentType = CraftingRecipeTypeEnum.None;
            BuildHideout = build;
            _hideout = definition;
            var built = definition.BuildEndsAt.HasValue;
            var requirement = built ? definition.UpgradeRequirement : definition.Requirement;
            _craftButtonText = CraftButton.GetComponentInChildren<TMP_Text>().text;
            CraftButton.GetComponentInChildren<TMP_Text>().text = TranslateManager.Instance.GetByKey(built ? "HideoutUpgrade" : "HideoutBuild");
            CurrentRecipe = new CraftingRecipeDto
            {
                Requirement = new CraftingRecipeRequirementDto { Items = requirement.Items, Level = 0 }
            };

            CharacterStash.Local?.Close();
            QuestUI.Instance.Hide();
            CharacterUI.Instance.Hide();
            MerchantUI.Instance.Hide();
            GearUI.Instance.Hide();

            var title = _recipesObjectPool.Get();
            title.Mesh.text = TranslateManager.Instance.GetByKey(definition.Name + "Title");
            if (definition.Farm != null)
            {
                title.Mesh.text += $" · {TranslateManager.Instance.GetByKey("HideoutLevel")} {definition.Farm.Level}";
            }

            title.Mesh.color = ColorUI.Green;
            _recipesPoolObjects.Add(InventoryItemEnum.Chamomile, title);
            RewardText.SetActive(true);
            RewardText.GetComponent<TMP_Text>().text = TranslateManager.Instance.GetByKey("HideoutBuildTime") + $": {(built ? definition.UpgradeTime : definition.BuildTime)}s";
            var preview = _recipeObjectPool.Get();
            preview.GameObject.transform.SetParent(Reward.transform, false);
            preview.Image.texture = Resources.Load<Texture2D>("Icons/" + definition.Name);
            preview.Mesh.text = string.Empty;
            preview.Mesh.gameObject.SetActive(false);

            ConfigurePreview(preview,
                TranslateManager.Instance.GetByKey(definition.Name + "Title"),
                TranslateManager.Instance.GetByKey(definition.Name + "Description"));

            _recipeObjects.Add(InventoryItemEnum.None, preview);

            RequirementsText.SetActive(requirement.Items.Length > 0);
            RequirementsFlexibleGridLayout.columns = Mathf.Max(1, requirement.Items.Length);

            foreach (var item in requirement.Items)
            {
                AddInventoryItem(item, Requirements.transform);
            }

            if (built && definition.Farm != null)
            {
                _recipeObjectPool.Release(preview);
                _recipeObjects.Remove(InventoryItemEnum.None);
                ShowFarmSlots(definition.Farm);
                InventoryUI.Instance.Inventory.SetActive(true);
            }

            Crafting.SetActive(true);
            CraftButton.interactable = HasAllRequirements;
        }

        public int GetFarmSlotIndex(GameObject target)
        {
            if (!IsFarmOpen || target == null)
            {
                return -1;
            }

            return _farmSlots.FindIndex(x => target.transform == x.transform || target.transform.IsChildOf(x.transform));
        }

        public void DepositSeed(int inventorySlot, int targetSlot = -1)
        {
            if (!IsFarmOpen)
            {
                return;
            }

            var items = InventoryManager.Instance.Dto.Inventory.Items;

            if (inventorySlot < 0 || inventorySlot >= items.Count)
            {
                return;
            }

            var item = items[inventorySlot];

            if (!CharacterHideout.Local.SelectedBuilding.IsSeed(item.Type))
            {
                return;
            }

            if (targetSlot < 0)
            {
                targetSlot = System.Array.FindIndex(_hideout.Farm.Slots.Take(_hideout.Farm.Capacity).ToArray(),
                    x => x.Seeds.Count == 0 || x.Seeds.Type == item.Type && x.Seeds.Count + item.Count <= 1024);
            }

            if (targetSlot >= 0)
            {
                CharacterHideout.Local.Request(HideoutOperationEnum.Deposit, targetSlot, inventorySlot, item);
            }
        }

        private void ShowFarmSlots(FarmStateDto farm)
        {
            var layout = Reward.GetComponent<FlexibleGridLayout>();
            layout.columns = 3;
            layout.rows = 2;
            layout.fitType = FlexibleGridLayout.FitType.FixedColumns;
            layout.spacing = new Vector2(8, 8);
            RewardText.GetComponent<TMP_Text>().text = TranslateManager.Instance.GetByKey("HideoutSeeds")
                + (_hideout.UpgradeTime > 0 ? $" · {TranslateManager.Instance.GetByKey("HideoutUpgrade")}: {_hideout.UpgradeTime}s" : string.Empty);

            for (var i = 0; i < farm.Capacity; i++)
            {
                var index = i;
                var state = farm.Slots[i];
                var slot = Instantiate(_inventorySlotPrefab, Reward.transform);
                _farmSlots.Add(slot);
                var image = slot.transform.Find("Background").GetComponent<RawImage>();
                var count = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                var preview = slot.transform.Find("Preview").gameObject;
                var shownType = state.Seeds.Count > 0 ? state.Seeds.Type : state.Ready;

                image.texture = shownType == InventoryItemEnum.None ? null : InventoryUI.Instance.Textures[shownType];
                image.color = shownType == InventoryItemEnum.None ? ColorUI.Black : ColorUI.White;
                count.text = state.Seeds.Count > 0 ? state.Seeds.Count.ToString() : string.Empty;
                count.gameObject.SetActive(state.Seeds.Count > 0);
                count.rectTransform.SetParent(image.rectTransform, false);
                count.rectTransform.anchorMin = Vector2.zero;
                count.rectTransform.anchorMax = Vector2.one;
                count.rectTransform.offsetMin = new Vector2(2, 2);
                count.rectTransform.offsetMax = new Vector2(-2, -2);
                count.enableAutoSizing = true;
                count.fontSizeMin = 8;
                count.fontSizeMax = 18;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.alignment = TextAlignmentOptions.BottomRight;
                count.raycastTarget = false;
                preview.SetActive(false);
                slot.GetComponent<HoverUI>().enabled = true;
                preview.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = shownType == InventoryItemEnum.None
                    ? TranslateManager.Instance.GetByKey("HideoutSeeds") : TranslateManager.Instance.GetByKey(shownType + "Title");
                preview.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = TranslateManager.Instance.GetByKey("HideoutSeedHint");

                var key = slot.GetInstanceID().ToString();
                OnPointerEnterSubscription.Instance.Subscribe(key, _ => preview.SetActive(true));
                OnPointerExitSubscription.Instance.Subscribe(key, _ => preview.SetActive(false));
                slot.GetComponent<ButtonUI>().OnRightClick.AddListener(() =>
                {
                    if (IsFarmOpen && state.Seeds.Count > 0)
                    {
                        CharacterHideout.Local.Request(HideoutOperationEnum.Withdraw, index);
                    }
                });

                InventoryUI.Instance.ConfigureItemDrag(slot, image, count,
                    () => state.Seeds.Count > 0 ? state.Seeds.Type : InventoryItemEnum.None,
                    data =>
                    {
                        if (InventoryUI.Instance.GetSlotIndex(data.pointerCurrentRaycast.gameObject) >= 0)
                        {
                            CharacterHideout.Local.Request(HideoutOperationEnum.Withdraw, index);
                        }
                    });
            }
        }

        private void ClearFarmSlots()
        {
            foreach (var slot in _farmSlots)
            {
                var key = slot.GetInstanceID().ToString();
                OnPointerEnterSubscription.Instance.Unsubscribe(key);
                OnPointerExitSubscription.Instance.Unsubscribe(key);
                slot.SetActive(false);
                slot.transform.SetParent(CraftingCanvas.transform, false);
                Destroy(slot);
            }

            _farmSlots.Clear();
            _hideout = null;

            foreach (var pair in _hideoutLayout)
            {
                pair.Key.anchorMin = pair.Value.Min;
                pair.Key.anchorMax = pair.Value.Max;
                pair.Key.anchoredPosition = pair.Value.Position;
                pair.Key.sizeDelta = pair.Value.Size;
            }

            _hideoutLayout.Clear();

            if (Reward != null)
            {
                ConfigureSlotLayout(Reward.GetComponent<FlexibleGridLayout>());
            }
        }

        private void LateUpdate()
        {
            if (BuildHideout == null || !Crafting.activeSelf)
            {
                return;
            }

            var canvas = (RectTransform)CraftingCanvas.transform;
            var width = Mathf.Min(380, canvas.rect.width - 24);
            var height = 410f;
            var reference = (RectTransform)QuestUI.Instance.Quest.transform;
            var left = reference.anchoredPosition.x - reference.rect.width / 2;
            var limit = Mathf.Max(0, (canvas.rect.width - width) / 2 - 12);
            var x = Mathf.Clamp(left + width / 2, -limit, limit);
            var y = reference.anchoredPosition.y;

            SetHideoutRect(Crafting, x, y, width, height);
            SetHideoutRect(Recipes, 0, 165, width - 24, 54);
            SetHideoutRect(Recipe, 0, -5, width - 24, 270);
            var labelWidth = width - 38;
            var requirementsOffset = RewardText.GetComponent<TMP_Text>().margin.x
                - RequirementsText.GetComponent<TMP_Text>().margin.x;

            SetHideoutRect(RewardText, 0, 112, labelWidth, 26);
            SetHideoutRect(Reward, 0, 32, width - 32, 128);
            SetHideoutRect(RequirementsText, requirementsOffset, -53, labelWidth, 26);
            SetHideoutRect(Requirements, 0, -101, width - 32, 60);
            SetHideoutRect(CraftButton.gameObject, width / 4 - 6, -173, width / 2 - 24, 36);
            SetHideoutRect(ExitButton.gameObject, -width / 4 + 6, -173, width / 2 - 24, 36);

            CraftButton.interactable = HasAllRequirements;

            var now = CharacterHideout.Local?.SelectedBuilding?.CurrentTime;
            var constructing = _hideout.BuildEndsAt.HasValue && _hideout.BuildEndsAt > now;
            var upgrading = _hideout.Farm?.UpgradeEndsAt.HasValue == true;
            var label = RewardText.GetComponent<TMP_Text>();

            if (!_hideout.BuildEndsAt.HasValue)
            {
                label.text = TranslateManager.Instance.GetByKey("HideoutBuildTime") + $": {_hideout.BuildTime}s";
            }
            else if (constructing)
            {
                label.text = string.Empty;
            }
            else
            {
                label.text = TranslateManager.Instance.GetByKey("HideoutSeeds");

                if (!upgrading && _hideout.Farm?.Level < 3 && _hideout.UpgradeTime > 0)
                {
                    label.text += $" · {TranslateManager.Instance.GetByKey("HideoutUpgrade")}: {_hideout.UpgradeTime}s";
                }
            }
        }

        private void SetHideoutRect(GameObject obj, float x, float y, float width, float height)
        {
            var rect = (RectTransform)obj.transform;

            if (!_hideoutLayout.ContainsKey(rect))
            {
                _hideoutLayout.Add(rect, (rect.anchorMin, rect.anchorMax, rect.anchoredPosition, rect.sizeDelta));
            }

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void Start()
        {
            CraftingCanvas = GameObject.Find("CraftingCanvas");
            Crafting = CraftingCanvas.transform.Find("Crafting").gameObject;
            Recipes = Crafting.transform.Find("Recipes").gameObject;
            RecipesContent = Recipes.transform.Find("Viewport/Content").gameObject;
            Recipe = Crafting.transform.Find("Recipe").gameObject;
            Reward = Recipe.transform.Find("Reward").gameObject;
            RewardText = Recipe.transform.Find("RewardText").gameObject;
            Requirements = Recipe.transform.Find("Requirements").gameObject;
            RequirementsFlexibleGridLayout = Requirements.GetComponent<FlexibleGridLayout>();
            RequirementsText = Recipe.transform.Find("RequirementsText").gameObject;
            CraftButton = Crafting.transform.Find("CraftButton").GetComponent<Button>();
            ExitButton = Crafting.transform.Find("ExitButton").GetComponent<Button>();

            ConfigureSlotLayout(Reward.GetComponent<FlexibleGridLayout>());
            ConfigureSlotLayout(RequirementsFlexibleGridLayout);

            _recipesObjectPool = new ObjectPool<RecipesPoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_textButtonPrefab, RecipesContent.transform);

                    return new RecipesPoolObject
                    {
                        GameObject = obj,
                        Mesh = obj.GetComponent<TextMeshProUGUI>(),
                        Button = obj.GetComponent<Button>()
                    };
                },
                actionOnGet: (RecipesPoolObject obj) =>
                {
                    obj.GameObject.SetActive(true);
                    obj.GameObject.transform.SetAsLastSibling();
                },
                actionOnRelease: (RecipesPoolObject obj) =>
                {
                    obj.GameObject.SetActive(false);
                    obj.Mesh.text = string.Empty;
                    obj.Mesh.color = ColorUI.White;
                    obj.Button.onClick.RemoveAllListeners();
                }
            );

            _recipeObjectPool = new ObjectPool<RecipePoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_inventorySlotPrefab);

                    var preview = obj.transform.Find("Preview").gameObject;
                    var count = obj.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                    count.enableAutoSizing = true;
                    count.fontSizeMin = 8;
                    count.fontSizeMax = 18;
                    count.textWrappingMode = TextWrappingModes.NoWrap;
                    count.alignment = TextAlignmentOptions.BottomRight;

                    return new RecipePoolObject
                    {
                        GameObject = obj,
                        Image = obj.transform.Find("Background").GetComponent<RawImage>(),
                        Mesh = count,
                        HoverUI = obj.GetComponent<HoverUI>(),
                        Preview = preview,
                        PreviewTitleMesh = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>(),
                        PreviewDescriptionMesh = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>(),
                    };
                },
                actionOnGet: (RecipePoolObject obj) =>
                {
                    obj.GameObject.SetActive(true);
                    obj.Image.color = ColorUI.White;
                    obj.Mesh.gameObject.SetActive(true);
                    obj.HoverUI.enabled = true;
                },
                actionOnRelease: (RecipePoolObject obj) =>
                {
                    obj.GameObject.SetActive(false);
                    obj.Preview.SetActive(false);
                    obj.GameObject.transform.SetParent(CraftingCanvas.transform, false);

                    var key = obj.GameObject.GetInstanceID().ToString();
                    OnPointerEnterSubscription.Instance.Unsubscribe(key);
                    OnPointerExitSubscription.Instance.Unsubscribe(key);

                    obj.Mesh.gameObject.SetActive(false);
                    obj.Mesh.text = string.Empty;
                    obj.Image.color = ColorUI.Black;
                    obj.Image.texture = null;
                    obj.HoverUI.enabled = false;
                }
            );
        }

        public void Show(GetCraftingRecipesDto dto, CraftingRecipeTypeEnum type)
        {
            if (Crafting.activeSelf)
            {
                return;
            }

            if (TradeUI.Instance?.OpenAfterTrade(() => Show(dto, type)) == true)
            {
                return;
            }

            CharacterStash.Local?.Close();

            // FIXME: array
            QuestUI.Instance.Hide();
            CharacterUI.Instance.Hide();
            MerchantUI.Instance.Hide();
            GearUI.Instance.Hide();
            Crafting.SetActive(true);

            if (CurrentType == type)
            {
                return;
            }

            if (CurrentType != CraftingRecipeTypeEnum.None)
            {
                ClearRecipes();

                ClearRecipe();
            }

            CurrentType = type;

            AddRecipes(dto);
        }

        public void Hide()
        {
            if (BuildHideout != null)
            {
                ClearFarmSlots();
                BuildHideout = null;
                CraftButton.GetComponentInChildren<TMP_Text>().text = _craftButtonText;
                ClearRecipes();
                ClearRecipe();
                CurrentRecipe = null;
                RewardText.GetComponent<TMP_Text>().text = TranslateManager.Instance.GetByKey("Reward");
            }

            if (Crafting.activeSelf)
            {
                Crafting.SetActive(false);
            }
        }

        public void UpdateRequirements(InventoryItemEnum? type = null)
        {
            // TODO: only if activeSelf?
            if (_recipeObjects.Count == 0)
            {
                return;
            }

            foreach (var recipeObject in _recipeObjects
                .Where(x => x.Value.GameObject.transform.parent == Requirements.transform)
                .Where(x => type.HasValue ? x.Key == type.Value : true))
            {
                var count = recipeObject.Key == InventoryItemEnum.Xp
                    ? UserManager.Instance.GetLevelByRecipeType(CurrentType)
                    : InventoryManager.Instance.Dto.Inventory.Items
                        .Where(x => x.Type == recipeObject.Key)
                        .Sum(x => x.Count);

                var required = recipeObject.Key == InventoryItemEnum.Xp 
                    ? CurrentRecipe.Requirement.Level
                    : CurrentRecipe.Requirement.Items
                        .Where(x => x.Type == recipeObject.Key)
                        .Sum(x => x.Count);

                recipeObject.Value.Mesh.text = $"{count}/{required}";
                recipeObject.Value.Mesh.color = count >= required ? ColorUI.Green: ColorUI.Red;
            }

            CraftButton.interactable = HasAllRequirements;
        }

        private void AddRecipes(GetCraftingRecipesDto dto)
        {
            foreach (var recipe in dto.CraftingRecipes)
            {
                var obj = _recipesObjectPool.Get();
                obj.Mesh.text = TranslateManager.Instance.GetByKey($"{recipe.Reward.Item.Type}Title");

                obj.Button.onClick.AddListener(() =>
                {
                    SetRecipe(recipe);
                });

                _recipesPoolObjects.Add(recipe.Reward.Item.Type, obj);
            }
        }

        private void SetRecipe(CraftingRecipeDto recipe)
        {
            if (CurrentRecipe == recipe)
            {
                return;
            }

            SetRecipesColor(recipe);

            ClearRecipe();

            CurrentRecipe = recipe;

            CraftButton.interactable = HasAllRequirements;

            SetReward(recipe);

            SetRequirements(recipe);
        }

        private void SetRecipesColor(CraftingRecipeDto recipe)
        {
            foreach (var recipeObject in _recipesPoolObjects)
            {
                recipeObject.Value.Mesh.color = recipeObject.Key == recipe.Reward.Item.Type ? ColorUI.Green : ColorUI.White;
            }
        }

        private void SetReward(CraftingRecipeDto recipe)
        {
            RewardText.SetActive(true);

            AddInventoryItem(recipe.Reward.Item, Reward.transform);
        }

        private void SetRequirements(CraftingRecipeDto recipe)
        {
            RequirementsText.SetActive(true);
            RequirementsFlexibleGridLayout.columns = recipe.Requirement.Items.Length + 1;

            foreach (var item in recipe.Requirement.Items)
            {
                AddInventoryItem(item, Requirements.transform);
            }

            AddInventoryItem(new InventoryItemDto
            {
                Type = InventoryItemEnum.Xp,
                Count = recipe.Requirement.Level
            }, Requirements.transform);
        }

        private void AddInventoryItem(InventoryItemDto item, Transform parent)
        {
            var obj = _recipeObjectPool.Get();

            obj.GameObject.transform.SetParent(parent, false);
            obj.GameObject.transform.SetAsLastSibling();

            obj.Image.texture = InventoryUI.Instance.Textures[item.Type];

            ConfigurePreview(obj,
                TranslateManager.Instance.GetByKey($"{item.Type}Title"),
                InventoryUI.Instance.PrepareDescription(item));

            var count = item.Type == InventoryItemEnum.Xp
                ? UserManager.Instance.GetLevelByRecipeType(CurrentType)
                : InventoryManager.Instance.Dto.Inventory.Items
                    .Where(x => x.Type == item.Type)
                    .Sum(x => x.Count);

            obj.Mesh.text = parent == Requirements.transform
                ? $"{count}/{item.Count}"
                : item.Count.ToString();

            obj.Mesh.rectTransform.SetParent(obj.Image.rectTransform, false);
            obj.Mesh.rectTransform.anchorMin = Vector2.zero;
            obj.Mesh.rectTransform.anchorMax = Vector2.one;
            obj.Mesh.rectTransform.offsetMin = new Vector2(3, 3);
            obj.Mesh.rectTransform.offsetMax = new Vector2(-3, -3);
            obj.Mesh.enableAutoSizing = true;
            obj.Mesh.fontSizeMin = 6;
            obj.Mesh.fontSizeMax = 18;
            obj.Mesh.textWrappingMode = TextWrappingModes.NoWrap;
            obj.Mesh.alignment = TextAlignmentOptions.BottomRight;

            obj.Mesh.color = parent == Reward.transform
                ? ColorUI.White
                : count >= item.Count
                    ? ColorUI.Green
                    : ColorUI.Red;

            _recipeObjects.Add(item.Type, obj);
        }

        private static void ConfigurePreview(RecipePoolObject obj, string title, string description)
        {
            obj.Preview.SetActive(false);
            obj.PreviewTitleMesh.text = title;
            obj.PreviewDescriptionMesh.text = description;
            obj.HoverUI.enabled = true;

            var key = obj.GameObject.GetInstanceID().ToString();
            OnPointerEnterSubscription.Instance.Unsubscribe(key);
            OnPointerExitSubscription.Instance.Unsubscribe(key);

            OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
            {
                obj.Preview.SetActive(true);
            });

            OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
            {
                obj.Preview.SetActive(false);
            });

        }

        private static void ConfigureSlotLayout(FlexibleGridLayout layout)
        {
            layout.fitX = false;
            layout.fitY = false;
            layout.cellSize = new Vector2(60, 60);
            layout.spacing = new Vector2(8, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.rows = 1;
            layout.fitType = FlexibleGridLayout.FitType.FixedRows;
        }

        private void ClearRecipes()
        {
            foreach (var recipesObject in _recipesPoolObjects)
            {
                _recipesObjectPool.Release(recipesObject.Value);
            }

            _recipesPoolObjects.Clear();
        }

        private void ClearRecipe()
        {
            RewardText.SetActive(false);
            RequirementsText.SetActive(false);

            foreach (var recipeObject in _recipeObjects)
            {
                _recipeObjectPool.Release(recipeObject.Value);
            }

            _recipeObjects.Clear();
        }

        private class RecipesPoolObject
        {
            public GameObject GameObject { get; set; }

            public TextMeshProUGUI Mesh { get; set; }

            public Button Button { get; set; }
        }

        private class RecipePoolObject
        {
            public GameObject GameObject { get; set; }

            public RawImage Image { get; set; }

            public TextMeshProUGUI Mesh { get; set; }

            public HoverUI HoverUI { get; set; }

            public GameObject Preview { get; set; }

            public TextMeshProUGUI PreviewTitleMesh { get; set; }

            public TextMeshProUGUI PreviewDescriptionMesh { get; set; }
        }
    }
}
