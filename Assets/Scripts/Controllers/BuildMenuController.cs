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
    
    void Start()
    {
        LoadItems();
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

        foreach (var item in buildItems)
        {
            CreateButton(item);
        }
    }

    private void CreateButton(ObjectBuildable data)
    {
        GameObject newButton = Instantiate(ItemButtonPrefab, contentContainer);
        
        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage) iconImage.sprite = data.icon;
        
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
        if (detailIcon) detailIcon.sprite = data.icon;
        if (detailName) detailName.text = data.objectName;
        if (detailDescription) detailDescription.text = data.description;
        if (detailCost) detailCost.text = "$" + data.cost;
    }

    public void ActivateDemolishMode()
    {
        BuildController.Instance.isDemolishMode = true;
    }
}