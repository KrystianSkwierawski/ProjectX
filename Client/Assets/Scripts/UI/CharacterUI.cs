using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class CharacterUI : MonoSingleton<CharacterUI>
    {
        #region

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
            Character.SetActive(true);
        }

        public void Hide()
        {
            if (Character.activeSelf)
            {
                Character.SetActive(false);
            }
        }
    }
}
