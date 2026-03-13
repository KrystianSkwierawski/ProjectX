using Assets.Scripts.Enums;
using Assets.Scripts.Extensions;
using Assets.Scripts.Shared;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Crafting : NetworkBehaviour
{
    private StarterAssetsInputs _input;
    private GameObject _crafting;

    private void Start()
    {
        if (IsOwner)
        {
            _input = GetComponent<StarterAssetsInputs>();

            CraftingUI.Instance.ExitButton.onClick.AddListener(() =>
            {
                CraftingUI.Instance.Hide();
                _crafting = null;
            });

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
            CheckHide();
            CheckStationClicked();
        }
    }

    private void CheckHide()
    {
        if (_crafting != null && _input.Move != Vector2.zero && _crafting.transform.IsFarToTarget(transform.gameObject, 3f))
        {
            CraftingUI.Instance.Hide();
            _crafting = null;
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

            return;
        }

        CursorUI.Instance.ShowPointer();

        if (mouse.rightButton.wasPressedThisFrame)
        {
            _crafting = hit.transform.gameObject;

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

        var dto = await CraftingRecipeManager.Instance.GetAsync(type);

        CraftingUI.Instance.Show(dto, type);
    }
}
