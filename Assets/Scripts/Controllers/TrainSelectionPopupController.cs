using UnityEngine;
using UnityEngine.UI;

public class TrainSelectionPopupController : MonoBehaviour
{
    public GameObject trainIconButtonPrefab;
    public Transform contentContainer;
    public Button closeButton;

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    void OnEnable()
    {
        LoadTrains();
    }

    public void LoadTrains()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        if (TrainManager.Instance == null) return;

        foreach (Train train in TrainManager.Instance.unlockedTrains)
        {
            GameObject newBtn = Instantiate(trainIconButtonPrefab, contentContainer);
            
            Image iconImage = newBtn.transform.GetChild(0).GetComponent<Image>();
            if (iconImage == null)
            {
                Transform iconTransform = newBtn.transform.Find("Icon");
                if (iconTransform != null) iconImage = iconTransform.GetChild(0).GetComponent<Image>();
            }

            if (iconImage != null) iconImage.sprite = train.icon;

            Button btnComp = newBtn.GetComponent<Button>();
            if (btnComp != null)
            {
                btnComp.onClick.AddListener(() => OnTrainSelected(train));
            }
        }
    }

    private void OnTrainSelected(Train train)
    {
        if (TrainManager.Instance.pendingPlatform != null && TrainManager.Instance.pendingSlot != -1)
        {
            TrainManager.Instance.AssignTrainToPlatformSlot(train, TrainManager.Instance.pendingPlatform, TrainManager.Instance.pendingSlot);
            TrainManager.Instance.pendingPlatform = null;
            TrainManager.Instance.pendingSlot = -1;
        }

        UIController.Instance.CloseTrainSelectionPopup();
    }

    public void ClosePopup()
    {
        TrainManager.Instance.pendingPlatform = null;
        TrainManager.Instance.pendingSlot = -1;
        UIController.Instance.CloseTrainSelectionPopup();
    }
}