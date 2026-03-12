using System.Collections;
using System.Collections.Generic;
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

        #endregion

        #region Button

        public Button CraftButton { get; private set; }

        public Button ExitButton { get; private set; }

        #endregion

        private ObjectPool<RecipePoolObject> _recipeObjectPool;
        private IDictionary<InventoryItemEnum, RecipePoolObject> _recipeObjects = new Dictionary<InventoryItemEnum, RecipePoolObject>();
        private CraftingRecipeTypeEnum _type;

        private void Start()
        {
            CraftingCanvas = GameObject.Find("CraftingCanvas");
            Crafting = CraftingCanvas.transform.Find("Crafting").gameObject;
            Recipes = Crafting.transform.Find("Recipes").gameObject;
            RecipesContent = Recipes.transform.Find("Viewport/Content").gameObject;
            CraftButton = Crafting.transform.Find("CraftButton").GetComponent<Button>();
            ExitButton = Crafting.transform.Find("ExitButton").GetComponent<Button>();

            _recipeObjectPool = new ObjectPool<RecipePoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_textButtonPrefab, RecipesContent.transform);

                    return new RecipePoolObject
                    {
                        GameObject = obj,
                        Mesh = obj.GetComponent<TextMeshProUGUI>(),
                        Button = obj.GetComponent<Button>()
                    };
                },
                actionOnGet: (RecipePoolObject obj) => obj.GameObject.SetActive(true),
                actionOnRelease: (RecipePoolObject obj) =>
                {
                    obj.GameObject.SetActive(false);
                    obj.Mesh.text = string.Empty;
                    obj.Mesh.color = ColorUI.White;
                    obj.Button.onClick.RemoveAllListeners();
                }
            );
        }

        public void Show(GetCraftingRecipesDto craftingRecipes, CraftingRecipeTypeEnum type)
        {
            if (Crafting.activeSelf)
            {
                return;
            }

            Crafting.SetActive(true);

            if (_type == type)
            {
                return;
            }

            if (_type != CraftingRecipeTypeEnum.None)
            {
                foreach (var recipeObject in _recipeObjects)
                {
                    _recipeObjectPool.Release(recipeObject.Value);
                }

                _recipeObjects.Clear();
            }

            _type = type;

            foreach (var recipe in craftingRecipes.craftingRecipes)
            {
                var obj = _recipeObjectPool.Get();
                obj.Mesh.text = TranslateManager.Instance.GetByKey($"{recipe.reward.item.type}Title");

                obj.Button.onClick.AddListener(() =>
                {
                    foreach (var recipeObject in _recipeObjects)
                    {
                        obj.Mesh.color = recipeObject.Key == recipe.reward.item.type ? ColorUI.Green : ColorUI.White;
                    }
                });

                _recipeObjects.Add(recipe.reward.item.type, obj);
            }
        }

        public void Hide()
        {
            if (!Crafting.activeSelf)
            {
                return;
            }

            Crafting.SetActive(false);
        }

        private class RecipePoolObject
        {
            public GameObject GameObject { get; set; }

            public TextMeshProUGUI Mesh { get; set; }

            public Button Button { get; set; }
        }
    }
}
