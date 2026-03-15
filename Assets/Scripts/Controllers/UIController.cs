using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;
    
    [Header("Button References")]
    public Button settingsButton;
    public Button cameraSwitchButton;
    public Button trainMenuButton;
    public Button buildMenuButton;
    public Button staffMenuButton;
    public Button ratingsMenuButton;
    public Button expansionMenuButton;
    public Button platformMenuButton;

    [Header("Panel References")]
    public GameObject settingsPanel;
    public GameObject trainPanel;
    public GameObject buildPanel;
    public GameObject staffPanel;
    public GameObject ratingsPanel;
    public GameObject expansionPanel;
    public GameObject platformPanel;
    
    [Header("Popup References")]
    public GameObject trainSelectionPopup;

    [Header("System References")]
    [SerializeField] private BuildController buildController;
    
    [Header("UI References")]
    public TextMeshProUGUI moneyText;
    public Color positiveMoneyColor = new Color(6, 159, 0);
    public Color negativeMoneyColor = new Color(188, 0, 0);

    private GameObject currentActivePanel;

    public static event Action OnDetailsViewUpdate;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CloseAllPanels();
        CloseTrainSelectionPopup();

        if (settingsButton) settingsButton.onClick.AddListener(() => TogglePanel(settingsPanel));
        if (trainMenuButton) trainMenuButton.onClick.AddListener(() => TogglePanel(trainPanel));
        if (staffMenuButton) staffMenuButton.onClick.AddListener(() => TogglePanel(staffPanel));
        if (ratingsMenuButton) ratingsMenuButton.onClick.AddListener(() => TogglePanel(ratingsPanel));
        if (expansionMenuButton) expansionMenuButton.onClick.AddListener(() => TogglePanel(expansionPanel));
        if (platformMenuButton) platformMenuButton.onClick.AddListener(() => TogglePanel(platformPanel));

        if (cameraSwitchButton) cameraSwitchButton.onClick.AddListener(OnCameraSwitchClicked);

        if (buildMenuButton) buildMenuButton.onClick.AddListener(OnBuildMenuClicked);
    }

    void OnEnable()
    {
        EconomyManager.OnMoneyChanged += OnMoneyChanged;
    }
    
    void OnDisable()
    {
        EconomyManager.OnMoneyChanged -= OnMoneyChanged;
    }

    private void TogglePanel(GameObject panelToToggle)
    {
        if (currentActivePanel == panelToToggle)
        {
            CloseAllPanels();
            return;
        }

        if (currentActivePanel != null)
        {
            currentActivePanel.SetActive(false);
        }

        if (panelToToggle != null)
        {
            panelToToggle.SetActive(true);
            currentActivePanel = panelToToggle;
        }

        if (buildController != null)
        {
            buildController.isBuildingMode = false;
        }
    }

    private void OnBuildMenuClicked()
    {
        if (currentActivePanel == buildPanel)
        {
            CloseAllPanels();
        }
        else
        {
            if (currentActivePanel != null) currentActivePanel.SetActive(false);

            buildPanel.SetActive(true);
            currentActivePanel = buildPanel;

            if (buildController != null) 
            {
                buildController.isBuildingMode = true;
            }
        }
    }

    private void OnCameraSwitchClicked()
    {
    }

    public void CloseAllPanels()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (trainPanel) trainPanel.SetActive(false);
        if (buildPanel) buildPanel.SetActive(false);
        if (staffPanel) staffPanel.SetActive(false);
        if (ratingsPanel) ratingsPanel.SetActive(false);
        if (expansionPanel) expansionPanel.SetActive(false);
        if (platformPanel) platformPanel.SetActive(false);

        currentActivePanel = null;

        if (buildController != null) 
        {
            buildController.isBuildingMode = false;
        }
    }

    public void OpenTrainSelectionPopup()
    {
        if (trainSelectionPopup) trainSelectionPopup.SetActive(true);
    }

    public void CloseTrainSelectionPopup()
    {
        if (trainSelectionPopup) trainSelectionPopup.SetActive(false);
    }

    private void OnDestroy()
    {
        if (settingsButton) settingsButton.onClick.RemoveAllListeners();
        if (cameraSwitchButton) cameraSwitchButton.onClick.RemoveAllListeners();
        if (trainMenuButton) trainMenuButton.onClick.RemoveAllListeners();
        if (buildMenuButton) buildMenuButton.onClick.RemoveAllListeners();
        if (staffMenuButton) staffMenuButton.onClick.RemoveAllListeners();
        if (ratingsMenuButton) ratingsMenuButton.onClick.RemoveAllListeners();
        if (expansionMenuButton) expansionMenuButton.onClick.RemoveAllListeners();
        if (platformMenuButton) platformMenuButton.onClick.RemoveAllListeners();
    }
    
    public void OnMoneyChanged(int amount)
    {
        UpdateMoneyText(amount);
        if (currentActivePanel != null)
        {
            OnDetailsViewUpdate?.Invoke();
        }
    }

    void UpdateMoneyText(int amount)
    {
        string abbreviatedAmount = AbbreviateNumber(amount);
        
        if (moneyText != null)
        {
            if (amount >= 0 && moneyText.color != positiveMoneyColor)
            {
                moneyText.color = positiveMoneyColor;
            }
            else if (amount < 0 && moneyText.color != negativeMoneyColor)
            {
                moneyText.color = negativeMoneyColor;
            }
            moneyText.text = "$" + abbreviatedAmount;
        }
    }
    
    public string AbbreviateNumber(int number)
    {
        if (number < 10000) return number.ToString();

        string[] suffixes = { "", "k", "M", "B", "T", "Qa", "Qi" };
        int suffixIndex = 0;
        double abbreviatedNumber = number;

        while (abbreviatedNumber >= 1000 && suffixIndex < suffixes.Length - 1)
        {
            abbreviatedNumber /= 1000;
            suffixIndex++;
        }

        string format;
        if (abbreviatedNumber >= 100)
            format = "0";
        else if (abbreviatedNumber >= 10)
            format = "0.#";
        else
            format = "0.##";

        return $"{abbreviatedNumber.ToString(format)}{suffixes[suffixIndex]}";
    }
}