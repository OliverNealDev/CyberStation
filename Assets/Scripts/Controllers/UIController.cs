using System;
using System.Collections;
using System.Collections.Generic;
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
    public Button progressionMenuButton;

    [Header("Panel References")]
    public GameObject settingsPanel;
    public GameObject trainPanel;
    public GameObject buildPanel;
    public GameObject staffPanel;
    public GameObject ratingsPanel;
    public GameObject expansionPanel;
    public GameObject platformPanel;
    public GameObject progressionPanel;
    
    [Header("Popup References")]
    public GameObject trainSelectionPopup;

    [Header("System References")]
    [SerializeField] private BuildController buildController;
    
    [Header("UI References")]
    public TextMeshProUGUI moneyText;
    public Color positiveMoneyColor = new Color(6, 159, 0);
    public Color negativeMoneyColor = new Color(188, 0, 0);
    
    [Header("Bill Feed")]
    public GameObject billPrefab;
    public float billLifetime = 5f;
    public float billFadeDuration = 0.4f;
    public float billMoveDuration = 0.25f;
    public float billSpacing = 56f;

    private GameObject currentActivePanel;
    private RectTransform billContainer;
    private readonly List<BillEntry> activeBills = new List<BillEntry>();

    public static event Action OnDetailsViewUpdate;

    private class BillEntry
    {
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI text;
        public Image image;
        public Coroutine moveCoroutine;
    }

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
        if (progressionMenuButton) progressionMenuButton.onClick.AddListener(() => TogglePanel(progressionPanel));

        if (cameraSwitchButton) cameraSwitchButton.onClick.AddListener(OnCameraSwitchClicked);

        if (buildMenuButton) buildMenuButton.onClick.AddListener(OnBuildMenuClicked);
    }

    void OnEnable()
    {
        EconomyManager.OnMoneyChanged += OnMoneyChanged;
        EconomyManager.OnExpenseRecorded += OnExpenseRecorded;
    }
    
    void OnDisable()
    {
        EconomyManager.OnMoneyChanged -= OnMoneyChanged;
        EconomyManager.OnExpenseRecorded -= OnExpenseRecorded;
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
         if (progressionPanel) progressionPanel.SetActive(false);

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
        if (progressionMenuButton) progressionMenuButton.onClick.RemoveAllListeners();
    }

    private void OnExpenseRecorded(int amount, Sprite icon)
    {
        if (amount <= 0 || billPrefab == null) return;

        EnsureBillContainer();
        if (billContainer == null) return;

        GameObject billObject = Instantiate(billPrefab, billContainer);
        RectTransform billRect = billObject.GetComponent<RectTransform>();
        CanvasGroup billCanvasGroup = billObject.GetComponent<CanvasGroup>();
        if (billCanvasGroup == null)
        {
            billCanvasGroup = billObject.AddComponent<CanvasGroup>();
        }

        BillEntry entry = new BillEntry
        {
            rectTransform = billRect,
            canvasGroup = billCanvasGroup,
            text = billObject.GetComponentInChildren<TextMeshProUGUI>(true),
            image = billObject.GetComponentInChildren<Image>(true)
        };

        ConfigureBillVisuals(entry, amount, icon);
        activeBills.Add(entry);
        RefreshBillPositions();

        StartCoroutine(BillLifecycle(entry));
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

    private void EnsureBillContainer()
    {
        if (billContainer != null) return;

        RectTransform moneyRect = moneyText != null ? moneyText.rectTransform : null;
        RectTransform parentRect = moneyRect != null && moneyRect.parent is RectTransform
            ? (RectTransform)moneyRect.parent
            : transform as RectTransform;

        if (parentRect == null) return;

        GameObject containerObject = new GameObject("BillFeed", typeof(RectTransform));
        billContainer = containerObject.GetComponent<RectTransform>();
        billContainer.SetParent(parentRect, false);
        billContainer.anchorMin = moneyRect != null ? moneyRect.anchorMin : new Vector2(0.5f, 1f);
        billContainer.anchorMax = moneyRect != null ? moneyRect.anchorMax : new Vector2(0.5f, 1f);
        billContainer.pivot = new Vector2(0.5f, 1f);

        Vector2 basePosition = moneyRect != null ? moneyRect.anchoredPosition : new Vector2(0f, -40f);
        billContainer.anchoredPosition = basePosition + new Vector2(0f, -42f);
        billContainer.sizeDelta = new Vector2(360f, 400f);
    }

    private void ConfigureBillVisuals(BillEntry entry, int amount, Sprite icon)
    {
        if (entry.rectTransform == null) return;

        entry.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        entry.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        entry.rectTransform.pivot = new Vector2(0.5f, 1f);
        entry.rectTransform.sizeDelta = new Vector2(340f, 52f);
        entry.rectTransform.anchoredPosition = new Vector2(0f, 16f);

        if (entry.text != null)
        {
            entry.text.text = $"-${amount}";
            entry.text.color = negativeMoneyColor;
        }

        if (entry.image != null)
        {
            entry.image.sprite = icon;
            entry.image.enabled = icon != null;
        }

        entry.canvasGroup.alpha = 0f;
    }

    private IEnumerator BillLifecycle(BillEntry entry)
    {
        yield return FadeBill(entry, 0f, 1f, billFadeDuration);
        yield return new WaitForSeconds(billLifetime);
        yield return FadeBill(entry, 1f, 0f, billFadeDuration);

        activeBills.Remove(entry);
        if (entry.rectTransform != null)
        {
            Destroy(entry.rectTransform.gameObject);
        }

        RefreshBillPositions();
    }

    private IEnumerator FadeBill(BillEntry entry, float from, float to, float duration)
    {
        if (entry.canvasGroup == null) yield break;

        float elapsed = 0f;
        entry.canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : elapsed / duration;
            entry.canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        entry.canvasGroup.alpha = to;
    }

    private void RefreshBillPositions()
    {
        for (int i = 0; i < activeBills.Count; i++)
        {
            BillEntry entry = activeBills[i];
            if (entry.rectTransform == null) continue;

            Vector2 targetPosition = new Vector2(0f, -(i * billSpacing));

            if (entry.moveCoroutine != null)
            {
                StopCoroutine(entry.moveCoroutine);
            }

            entry.moveCoroutine = StartCoroutine(AnimateBillPosition(entry.rectTransform, targetPosition));
        }
    }

    private IEnumerator AnimateBillPosition(RectTransform rectTransform, Vector2 targetPosition)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < billMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = billMoveDuration <= 0f ? 1f : elapsed / billMoveDuration;
            float easedT = t * t * (3f - 2f * t);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedT);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
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
