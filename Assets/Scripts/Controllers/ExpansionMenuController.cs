using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpansionMenuController : MonoBehaviour
{
    public GameObject expansionItemButtonPrefab;
    public Transform contentContainer;
    
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public TextMeshProUGUI upfrontServiceCost;
    public Button buyExpansionButton;
    
    private Expansion selectedExpansion;
    
    void Start()
    {
        LoadItems();
    }

    void OnEnable()
    {
        UIController.OnDetailsViewUpdate += CheckButtonInteractabilities;
        ExpansionManager.OnExpansionBuilt += RefreshItemsAndSelection;
        ProgressionManager.OnProgressionChanged += LoadItems;
        LoadItems();
        if (selectedExpansion != null) UpdateDetailView(selectedExpansion);
    }
    
    void OnDisable()
    {
        UIController.OnDetailsViewUpdate -= CheckButtonInteractabilities;
        ExpansionManager.OnExpansionBuilt -= RefreshItemsAndSelection;
        ProgressionManager.OnProgressionChanged -= LoadItems;
    }

    public void LoadItems()
    {
        UIRuntimeListUtility.ClearChildren(contentContainer);
        
        if (ExpansionManager.Instance == null || ExpansionManager.Instance.allExpansions == null)
        {
            UIRuntimeListUtility.RefreshLayout(contentContainer);
            return;
        }

        foreach (var item in ExpansionManager.Instance.allExpansions)
        {
            if (!ShouldShowExpansion(item))
            {
                continue;
            }

            CreateButton(item);
        }

        UIRuntimeListUtility.RefreshLayout(contentContainer);
    }

    private bool ShouldShowExpansion(Expansion data)
    {
        if (data == null)
        {
            return false;
        }

        if (ProgressionManager.Instance != null && !ProgressionManager.Instance.IsUnlocked(data))
        {
            return false;
        }

        return !ExpansionManager.Instance.TryGetMissingPlatformRequirement(data, out _);
    }

    private void RefreshItemsAndSelection()
    {
        LoadItems();
        CheckButtonInteractabilities();
    }

    private void CreateButton(Expansion data)
    {
        GameObject newButton = Instantiate(expansionItemButtonPrefab, contentContainer);
        
        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage) iconImage.sprite = data.GetIcon();
        
        Button btnComp = newButton.GetComponent<Button>();
        btnComp.onClick.AddListener(() => OnExpansionItemButtonClicked(data));
    }
    
    private void OnExpansionItemButtonClicked(Expansion data)
    {
        selectedExpansion = data;
        UpdateDetailView(data);
    }

    public void OnBuyExpansionButtonClicked()
    {
        if (selectedExpansion != null)
        {
            ExpansionManager.Instance.TryBuyExpansion(selectedExpansion);
            UpdateDetailView(selectedExpansion);
        }
    }
    
    void CheckButtonInteractabilities()
    {
        if (selectedExpansion != null)
        {
            checkBuyExpansionButtonInteractability(selectedExpansion);
        }
    }

    void checkBuyExpansionButtonInteractability(Expansion data)
    {
        bool isUnlocked = ProgressionManager.Instance == null || ProgressionManager.Instance.IsUnlocked(data);
        bool isBuilt = ExpansionManager.Instance.IsExpansionBuilt(data);
        bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.money >= data.upfrontCost;
        bool hasMissingPlatformRequirement = ExpansionManager.Instance.TryGetMissingPlatformRequirement(data, out Expansion requiredPlatform);

        buyExpansionButton.interactable = !isBuilt && isUnlocked && !hasMissingPlatformRequirement && canAfford;

        TextMeshProUGUI buyText = buyExpansionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buyText != null)
        {
            if (isBuilt)
            {
                buyText.text = "Purchased";
            }
            else if (!isUnlocked)
            {
                buyText.text = $"Requires Tier {Mathf.Max(1, data.requiredTier)}";
            }
            else if (hasMissingPlatformRequirement)
            {
                buyText.text = $"Requires {requiredPlatform.name}";
            }
            else
            {
                buyText.text = $"Buy ${data.upfrontCost}";
            }
        }
    }
    
    private void UpdateDetailView(Expansion data)
    {
        if (detailIcon) detailIcon.sprite = data.GetIcon();
        if (detailName) detailName.text = data.name;
        if (detailDescription) detailDescription.text = data.description;
        if (upfrontServiceCost) upfrontServiceCost.text = ExpansionManager.Instance.IsExpansionBuilt(data) ? "Owned" : $"Buy ${data.upfrontCost}";

        checkBuyExpansionButtonInteractability(data);
    }
}
