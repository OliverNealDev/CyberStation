using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuController : MonoBehaviour
{
    public string targetFolderPath = "BuildItems";
    public GameObject ItemButtonPrefab;
    public Transform contentContainer;

    public ObjectBuildable[] buildItems;
    
    // Detail View References
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    
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
        
        // Debug check to help you verify if the path is correct
        if (buildItems.Length == 0)
        {
            Debug.LogError($"No ObjectBuildable items found in Resources/{targetFolderPath}. Check folder name and file types!");
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
        Debug.Log("Clicked on build item: " + data.objectName);
        
        BuildController.Instance.ChangePreviewObject(data.prefab);
        
        UpdateDetailView(data);
    }
    
    private void UpdateDetailView(ObjectBuildable data)
    {
        if (detailIcon) detailIcon.sprite = data.icon;
        if (detailName) detailName.text = data.objectName;
        if (detailDescription) detailDescription.text = data.description;
    }
}
