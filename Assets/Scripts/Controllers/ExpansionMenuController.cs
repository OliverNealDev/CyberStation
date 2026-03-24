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
        ExpansionManager.OnExpansionBuilt += CheckButtonInteractabilities;
        if (selectedExpansion != null) UpdateDetailView(selectedExpansion);
    }
    
    void OnDisable()
    {
        UIController.OnDetailsViewUpdate -= CheckButtonInteractabilities;
        ExpansionManager.OnExpansionBuilt -= CheckButtonInteractabilities;
    }

    public void LoadItems()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
        
        if (ExpansionManager.Instance == null || ExpansionManager.Instance.allExpansions == null) return;

        foreach (var item in ExpansionManager.Instance.allExpansions)
        {
            CreateButton(item);
        }
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
        bool isBuilt = ExpansionManager.Instance.IsExpansionBuilt(data);
        bool canAfford = EconomyManager.Instance.money >= data.upfrontCost;

        buyExpansionButton.interactable = !isBuilt && canAfford;

        TextMeshProUGUI buyText = buyExpansionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buyText != null)
        {
            buyText.text = isBuilt ? "Purchased" : "Buy Expansion";
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
