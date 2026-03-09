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

    void OnEnable()
    {
        UIController.OnDetailsViewUpdate += CheckButtonInteractabilities;
        if (selectedTrain != null) UpdateDetailView(selectedTrain);
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
        
        trains = Resources.LoadAll<Train>(targetFolderPath);
        
        if (trains.Length == 0) return;

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
        selectedTrain = data;
        UpdateDetailView(data);
    }

    public void OnBuyServiceButtonClicked()
    {
        if (!TrainManager.Instance.unlockedTrains.Contains(selectedTrain))
        {
            if (EconomyManager.Instance.money >= selectedTrain.upfrontCost)
            {
                EconomyManager.Instance.SpendMoney(selectedTrain.upfrontCost);
                TrainManager.Instance.UnlockTrain(selectedTrain);
            }
        }

        UpdateDetailView(selectedTrain);
    }

    public void OnEndServiceButtonClicked()
    {
        TrainManager.Instance.RemoveTrainFromService(selectedTrain);
        UpdateDetailView(selectedTrain);
    }
    
    void CheckButtonInteractabilities()
    {
        if (selectedTrain != null)
        {
            checkBuyServiceButtonInteractability(selectedTrain);
            checkEndServiceButtonInteractability(selectedTrain);
        }
    }

    void checkBuyServiceButtonInteractability(Train data)
    {
        bool isUnlocked = TrainManager.Instance.unlockedTrains.Contains(data);
        bool canAfford = EconomyManager.Instance.money >= data.upfrontCost;

        if (isUnlocked)
        {
            buyServiceButton.interactable = false;
        }
        else
        {
            buyServiceButton.interactable = canAfford;
        }
    }
    
    void checkEndServiceButtonInteractability(Train data)
    {
        bool canEnd = TrainManager.Instance.activeTrainServices.Exists(s => s.trainData == data);
        endServiceButton.interactable = canEnd;
    }
    
    private void UpdateDetailView(Train data)
    {
        if (detailIcon) detailIcon.sprite = data.icon;
        if (detailName) detailName.text = data.name;
        if (detailDescription) detailDescription.text = data.description;
        if (costPerMinute) costPerMinute.text = $"${data.costPerMinute}/min";

        TextMeshProUGUI buyText = buyServiceButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buyText != null)
        {
            if (TrainManager.Instance.unlockedTrains.Contains(data))
            {
                buyText.text = "Purchased";
                if (upfrontServiceCost) upfrontServiceCost.text = "Unlocked";
            }
            else
            {
                buyText.text = "Buy Train";
                if (upfrontServiceCost) upfrontServiceCost.text = $"Buy ${data.upfrontCost}";
            }
        }

        checkBuyServiceButtonInteractability(data);
        checkEndServiceButtonInteractability(data);
    }
}