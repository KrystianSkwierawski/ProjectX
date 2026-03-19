using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Extensions;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
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

    private Color _originalBarColor;
    private bool _isCrafting = false;
    private float _craftingTime = 4f;
    private float _craftingTimer = 0f;

    private void Start()
    {
        if (IsOwner)
        {
            _input = GetComponent<StarterAssetsInputs>();

            CraftingUI.Instance.ExitButton.onClick.AddListener(Exit);
            CraftingUI.Instance.CraftButton.onClick.AddListener(StartCrafting);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            CheckHide();
            CheckStationClicked();
            CheckCrafting();
        }
    }

    private void StartCrafting()
    {
        AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.CookingPrepare, 0.5f);
        _originalBarColor = PlayerUI.Instance.CastProgressBar.color;
        _isCrafting = true;
        _craftingTimer = 0f;
        PlayerUI.Instance.UpdateCastBar(_craftingTimer / _craftingTime);
    }

    private void CheckCrafting()
    {
        if (!_isCrafting)
        {
            return;
        }

        // TODO: interrupt

        _craftingTimer += Time.deltaTime;
        PlayerUI.Instance.UpdateCastBar(_craftingTimer / _craftingTime);

        if (_craftingTimer >= _craftingTime)
        {
            StopCrafting();
            CraftServerRpc(CraftingUI.Instance.CurrentRecipe.id, CraftingUI.Instance.CurrentType, UserManager.Instance.Token);
        }
    }

    private void StopCrafting()
    {
        _isCrafting = false;
        _craftingTimer = 0f;
        PlayerUI.Instance.HideCastBar();
    }

    [ServerRpc]
    private void CraftServerRpc(CraftingRecipeEnum id, CraftingRecipeTypeEnum type, string clientToken)
    {
        // TODO: validate
        CraftAsync(id, type, clientToken).Forget();
    }

    private async UniTaskVoid CraftAsync(CraftingRecipeEnum id, CraftingRecipeTypeEnum type, string clientToken)
    {
        var dto = await CraftingRecipeManager.Instance.GetAsync(type);

        var recipe = dto.craftingRecipes
            .Where(x => x.id == id)
            .Single();

        var key = OwnerClientId.ToString();

        AddInventoryItemSubscription.Instance.Invoke(key, new AddInventoryItemSubscriptionEvent
        {
            Item = recipe.reward.item,
            ClientToken = clientToken,
        });

        // TODO: invoke once?
        foreach (var requirement in recipe.requirement.items)
        {
            RemoveInventoryItemSubscription.Instance.Invoke(OwnerClientId.ToString(), new RemoveInventoryItemSubscriptionEvent
            {
                Item = requirement,
                ClientToken = clientToken,
            });
        }

        CheckCharacterQuestSubscription.Instance.Invoke(key, new CheckCharacterQuestSubscriptionEvent
        {
            Progress = recipe.reward.item.count,
            GameObjectName = recipe.reward.item.type.ToString(),
            ClientToken = clientToken,
        });

        var experienceType = type switch
        {
            CraftingRecipeTypeEnum.Cooking => ExperienceTypeEnum.Cooking,
            _ => ExperienceTypeEnum.None,
        };

        if (experienceType != ExperienceTypeEnum.None)
        {
            AddExperienceSubscription.Instance.Invoke(key, new AddExperienceSubscriptionEvent
            {
                Amount = recipe.reward.experience,
                Type = experienceType,
                ClientToken = clientToken,
            });
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

    private void Exit()
    {
        CraftingUI.Instance.Hide();
        _crafting = null;
    }
}
