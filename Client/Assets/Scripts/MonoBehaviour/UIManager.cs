using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [NonSerialized] public GameObject ProgressBarCanvas;
    [NonSerialized] public GameObject TargetCanvas;
    [NonSerialized] public GameObject QuestCanvas;
    [NonSerialized] public GameObject PlayerCanvas;

    [NonSerialized] public Image CastProgressBar;
    [NonSerialized] public GameObject Target;
    [NonSerialized] public TextMeshProUGUI TargetNameText;
    [NonSerialized] public TextMeshProUGUI TargetHealthPointsText;
    [NonSerialized] public Button QuestAcceptButton;
    [NonSerialized] public Button QuestDeclineButton;

    [NonSerialized] public GameObject Quest;
    [NonSerialized] public TextMeshProUGUI QuestTitleText;
    [NonSerialized] public TextMeshProUGUI QuestDescriptionText;
    [NonSerialized] public GameObject QuestLog;
    [NonSerialized] public TextMeshProUGUI QuestLogText;
    [NonSerialized] public TextMeshProUGUI PlayerLevelText;
    [NonSerialized] public TextMeshProUGUI PlayerNameText;
    [NonSerialized] public TextMeshProUGUI PlayerHealthPointsText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Init()
    {
        ProgressBarCanvas = GameObject.Find("ProgressBarCanvas");
        TargetCanvas = GameObject.Find("TargetCanvas");
        QuestCanvas = GameObject.Find("QuestCanvas");
        PlayerCanvas = GameObject.Find("PlayerCanvas");

        CastProgressBar = GameObject.Find("ProgressBar").GetComponent<Image>();
        Target = TargetCanvas.transform.Find("Target").gameObject;
        TargetNameText = TargetCanvas.transform.Find("Target/Name").GetComponent<TextMeshProUGUI>();
        TargetHealthPointsText = TargetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>();
        QuestAcceptButton = QuestCanvas.transform.Find("Quest/AcceptButton").GetComponent<Button>();
        QuestDeclineButton = QuestCanvas.transform.Find("Quest/DeclineButton").GetComponent<Button>();
        Quest = QuestCanvas.transform.Find("Quest").gameObject;
        QuestTitleText = QuestCanvas.transform.Find("Quest/Title").GetComponent<TextMeshProUGUI>();
        QuestDescriptionText = QuestCanvas.transform.Find("Quest/Description").GetComponent<TextMeshProUGUI>();
        QuestLog = QuestCanvas.transform.Find("Log").gameObject;
        QuestLogText = QuestCanvas.transform.Find("Log/Text").GetComponent<TextMeshProUGUI>();
        PlayerLevelText = PlayerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>();
        PlayerNameText = PlayerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>();
        PlayerHealthPointsText = PlayerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>();
    }

    public void SetTarget(string name, string health)
    {
        Target.SetActive(true);
        TargetNameText.text = name;
        TargetHealthPointsText.text = health;
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
            CastProgressBar.color = Color.red;
            CastProgressBar.fillAmount = 1f;
            ProgressBarCanvas.SetActive(true);
        }
    }

    public void ShowQuest(string title, string description)
    {
        Quest.SetActive(true);
        QuestTitleText.text = title;
        QuestDescriptionText.text = description;
    }

    public void HideQuestCanvas()
    {
        Quest.SetActive(false);
    }

    public void SetQuestLog(string text)
    {
        QuestLog.SetActive(true);
        QuestLogText.text = text;
    }

    public void SetPlayer(string name, string health, string level)
    {
        PlayerNameText.text = name;
        PlayerHealthPointsText.text = health;
        PlayerLevelText.text = $"Level: {level}";
    }
}
