using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuController : MonoBehaviour
{
    public string targetFolderPath = "BuildItems";
    public GameObject ItemButtonPrefab;
    public Transform contentContainer;

    public ObjectBuildable[] buildItems;
    
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public TextMeshProUGUI detailCost;
    
    public GameObject demolishModeUI; 
    
    void Start()
    {
        LoadItems();
    }

    void Update()
    {
        if (demolishModeUI != null && BuildController.Instance != null)
        {
            if (demolishModeUI.activeSelf != BuildController.Instance.isDemolishMode)
            {
                demolishModeUI.SetActive(BuildController.Instance.isDemolishMode);
            }
        }
    }

    public void LoadItems()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
        
        buildItems = Resources.LoadAll<ObjectBuildable>(targetFolderPath);
        
        if (buildItems.Length == 0)
        {
            return;
        }

        System.Array.Sort(buildItems, (a, b) => a.cost.CompareTo(b.cost));

        foreach (var item in buildItems)
        {
            CreateButton(item);
        }
    }

    private void CreateButton(ObjectBuildable data)
    {
        GameObject newButton = Instantiate(ItemButtonPrefab, contentContainer);
        Sprite icon = data.GetIcon();
        
        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage) iconImage.sprite = icon;
        
        Button btnComp = newButton.GetComponent<Button>();
        btnComp.onClick.AddListener(() => OnItemButtonClicked(data));
    }
    
    private void OnItemButtonClicked(ObjectBuildable data)
    {
        BuildController.Instance.ChangePreviewObject(data);
        UpdateDetailView(data);
    }
    
    private void UpdateDetailView(ObjectBuildable data)
    {
        if (detailIcon) detailIcon.sprite = data.GetIcon();
        if (detailName) detailName.text = data.objectName;
        if (detailDescription) detailDescription.text = data.description;
        if (detailCost) detailCost.text = "$" + data.cost;
    }

    public void ActivateDemolishMode()
    {
        BuildController.Instance.isDemolishMode = true;
    }
}
