using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;
    public static bool IsUiHidden => Instance != null && Instance.isUiHidden;
    public static bool IsCameraInputBlockedByMenu => Instance != null && !Instance.isUiHidden && Instance.HasCameraBlockingMenuOpen();
    
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
    public TextMeshProUGUI incomeText;
    public Color positiveMoneyColor = new Color(6, 159, 0);
    public Color negativeMoneyColor = new Color(188, 0, 0);

    [Header("Pointer Feel")]
    [SerializeField] private int uiDragThresholdPixels = 24;
    
    [Header("Bill Feed")]
    public GameObject billPrefab;
    public float billLifetime = 5f;
    public float billFadeDuration = 0.4f;
    public float billMoveDuration = 0.25f;
    public float billSpacing = 56f;

    private GameObject currentActivePanel;
    private RectTransform billContainer;
    private readonly List<BillEntry> activeBills = new List<BillEntry>();
    private readonly Dictionary<Canvas, bool> canvasEnabledStates = new Dictionary<Canvas, bool>();
    private readonly List<Canvas> staleCanvasStateKeys = new List<Canvas>();
    private bool isUiHidden;

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
        ConfigureEventSystemDragThreshold();
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

        RefreshTopBarButtonVisibility();

        if (EconomyManager.Instance != null)
        {
            OnMoneyChanged(EconomyManager.Instance.money);
            OnIncomePerMinuteChanged(EconomyManager.Instance.CurrentIncomePerMinute);
        }
    }

    void OnEnable()
    {
        ConfigureEventSystemDragThreshold();
        EconomyManager.OnMoneyChanged += OnMoneyChanged;
        EconomyManager.OnIncomePerMinuteChanged += OnIncomePerMinuteChanged;
        EconomyManager.OnExpenseRecorded += OnExpenseRecorded;
        ProgressionManager.OnProgressionChanged += RefreshTopBarButtonVisibility;
        TrainManager.OnTrainAssignmentsChanged += RefreshTopBarButtonVisibility;
        ExpansionManager.OnExpansionBuilt += RefreshTopBarButtonVisibility;
    }
    
    void OnDisable()
    {
        if (isUiHidden)
        {
            SetUiHidden(false);
        }

        EconomyManager.OnMoneyChanged -= OnMoneyChanged;
        EconomyManager.OnIncomePerMinuteChanged -= OnIncomePerMinuteChanged;
        EconomyManager.OnExpenseRecorded -= OnExpenseRecorded;
        ProgressionManager.OnProgressionChanged -= RefreshTopBarButtonVisibility;
        TrainManager.OnTrainAssignmentsChanged -= RefreshTopBarButtonVisibility;
        ExpansionManager.OnExpansionBuilt -= RefreshTopBarButtonVisibility;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
        {
            ToggleUiVisibility();
        }

        if (isUiHidden || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (trainSelectionPopup != null && trainSelectionPopup.activeSelf)
        {
            CloseTrainSelectionPopup();
            return;
        }

        if (currentActivePanel != null)
        {
            CloseAllPanels();
        }
    }

    void LateUpdate()
    {
        if (isUiHidden)
        {
            ApplyHiddenStateToCanvases();
        }
    }

    public void ToggleUiVisibility()
    {
        SetUiHidden(!isUiHidden);
    }

    public void SetUiHidden(bool hidden)
    {
        if (isUiHidden == hidden)
        {
            if (isUiHidden)
            {
                ApplyHiddenStateToCanvases();
            }

            return;
        }

        isUiHidden = hidden;

        if (isUiHidden)
        {
            ApplyHiddenStateToCanvases();
        }
        else
        {
            RestoreCanvasStates();
        }
    }

    private void ApplyHiddenStateToCanvases()
    {
        RemoveStaleCanvasStateKeys();

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
            {
                continue;
            }

            if (ShouldPreserveCanvasWhenUiHidden(canvas))
            {
                continue;
            }

            if (!canvasEnabledStates.ContainsKey(canvas))
            {
                canvasEnabledStates[canvas] = canvas.enabled;
            }

            if (canvas.enabled)
            {
                canvas.enabled = false;
            }
        }
    }

    private void RestoreCanvasStates()
    {
        foreach (KeyValuePair<Canvas, bool> canvasState in canvasEnabledStates)
        {
            Canvas canvas = canvasState.Key;
            if (canvas != null)
            {
                canvas.enabled = canvasState.Value;
            }
        }

        canvasEnabledStates.Clear();
    }

    private static bool ShouldPreserveCanvasWhenUiHidden(Canvas canvas)
    {
        return canvas != null && canvas.GetComponentInParent<PlacedBuildable>() != null;
    }

    private void RemoveStaleCanvasStateKeys()
    {
        staleCanvasStateKeys.Clear();

        foreach (Canvas canvas in canvasEnabledStates.Keys)
        {
            if (canvas == null)
            {
                staleCanvasStateKeys.Add(canvas);
            }
        }

        for (int i = 0; i < staleCanvasStateKeys.Count; i++)
        {
            canvasEnabledStates.Remove(staleCanvasStateKeys[i]);
        }
    }

    private void TogglePanel(GameObject panelToToggle)
    {
        if (currentActivePanel == panelToToggle)
        {
            CloseAllPanels();
            return;
        }

        OpenPanel(panelToToggle);
    }

    private void OpenPanel(GameObject panelToOpen)
    {
        if (currentActivePanel != null)
        {
            currentActivePanel.SetActive(false);
        }

        if (panelToOpen != null)
        {
            panelToOpen.SetActive(true);
            currentActivePanel = panelToOpen;
        }

        if (buildController != null)
        {
            buildController.ExitBuildModes();
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
                buildController.ResumeBuildMode();
            }
        }
    }

    private void OnCameraSwitchClicked()
    {
    }

    private bool HasCameraBlockingMenuOpen()
    {
        return IsPanelOpen(trainSelectionPopup) ||
               IsPanelOpen(settingsPanel) ||
               IsPanelOpen(trainPanel) ||
               IsPanelOpen(staffPanel) ||
               IsPanelOpen(ratingsPanel) ||
               IsPanelOpen(expansionPanel) ||
               IsPanelOpen(platformPanel) ||
               IsPanelOpen(progressionPanel);
    }

    private static bool IsPanelOpen(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    private void RefreshTopBarButtonVisibility()
    {
        SetButtonVisible(cameraSwitchButton, false);
        SetButtonVisible(trainMenuButton, HasUnlockedTrains(), trainPanel);
        SetButtonVisible(buildMenuButton, HasUnlockedBuildItems(), buildPanel);
        SetButtonVisible(staffMenuButton, HasUnlockedStaff(), staffPanel);
        SetButtonVisible(expansionMenuButton, HasUnlockedExpansions(), expansionPanel);
        SetButtonVisible(platformMenuButton, HasPlatformMenuFunctionality(), platformPanel);
        SetButtonVisible(progressionMenuButton, true, progressionPanel);
    }

    private void SetButtonVisible(Button button, bool isVisible, GameObject linkedPanel = null)
    {
        if (button == null)
        {
            return;
        }

        if (!isVisible && linkedPanel != null && currentActivePanel == linkedPanel)
        {
            CloseAllPanels();
        }

        if (button.gameObject.activeSelf != isVisible)
        {
            button.gameObject.SetActive(isVisible);
        }
    }

    private bool HasUnlockedBuildItems()
    {
        ObjectBuildable[] buildItems = Resources.LoadAll<ObjectBuildable>("BuildItems");
        if (buildItems == null || buildItems.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < buildItems.Length; i++)
        {
            if (buildItems[i] != null &&
                (ProgressionManager.Instance == null || ProgressionManager.Instance.IsUnlocked(buildItems[i])))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasUnlockedTrains()
    {
        Train[] trains = Resources.LoadAll<Train>("Trains");
        if (trains == null || trains.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < trains.Length; i++)
        {
            if (trains[i] != null &&
                (ProgressionManager.Instance == null || ProgressionManager.Instance.IsUnlocked(trains[i])))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasUnlockedStaff()
    {
        StaffMember[] staffMembers = Resources.LoadAll<StaffMember>("Staff");
        if (staffMembers == null || staffMembers.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < staffMembers.Length; i++)
        {
            if (staffMembers[i] != null &&
                (ProgressionManager.Instance == null || ProgressionManager.Instance.IsUnlocked(staffMembers[i])))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasUnlockedExpansions()
    {
        Expansion[] expansions = Resources.LoadAll<Expansion>("Expansions");
        if (expansions == null || expansions.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < expansions.Length; i++)
        {
            if (expansions[i] != null &&
                (ProgressionManager.Instance == null || ProgressionManager.Instance.IsUnlocked(expansions[i])))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasPlatformMenuFunctionality()
    {
        return TrainManager.Instance != null &&
               TrainManager.Instance.activePlatforms != null &&
               TrainManager.Instance.activePlatforms.Count > 0 &&
               TrainManager.Instance.unlockedTrains != null &&
               TrainManager.Instance.unlockedTrains.Count > 0;
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
            buildController.ExitBuildModes();
        }
    }

    public void OpenTrainSelectionPopup()
    {
        if (trainSelectionPopup) trainSelectionPopup.SetActive(true);
    }

    public void CloseTrainSelectionPopup()
    {
        if (trainSelectionPopup) trainSelectionPopup.SetActive(false);

        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.pendingPlatform = null;
            TrainManager.Instance.pendingSlot = -1;
        }
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
        PointerUiUtility.DisableRaycastTargets(billObject);
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

    private void OnIncomePerMinuteChanged(int amountPerMinute)
    {
        UpdateIncomeText(amountPerMinute);
    }

    void UpdateMoneyText(int amount)
    {
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
            moneyText.text = FormatMoney(amount);
        }
    }

    private void UpdateIncomeText(int amountPerMinute)
    {
        if (incomeText == null)
        {
            return;
        }

        if (amountPerMinute < 0)
        {
            incomeText.color = negativeMoneyColor;
            incomeText.text = "-$" + FormatWholeNumber(Mathf.Abs(amountPerMinute)) + "/m";
            return;
        }

        incomeText.color = positiveMoneyColor;
        incomeText.text = amountPerMinute > 0
            ? "+$" + FormatWholeNumber(amountPerMinute) + "/m"
            : "$0/m";
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
            entry.text.text = "-$" + FormatWholeNumber(amount);
            entry.text.color = negativeMoneyColor;
        }

        if (entry.image != null)
        {
            entry.image.sprite = icon;
            entry.image.color = Color.white;
            entry.image.enabled = icon != null;
            entry.image.preserveAspect = true;
        }

        entry.canvasGroup.alpha = 0f;
    }

    private void ConfigureEventSystemDragThreshold()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.pixelDragThreshold = Mathf.Max(
            EventSystem.current.pixelDragThreshold,
            uiDragThresholdPixels);
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
    
    private string FormatMoney(int amount)
    {
        string formattedNumber = FormatWholeNumber(Mathf.Abs(amount));
        return amount < 0 ? "-$" + formattedNumber : "$" + formattedNumber;
    }

    private string FormatWholeNumber(int number)
    {
        return number.ToString("N0", CultureInfo.InvariantCulture);
    }
}

public static class UIRuntimeListUtility
{
    public static void ClearChildren(Transform container)
    {
        if (container == null)
        {
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            child.SetParent(null, false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    public static void RefreshLayout(Transform container)
    {
        RectTransform current = container as RectTransform;
        if (current == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        RectTransform root = current;
        ResizeToPreferredSize(current);

        while (current != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(current);
            current = current.parent as RectTransform;
        }

        ExpandHeightToVisibleChildBounds(root);
        current = root;
        while (current != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(current);
            current = current.parent as RectTransform;
        }

        Canvas.ForceUpdateCanvases();
    }

    private static void ResizeToPreferredSize(RectTransform target)
    {
        if (target == null || target.GetComponent<LayoutGroup>() == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(target);

        float preferredWidth = LayoutUtility.GetPreferredWidth(target);
        float preferredHeight = LayoutUtility.GetPreferredHeight(target);
        Vector2 sizeDelta = target.sizeDelta;

        if (target.anchorMin.x == target.anchorMax.x && preferredWidth > 0f)
        {
            sizeDelta.x = preferredWidth;
        }

        if (target.anchorMin.y == target.anchorMax.y)
        {
            sizeDelta.y = Mathf.Max(0f, preferredHeight);
        }

        target.sizeDelta = sizeDelta;
    }

    private static void ExpandHeightToVisibleChildBounds(RectTransform target)
    {
        if (target == null || target.childCount == 0 || target.anchorMin.y != target.anchorMax.y)
        {
            return;
        }

        Bounds visibleBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(target, target);
        float overflowBelow = target.rect.yMin - visibleBounds.min.y;
        if (overflowBelow <= 0.01f || target.pivot.y <= 0.01f)
        {
            return;
        }

        Vector2 sizeDelta = target.sizeDelta;
        sizeDelta.y += Mathf.Ceil(overflowBelow / target.pivot.y);
        target.sizeDelta = sizeDelta;
    }
}
