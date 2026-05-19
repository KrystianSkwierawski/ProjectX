using System.Linq;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Professions.Enums;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Professions.Mono
{
public class Crafting : NetworkBehaviour
{
    private StarterAssetsInputs _input;
    private GameObject _crafting;

    private Color _originalBarColor;
    private bool _isCrafting = false;
    private float _craftingTime = 4f;
    private float _craftingTimer = 0f;
    private float _sfxTimer;
    private float _sfxTime;

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
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CraftingUI.Instance.Hide();
            }

            CheckHide();
            CheckCraftingClicked();
            CheckCrafting();
            CheckSfx();
        }
    }

    private void StartCrafting()
    {
        _sfxTimer = CraftingUI.Instance.CurrentType switch
        {
            CraftingRecipeTypeEnum.Cooking => AudioManager.Instance.AudioClips[AudioTypeEnum.CookingPrepare].length,
            CraftingRecipeTypeEnum.Blacksmithing => AudioManager.Instance.AudioClips[AudioTypeEnum.Blacksmithing].length + 0.5f,
            CraftingRecipeTypeEnum.Alchemy => AudioManager.Instance.AudioClips[AudioTypeEnum.Alchemy].length,
            _ => 0f
        };

        _sfxTime = _sfxTimer;

        _originalBarColor = PlayerUI.Instance.CastProgressBar.color;
        _isCrafting = true;
        _craftingTimer = 0f;
        PlayerUI.Instance.UpdateCastBar(_craftingTimer / _craftingTime);
        CraftingUI.Instance.CraftButton.interactable = false;
    }

    private void CheckCrafting()
    {
        if (!_isCrafting)
        {
            return;
        }

        // TODO: interrupt
        //PlayerUI.Instance.CastProgressBar.color = _originalBarColor;

        _craftingTimer += Time.deltaTime;
        PlayerUI.Instance.UpdateCastBar(_craftingTimer / _craftingTime);

        if (_craftingTimer >= _craftingTime)
        {
            StopCrafting();
            CraftServerRpc(CraftingUI.Instance.CurrentRecipe.Id, CraftingUI.Instance.CurrentType, UserManager.Instance.Token);
        }
    }

    private void StopCrafting()
    {
        _isCrafting = false;
        _craftingTimer = 0f;
        PlayerUI.Instance.HideCastBar();
        CraftingUI.Instance.CraftButton.interactable = true;
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

        var recipe = dto.CraftingRecipes
            .Where(x => x.Id == id)
            .Single();

        var key = OwnerClientId.ToString();

        UpdateInventorySubscription.Instance.Invoke(key, new UpdateInventorySubscriptionEvent
        {
            Request = new UpdateCharacterInventoryCommand
            {
                CharacterId = 1,
                Add = new InventoryItemDto[] { recipe.Reward.Item },
                Remove = recipe.Requirement.Items
            },
            ClientToken = clientToken,
        });

        var experienceType = type switch
        {
            CraftingRecipeTypeEnum.Cooking => ExperienceTypeEnum.Cooking,
            CraftingRecipeTypeEnum.Blacksmithing => ExperienceTypeEnum.Blacksmithing,
            CraftingRecipeTypeEnum.Alchemy => ExperienceTypeEnum.Alchemy,
            _ => ExperienceTypeEnum.None,
        };

        if (experienceType != ExperienceTypeEnum.None)
        {
            AddExperienceSubscription.Instance.Invoke(key, new AddExperienceSubscriptionEvent
            {
                Amount = recipe.Reward.Experience,
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

    private void CheckCraftingClicked()
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
                "CookingCrafting" => CraftingRecipeTypeEnum.Cooking,
                "BlacksmithingCrafting" => CraftingRecipeTypeEnum.Blacksmithing,
                "AlchemyCrafting" => CraftingRecipeTypeEnum.Alchemy,
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

    private void CheckSfx()
    {
        if (!_isCrafting)
        {
            return;
        }

        _sfxTimer += Time.deltaTime;

        if (_sfxTimer >= _sfxTime)
        {
            var audioType = CraftingUI.Instance.CurrentType switch
            {
                CraftingRecipeTypeEnum.Cooking => AudioTypeEnum.CookingPrepare,
                CraftingRecipeTypeEnum.Blacksmithing => AudioTypeEnum.Blacksmithing,
                CraftingRecipeTypeEnum.Alchemy => AudioTypeEnum.Alchemy,
                _ => AudioTypeEnum.None
            };

            if (audioType != AudioTypeEnum.None)
            {
                AudioManager.Instance.TryPlayOneShot(audioType, 0.5f);
            }

            _sfxTimer = 0f;
        }
    }

    private void Exit()
    {
        CraftingUI.Instance.Hide();
        _crafting = null;
    }
}
}