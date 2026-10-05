using Assets.Scripts.Areas.Inventory.Mono;
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

        private ObjectPool<RecipesPoolObject> _recipesObjectPool;

        private IDictionary<InventoryItemEnum, RecipesPoolObject> _recipesPoolObjects = new Dictionary<InventoryItemEnum, RecipesPoolObject>();

        private ObjectPool<RecipePoolObject> _recipeObjectPool;

        private IDictionary<InventoryItemEnum, RecipePoolObject> _recipeObjects = new Dictionary<InventoryItemEnum, RecipePoolObject>();

        public bool HasAllRequirements => HasRequiredItems && HasRequiredLevel;

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
            Hide();
            ClearRecipes();
            ClearRecipe();
            CurrentType = CraftingRecipeTypeEnum.None;
            BuildHideout = build;
            _craftButtonText = CraftButton.GetComponentInChildren<TMP_Text>().text;
            CraftButton.GetComponentInChildren<TMP_Text>().text = TranslateManager.Instance.GetByKey("HideoutBuild");
            CurrentRecipe = new CraftingRecipeDto
            {
                Requirement = new CraftingRecipeRequirementDto { Items = definition.Requirement.Items, Level = 0 }
            };

            CharacterStash.Local?.Close();
            QuestUI.Instance.Hide();
            CharacterUI.Instance.Hide();
            MerchantUI.Instance.Hide();
            GearUI.Instance.Hide();

            var title = _recipesObjectPool.Get();
            title.Mesh.text = TranslateManager.Instance.GetByKey(definition.Name + "Title");
            title.Mesh.color = ColorUI.Green;
            _recipesPoolObjects.Add(InventoryItemEnum.Chamomile, title);
            RewardText.SetActive(true);
            RewardText.GetComponent<TMP_Text>().text = TranslateManager.Instance.GetByKey("HideoutBuildTime") + $": {definition.BuildTime}s";
            var preview = _recipeObjectPool.Get();
            preview.GameObject.transform.SetParent(Reward.transform, false);
            preview.Image.texture = Resources.Load<Texture2D>("Icons/ChamomileFarm");
            preview.Mesh.text = string.Empty;
            preview.Mesh.gameObject.SetActive(false);

            ConfigurePreview(preview,
                TranslateManager.Instance.GetByKey(definition.Name + "Title"),
                TranslateManager.Instance.GetByKey(definition.Name + "Description"));

            _recipeObjects.Add(InventoryItemEnum.None, preview);

            RequirementsText.SetActive(true);
            RequirementsFlexibleGridLayout.columns = definition.Requirement.Items.Length;

            foreach (var item in definition.Requirement.Items)
            {
                AddInventoryItem(item, Requirements.transform);
            }

            Crafting.SetActive(true);
            CraftButton.interactable = HasAllRequirements;
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
