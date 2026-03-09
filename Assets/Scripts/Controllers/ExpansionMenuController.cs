using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpansionMenuController : MonoBehaviour
{
    public string targetFolderPath = "Expansions";
    public GameObject ExpansionItemButtonPrefab;
    public Transform contentContainer;

    public Expansion[] expansions;
    
    // Detail View References
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public TextMeshProUGUI upfrontServiceCost;

    public Button buyExpansionButton;
    
    private Expansion selectedExpansion;
    
    private List<Expansion> builtExpansions = new List<Expansion>();
    
    void Start()
    {
        LoadItems();
    }

    void OnEnable()
    {
        UIController.OnDetailsViewUpdate += CheckButtonInteractabilities;
    }
    
    void OnDisable()
    {
        UIController.OnDetailsViewUpdate -= CheckButtonInteractabilities;
    }

    public void LoadItems()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
        
        expansions = Resources.LoadAll<Expansion>(targetFolderPath);
        
        // Debug check to help you verify if the path is correct
        if (expansions.Length == 0)
        {
            Debug.LogError($"No Expansion items found in Resources/{targetFolderPath}. Check folder name and file types!");
            return;
        }

        foreach (var item in expansions)
        {
            CreateButton(item);
        }
    }

    private void CreateButton(Expansion data)
    {
        GameObject newButton = Instantiate(ExpansionItemButtonPrefab, contentContainer);
        
        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage) iconImage.sprite = data.icon;
        
        Button btnComp = newButton.GetComponent<Button>();
        btnComp.onClick.AddListener(() => OnExpansionItemButtonClicked(data));
    }
    
    private void OnExpansionItemButtonClicked(Expansion data)
    {
        Debug.Log("Clicked on train item: " + data.name);

        selectedExpansion = data;
        UpdateDetailView(data);
    }

    public void OnBuyExpansionButtonClicked()
    {
        if (!builtExpansions.Contains(selectedExpansion) && 
            EconomyManager.Instance.money >= selectedExpansion.upfrontCost  && 
            selectedExpansion.expansionPrefab != null)
        {
            EconomyManager.Instance.SpendMoney(selectedExpansion.upfrontCost);
            UpdateDetailView(selectedExpansion);
            ExpansionManager.Instance.BuildExpansion(selectedExpansion.expansionPrefab);
            builtExpansions.Add(selectedExpansion);
            CheckButtonInteractabilities();
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
        bool canBuy = EconomyManager.Instance.money >= data.upfrontCost && !builtExpansions.Contains(selectedExpansion);
        buyExpansionButton.interactable = canBuy;
    }
    
    private void UpdateDetailView(Expansion data)
    {
        if (detailIcon) detailIcon.sprite = data.icon;
        if (detailName) detailName.text = data.name;
        if (detailDescription) detailDescription.text = data.description;
        if (upfrontServiceCost) upfrontServiceCost.text = $"Buy ${data.upfrontCost}";

        checkBuyExpansionButtonInteractability(data);
    }
}
