using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TrainMenuController : MonoBehaviour
{
    public string targetFolderPath = "Trains";
    public GameObject TrainItemButtonPrefab;
    public Transform contentContainer;
    [SerializeField] private ScrollRect trainScrollRect;

    public Train[] trains;
    
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public TextMeshProUGUI upfrontServiceCost;
    public TextMeshProUGUI costPerMinute;

    public Button buyServiceButton;
    public Button endServiceButton;
    
    private Train selectedTrain;
    private Coroutine pendingScrollReset;
    
    void Start()
    {
        LoadItems();
    }

    void OnEnable()
    {
        UIController.OnDetailsViewUpdate += CheckButtonInteractabilities;
        ProgressionManager.OnProgressionChanged += LoadItems;
        LoadItems();
        QueueScrollReset();
        if (selectedTrain != null) UpdateDetailView(selectedTrain);
    }
    
    void OnDisable()
    {
        UIController.OnDetailsViewUpdate -= CheckButtonInteractabilities;
        ProgressionManager.OnProgressionChanged -= LoadItems;
        StopQueuedScrollReset();
        ClearSelectedMenuObject();
    }

    public void LoadItems()
    {
        UIRuntimeListUtility.ClearChildren(contentContainer);
        
        trains = Resources.LoadAll<Train>(targetFolderPath);
        
        if (trains.Length == 0)
        {
            UIRuntimeListUtility.RefreshLayout(contentContainer);
            return;
        }

        System.Array.Sort(trains, CompareTrainMenuOrder);

        foreach (var item in trains)
        {
            if (ProgressionManager.Instance != null && !ProgressionManager.Instance.IsUnlocked(item))
            {
                continue;
            }

            CreateButton(item);
        }

        UIRuntimeListUtility.RefreshLayout(contentContainer);
    }

    private void CreateButton(Train data)
    {
        GameObject newButton = Instantiate(TrainItemButtonPrefab, contentContainer);
        
        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage) iconImage.sprite = data.GetIcon();
        
        Button btnComp = newButton.GetComponent<Button>();
        btnComp.onClick.AddListener(() => OnTrainItemButtonClicked(data));
    }

    private int CompareTrainMenuOrder(Train left, Train right)
    {
        int tierComparison = left.requiredTier.CompareTo(right.requiredTier);
        if (tierComparison != 0)
        {
            return tierComparison;
        }

        return left.upfrontCost.CompareTo(right.upfrontCost);
    }
    
    private void OnTrainItemButtonClicked(Train data)
    {
        selectedTrain = data;
        UpdateDetailView(data);
    }

    public void OnBuyServiceButtonClicked()
    {
        if (selectedTrain == null || TrainManager.Instance == null)
        {
            return;
        }

        bool wasUnlocked = TrainManager.Instance.unlockedTrains.Contains(selectedTrain);
        if (!wasUnlocked && EconomyManager.Instance != null && EconomyManager.Instance.money >= selectedTrain.upfrontCost)
        {
            EconomyManager.Instance.SpendMoney(selectedTrain.upfrontCost);
            TrainManager.Instance.UnlockTrain(selectedTrain);
            TrainManager.Instance.TryAssignTrainToFirstAvailablePlatformSlot(selectedTrain);
        }

        UpdateDetailView(selectedTrain);
    }

    public void OnEndServiceButtonClicked()
    {
        if (selectedTrain == null || TrainManager.Instance == null)
        {
            return;
        }

        if (!TrainManager.Instance.unlockedTrains.Contains(selectedTrain))
        {
            UpdateDetailView(selectedTrain);
            return;
        }

        TrainService activeService = TrainManager.Instance.GetServiceByTrain(selectedTrain);
        if (activeService != null)
        {
            TrainManager.Instance.RemoveTrainFromService(selectedTrain);
        }

        TrainManager.Instance.unlockedTrains.Remove(selectedTrain);
        TrainManager.Instance.NotifyTrainAssignmentsChanged();

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(selectedTrain.upfrontCost, false);
        }

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
        bool canEnd = TrainManager.Instance.unlockedTrains.Contains(data);
        endServiceButton.interactable = canEnd;
    }
    
    private void UpdateDetailView(Train data)
    {
        if (detailIcon) detailIcon.sprite = data.GetIcon();
        if (detailName) detailName.text = data.trainName;
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

    private void QueueScrollReset()
    {
        StopQueuedScrollReset();
        pendingScrollReset = StartCoroutine(ResetScrollStateNextFrame());
    }

    private void StopQueuedScrollReset()
    {
        if (pendingScrollReset == null)
        {
            return;
        }

        StopCoroutine(pendingScrollReset);
        pendingScrollReset = null;
    }

    private IEnumerator ResetScrollStateNextFrame()
    {
        yield return null;
        pendingScrollReset = null;

        UIRuntimeListUtility.RefreshLayout(contentContainer);
        ClearSelectedMenuObject();

        ScrollRect scrollRect = ResolveScrollRect();
        if (scrollRect == null)
        {
            yield break;
        }

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;

        // Toggling the component forces Unity to discard stale drag state after the panel is reopened.
        scrollRect.enabled = false;
        scrollRect.enabled = true;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private ScrollRect ResolveScrollRect()
    {
        if (trainScrollRect == null && contentContainer != null)
        {
            trainScrollRect = contentContainer.GetComponentInParent<ScrollRect>(true);
        }

        return trainScrollRect;
    }

    private void ClearSelectedMenuObject()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null && selectedObject.transform.IsChildOf(transform))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
