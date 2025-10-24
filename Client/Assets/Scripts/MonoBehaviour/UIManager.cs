using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public Texture CanTexture;
    public GameObject InventorySlotPrefab;

    [NonSerialized] public GameObject ProgressBarCanvas;
    [NonSerialized] public GameObject TargetCanvas;
    [NonSerialized] public GameObject QuestCanvas;
    [NonSerialized] public GameObject PlayerCanvas;
    [NonSerialized] public GameObject InventoryCanvas;

    [NonSerialized] public Image CastProgressBar;
    [NonSerialized] public GameObject Target;
    [NonSerialized] public TextMeshProUGUI TargetNameText;
    [NonSerialized] public TextMeshProUGUI TargetHealthPointsText;
    [NonSerialized] public Button QuestAcceptButton;
    [NonSerialized] public TextMeshProUGUI QuestAcceptButtonText;
    [NonSerialized] public Button QuestCancelButton;

    [NonSerialized] public GameObject Quest;
    [NonSerialized] public TextMeshProUGUI QuestTitleText;
    [NonSerialized] public TextMeshProUGUI QuestDescriptionText;
    [NonSerialized] public GameObject QuestLog;
    [NonSerialized] public TextMeshProUGUI QuestLogText;
    [NonSerialized] public TextMeshProUGUI PlayerLevelText;
    [NonSerialized] public TextMeshProUGUI PlayerNameText;
    [NonSerialized] public TextMeshProUGUI PlayerHealthPointsText;
    [NonSerialized] public GameObject Inventory;

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
        InventoryCanvas = GameObject.Find("InventoryCanvas");

        CastProgressBar = GameObject.Find("ProgressBar").GetComponent<Image>();
        Target = TargetCanvas.transform.Find("Target").gameObject;
        TargetNameText = TargetCanvas.transform.Find("Target/Name").GetComponent<TextMeshProUGUI>();
        TargetHealthPointsText = TargetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>();
        QuestAcceptButton = QuestCanvas.transform.Find("Quest/AcceptButton").GetComponent<Button>();
        QuestAcceptButtonText = QuestCanvas.transform.Find("Quest/AcceptButton/Text").GetComponent<TextMeshProUGUI>();
        QuestCancelButton = QuestCanvas.transform.Find("Quest/CancelButton").GetComponent<Button>();
        Quest = QuestCanvas.transform.Find("Quest").gameObject;
        QuestTitleText = QuestCanvas.transform.Find("Quest/Title").GetComponent<TextMeshProUGUI>();
        QuestDescriptionText = QuestCanvas.transform.Find("Quest/Description").GetComponent<TextMeshProUGUI>();
        QuestLog = QuestCanvas.transform.Find("Log").gameObject;
        QuestLogText = QuestCanvas.transform.Find("Log/Text").GetComponent<TextMeshProUGUI>();
        PlayerLevelText = PlayerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>();
        PlayerNameText = PlayerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>();
        PlayerHealthPointsText = PlayerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>();
        Inventory = InventoryCanvas.transform.Find("Inventory").gameObject;

        for (int i = 0; i < 9; i++)
        {
            var slot = Instantiate(InventorySlotPrefab);
            slot.transform.SetParent(Inventory.transform);
        }
    }

    public void UpdateInventory(InventoryItem item)
    {
        var slots = GameObject.FindGameObjectsWithTag("InventorySlot");

        var slot = slots
            .Select(x => new
            {
                Image = x.transform.Find("Background").GetComponent<RawImage>(),
                Text = x.transform.Find("Text").GetComponent<TextMeshProUGUI>()
            })
            //.Where(x => x.Image.texture == null)
            .FirstOrDefault();

        if (slot != null)
        {
            slot.Text.gameObject.SetActive(true);
            slot.Text.text = item.count.ToString();

            slot.Image.color = Color.white;

            slot.Image.texture = item.type switch
            {
                CharacterInventoryTypeEnum.Can => CanTexture,
                _ => null,
            };
        }

        // todo: out of slots
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

    public void ShowQuest(QuestNpc questNpc)
    {
        if (questNpc.CharacterQuest == null || questNpc.CharacterQuest.status == CharacterQuestStatusEnum.Finished)
        {
            Quest.SetActive(true);
            QuestTitleText.text = questNpc.Quest.title;
            QuestDescriptionText.text = questNpc.CharacterQuest == null ? questNpc.Quest.description : questNpc.Quest.completeDescription;
            QuestAcceptButtonText.text = questNpc.CharacterQuest == null ? "Accept" : "Complete";
        }
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
