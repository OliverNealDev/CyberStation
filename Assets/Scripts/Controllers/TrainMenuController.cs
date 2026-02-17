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
    public TextMeshProUGUI upfrontServiceCost;
    public TextMeshProUGUI costPerMinute;

    public Button buyServiceButton;
    public Button endServiceButton;
    
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

    public void OnBuyServiceButtonClicked()
    {
        if (EconomyManager.Instance.money >= selectedTrain.upfrontCost)
        {
            EconomyManager.Instance.SpendMoney(selectedTrain.upfrontCost);
            TrainManager.Instance.AddTrainToService(selectedTrain);
            UpdateDetailView(selectedTrain);
        }
    }

    public void OnEndServiceButtonClicked()
    {
        TrainManager.Instance.RemoveTrainFromService(selectedTrain);
        UpdateDetailView(selectedTrain);
    }
    
    private void UpdateDetailView(Train data)
    {
        if (detailIcon) detailIcon.sprite = data.icon;
        if (detailName) detailName.text = data.name;
        if (detailDescription) detailDescription.text = data.description;
        if (upfrontServiceCost) upfrontServiceCost.text = $"Buy ${data.upfrontCost}";
        if (costPerMinute) costPerMinute.text = $"${data.costPerMinute}/min";
        
        buyServiceButton.interactable = !TrainManager.Instance.activeTrainServices.Exists(s => s.trainData == data);
        endServiceButton.interactable = TrainManager.Instance.activeTrainServices.Exists(s => s.trainData == data);
    }
}