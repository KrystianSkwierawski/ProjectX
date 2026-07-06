using System.Linq;
using TMPro;
using UnityEngine;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;

namespace Assets.Scripts.Areas.Character.UI
{
    public class CharacterUI : MonoSingleton<CharacterUI>
    {
        #region GameObject

        public GameObject CharacterCanvas { get; private set; }

        public GameObject Character { get; private set; }

        public GameObject Description { get; private set; }

        #endregion

        public TextMeshProUGUI DescriptionText { get; private set; }

        public void Start()
        {
            CharacterCanvas = GameObject.Find("CharacterCanvas");
            Character = CharacterCanvas.transform.Find("Character").gameObject;
            Description = Character.transform.Find("Description").gameObject;
            DescriptionText = Description.GetComponent<TextMeshProUGUI>();
        }

        public void Show()
        {
            if (Character.activeSelf)
            {
                return;
            }

            // FIXME: array
            CraftingUI.Instance.Hide();
            QuestUI.Instance.Hide();
            MerchantUI.Instance.Hide();
            GearUI.Instance.Hide();
            Character.SetActive(true);
        }

        public void RefreshDescription()
        {
            if (UserManager.Instance.Character?.Levels == null)
            {
                return;
            }

            DescriptionText.text = string.Format(TranslateManager.Instance.GetByKey(TranslateKeyEnum.CharacterDescription), UserManager.Instance.Character.Levels.Values.Cast<object>().ToArray());
        }

        public void Hide()
        {
            if (Character.activeSelf)
            {
                Character.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (Character.activeSelf)
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);

                Hide();

                return;
            }

            if (UserManager.Instance.Character?.Levels == null)
            {
                return;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

            RefreshDescription();

            Show();
        }
    }
}
