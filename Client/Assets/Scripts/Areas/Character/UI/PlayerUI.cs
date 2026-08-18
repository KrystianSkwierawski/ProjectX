using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Character.UI
{
    public class PlayerUI : MonoSingleton<PlayerUI>
    {
        #region GameObject

        public GameObject PlayerCanvas { get; private set; }

        public GameObject ProgressBarCanvas { get; private set; }

        #endregion

        #region TextMesh

        public TextMeshProUGUI PlayerLevelText { get; private set; }

        public TextMeshProUGUI PlayerNameText { get; private set; }

        public TextMeshProUGUI PlayerHealthPointsText { get; private set; }

        #endregion

        #region Image

        public Image CastProgressBar { get; private set; }

        #endregion

        public void Start()
        {
            PlayerCanvas = GameObject.Find("PlayerCanvas");
            ProgressBarCanvas = GameObject.Find("ProgressBarCanvas");
            PlayerLevelText = PlayerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>();
            PlayerNameText = PlayerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>();
            PlayerHealthPointsText = PlayerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>();
            CastProgressBar = GameObject.Find("ProgressBar").GetComponent<Image>();
        }

        public void UpdateCastBar(float progress)
        {
            if (CastProgressBar != null)
            {
                ProgressBarCanvas.SetActive(true);
                CastProgressBar.fillAmount = Mathf.Clamp01(progress);
            }
        }

        public void HideCastBar()
        {
            if (CastProgressBar != null)
            {
                ProgressBarCanvas.SetActive(false);
            }
        }

        public void FailCastBar()
        {
            if (CastProgressBar != null)
            {
                CastProgressBar.color = ColorUI.RedA;
                CastProgressBar.fillAmount = 1f;
                ProgressBarCanvas.SetActive(true);
            }
        }

        public void SetPlayer()
        {
            var character = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId];

            SetName(character.Name);

            SetHealthText(character.Health, character.MaxHealth);

            SetMainLevel(character.Levels[ExperienceTypeEnum.Main]);
        }

        public void SetName(string name)
        {
            PlayerNameText.text = name;
        }

        public void SetHealth(int health)
        {
            var character = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId];

            SetHealthText(health, character.MaxHealth);
        }

        public void SetMaxHealth(int maxHealth)
        {
            var character = UserManager.Instance.Characters[NetworkManager.Singleton.LocalClientId];

            SetHealthText(character.Health, maxHealth);
        }

        public void SetMainLevel(int level)
        {
            PlayerLevelText.text = $"Level: {level}";
        }

        private void SetHealthText(int health, int maxHealth)
        {
            PlayerHealthPointsText.text = $"{health}/{maxHealth}";
        }
    }
}
