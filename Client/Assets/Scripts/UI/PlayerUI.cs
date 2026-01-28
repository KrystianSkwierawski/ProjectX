using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
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

        #region Texture

        public Texture2D CursorPointer { get; private set; }

        #endregion

        public void Start()
        {
            PlayerCanvas = GameObject.Find("PlayerCanvas");
            ProgressBarCanvas = GameObject.Find("ProgressBarCanvas");
            PlayerLevelText = PlayerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>();
            PlayerNameText = PlayerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>();
            PlayerHealthPointsText = PlayerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>();
            CastProgressBar = GameObject.Find("ProgressBar").GetComponent<Image>();
            CursorPointer = Resources.Load<Texture2D>($"Textures/CursorPointer");
        }

        public void ShowCastBar(float progress)
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

        public void SetPlayer(string name, string health, string level)
        {
            PlayerNameText.text = name;
            PlayerHealthPointsText.text = health;
            PlayerLevelText.text = $"Level: {level}";
        }
    }
}
