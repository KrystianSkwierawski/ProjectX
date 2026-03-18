using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Assets.Scripts.UI
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

        private ObjectPool<RecipesPoolObject> _recipesObjectPool;

        private IDictionary<InventoryItemEnum, RecipesPoolObject> _recipesPoolObjects = new Dictionary<InventoryItemEnum, RecipesPoolObject>();

        private ObjectPool<RecipePoolObject> _recipeObjectPool;

        private IDictionary<InventoryItemEnum, RecipePoolObject> _recipeObjects = new Dictionary<InventoryItemEnum, RecipePoolObject>();

        public bool HasAllRequirements => HasRequiredItems && HasRequiredLevel;

        public bool HasRequiredItems => CraftingUI.Instance.CurrentRecipe.requirement.items.All(x =>
        {
            var count = InventoryManager.Instance.Dto.inventory.items
                .Where(i => i.type == x.type)
                .Sum(i => i.count);

            return count >= x.count;
        });

        public bool HasRequiredLevel => true; // TODO

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
                actionOnGet: (RecipesPoolObject obj) => obj.GameObject.SetActive(true),
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

                    return new RecipePoolObject
                    {
                        GameObject = obj,
                        Image = obj.transform.Find("Background").GetComponent<RawImage>(),
                        Mesh = obj.transform.Find("Text").GetComponent<TextMeshProUGUI>(),
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
                    obj.Mesh.gameObject.SetActive(false);
                    obj.Mesh.text = string.Empty;
                    obj.Image.color = ColorUI.Black;
                    obj.Image.texture = null;
                    obj.HoverUI.enabled = false;
                    //obj.Button.onClick.RemoveAllListeners();
                }
            );
        }

        public void Show(GetCraftingRecipesDto dto, CraftingRecipeTypeEnum type)
        {
            if (Crafting.activeSelf)
            {
                return;
            }

            QuestUI.Instance.Hide();
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
            if (!Crafting.activeSelf)
            {
                return;
            }

            Crafting.SetActive(false);
        }

        public void UpdateRequirements()
        {
            foreach (var obj in _recipeObjects)
            {
                if (obj.Value.GameObject.transform.parent == Requirements.transform)
                {
                    var count = InventoryManager.Instance.Dto.inventory.items
                        .Where(x => x.type == obj.Key)
                        .Sum(x => x.count);

                    var required = CurrentRecipe.requirement.items
                        .Where(x => x.type == obj.Key)
                        .Sum(x => x.count);

                    obj.Value.Mesh.text = $"{count}/{required}";
                }
            }

            CraftButton.interactable = HasAllRequirements;
        }

        private void AddRecipes(GetCraftingRecipesDto dto)
        {
            foreach (var recipe in dto.craftingRecipes)
            {
                var obj = _recipesObjectPool.Get();
                obj.Mesh.text = TranslateManager.Instance.GetByKey($"{recipe.reward.item.type}Title");

                obj.Button.onClick.AddListener(() =>
                {
                    SetRecipe(recipe);
                });

                _recipesPoolObjects.Add(recipe.reward.item.type, obj);
            }
        }

        private void SetRecipe(CraftingRecipeDto recipe)
        {
            if (CurrentRecipe == recipe)
            {
                return;
            }

            foreach (var recipeObject in _recipesPoolObjects)
            {
                recipeObject.Value.Mesh.color = recipeObject.Key == recipe.reward.item.type ? ColorUI.Green : ColorUI.White;
            }

            ClearRecipe();

            CurrentRecipe = recipe;

            CraftButton.interactable = HasAllRequirements;

            SetReward(recipe);

            SetRequirements(recipe);
        }

        private void SetReward(CraftingRecipeDto recipe)
        {
            RewardText.SetActive(true);

            AddInventoryItem(recipe.reward.item, Reward.transform);
        }

        private void SetRequirements(CraftingRecipeDto recipe)
        {
            RequirementsText.SetActive(true);
            RequirementsFlexibleGridLayout.columns = recipe.requirement.items.Count;

            foreach (var item in recipe.requirement.items)
            {
                AddInventoryItem(item, Requirements.transform);
            }
        }

        private void AddInventoryItem(InventoryItemDto item, Transform parent)
        {
            var obj = _recipeObjectPool.Get();

            obj.GameObject.transform.SetParent(parent);

            obj.Image.texture = InventoryUI.Instance.Textures[item.type];
            obj.PreviewTitleMesh.text = TranslateManager.Instance.GetByKey($"{item.type}Title");
            obj.PreviewDescriptionMesh.text = TranslateManager.Instance.GetByKey($"{item.type}Description");

            var count = InventoryManager.Instance.Dto.inventory.items
                .Where(x => x.type == item.type)
                .Sum(x => x.count);

            obj.Mesh.text = parent == Requirements.transform
                ? $"{count}/{item.count}"
                : item.count.ToString();

            var key = obj.GameObject.GetInstanceID().ToString();

            OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
            {
                obj.Preview.SetActive(true);
            });

            OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
            {
                obj.Preview.SetActive(false);
            });

            _recipeObjects.Add(item.type, obj);
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