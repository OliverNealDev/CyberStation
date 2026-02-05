using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TrainMenuController : MonoBehaviour
{
    public string targetFolderPath = "Trains";
    public GameObject TrainItemButtonPrefab;
    public Transform contentContainer;

    public Train[] trains;
    
    // Detail View References
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;

    public Button purchaseTrainButton;
    
    private Train selectedTrain;
    
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
        
        trains = Resources.LoadAll<Train>(targetFolderPath);
        
        // Debug check to help you verify if the path is correct
        if (trains.Length == 0)
        {
            Debug.LogError($"No Train items found in Resources/{targetFolderPath}. Check folder name and file types!");
            return;
        }

        foreach (var item in trains)
        {
            CreateButton(item);
        }
    }

    private void CreateButton(Train data)
    {
        GameObject newButton = Instantiate(TrainItemButtonPrefab, contentContainer);
        
        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage) iconImage.sprite = data.icon;
        
        Button btnComp = newButton.GetComponent<Button>();
        btnComp.onClick.AddListener(() => OnTrainItemButtonClicked(data));
    }
    
    private void OnTrainItemButtonClicked(Train data)
    {
        Debug.Log("Clicked on train item: " + data.name);

        selectedTrain = data;
        UpdateDetailView(data);
    }

    public void OnPurchaseTrainButtonClicked()
    {
        TrainManager.Instance.AddTrainToService(selectedTrain);
    }
    
    private void UpdateDetailView(Train data)
    {
        if (detailIcon) detailIcon.sprite = data.icon;
        if (detailName) detailName.text = data.name;
        if (detailDescription) detailDescription.text = data.description;
    }
}