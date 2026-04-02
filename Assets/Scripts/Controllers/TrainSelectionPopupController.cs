using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainSelectionPopupController : MonoBehaviour
{
    public GameObject trainIconButtonPrefab;
    public Transform contentContainer;
    public Button closeButton;

    private const string UnassignLabel = "Unassign";

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
        UIRuntimeListUtility.ClearChildren(contentContainer);

        if (TrainManager.Instance == null)
        {
            UIRuntimeListUtility.RefreshLayout(contentContainer);
            return;
        }

        CreateClearSlotButton();

        foreach (Train train in TrainManager.Instance.unlockedTrains)
        {
            CreateTrainButton(train);
        }

        UIRuntimeListUtility.RefreshLayout(contentContainer);
    }

    private void CreateClearSlotButton()
    {
        GameObject newBtn = Instantiate(trainIconButtonPrefab, contentContainer);
        Image buttonImage = newBtn.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.22f, 0.22f, 0.22f, 1f);
        }

        Image iconImage = GetButtonIconImage(newBtn);
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        TextMeshProUGUI label = GetOrCreateLabel(newBtn.transform);
        if (label != null)
        {
            label.text = UnassignLabel;
        }

        Button btnComp = newBtn.GetComponent<Button>();
        if (btnComp != null)
        {
            btnComp.onClick.AddListener(() => OnTrainSelected(null));
        }
    }

    private void CreateTrainButton(Train train)
    {
        GameObject newBtn = Instantiate(trainIconButtonPrefab, contentContainer);

        Image iconImage = GetButtonIconImage(newBtn);
        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = train.GetIcon();
        }

        Button btnComp = newBtn.GetComponent<Button>();
        if (btnComp != null)
        {
            btnComp.onClick.AddListener(() => OnTrainSelected(train));
        }
    }

    private Image GetButtonIconImage(GameObject buttonObject)
    {
        if (buttonObject == null)
        {
            return null;
        }

        Image iconImage = null;
        if (buttonObject.transform.childCount > 0)
        {
            iconImage = buttonObject.transform.GetChild(0).GetComponent<Image>();
        }

        if (iconImage == null)
        {
            Transform iconTransform = buttonObject.transform.Find("Icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();

                if (iconImage == null && iconTransform.childCount > 0)
                {
                    iconImage = iconTransform.GetChild(0).GetComponent<Image>();
                }
            }
        }

        return iconImage;
    }

    private TextMeshProUGUI GetOrCreateLabel(Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        TextMeshProUGUI label = parent.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        if (fontAsset != null)
        {
            label.font = fontAsset;
        }

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 10f);
        labelRect.offsetMax = new Vector2(-10f, -10f);

        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 28f;
        label.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        return label;
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
