using Assets.Scripts.Enums;
using Assets.Scripts.Extensions;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class Crafting : NetworkBehaviour
{
    private void Start()
    {
        if (IsOwner)
        {
            CraftingUI.Instance.ExitButton.onClick.AddListener(() => CraftingUI.Instance.Hide());

            CraftingUI.Instance.CraftButton.onClick.AddListener(() =>
            {
                Debug.Log("craft");
            });
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            CheckStationClicked();
        }
    }

    private void CheckStationClicked()
    {
        var mouse = Mouse.current;

        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

        var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "Crafting";

        if (!hover)
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        if (hit.transform.IsFarToTarget(transform.gameObject, 3f))
        {
            CursorUI.Instance.ShowDefault();
            CraftingUI.Instance.Hide();

            return;
        }

        CursorUI.Instance.ShowPointer();

        if (mouse.rightButton.wasPressedThisFrame)
        {
            var type = hit.transform.gameObject.name switch
            {
                "CookingStation" => CraftingRecipeTypeEnum.Cooking,
                _ => CraftingRecipeTypeEnum.None
            };

            ShowCrafting(type).Forget();
        }
    }

    private async UniTask ShowCrafting(CraftingRecipeTypeEnum type)
    {
        if (type == CraftingRecipeTypeEnum.None)
        {
            return;
        }

        var recipes = await CraftingRecipeManager.Instance.GetAsync(type);

        CraftingUI.Instance.Show(recipes, type);
    }
}
