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
    public GameObject decorationModeUI;

    private ObjectBuildable selectedBuildItem;
    private GameObject configuredDemolishModeUi;
    private GameObject configuredDecorationModeUi;
    
    void Start()
    {
        LoadItems();
        EnsureModeUiReferences();
        SyncModeUi();
    }

    void OnEnable()
    {
        ProgressionManager.OnProgressionChanged += LoadItems;
        BuildController.OnBuildablesChanged += HandleBuildablesChanged;
        LoadItems();
        EnsureModeUiReferences();
        SyncModeUi();
        if (selectedBuildItem != null)
        {
            UpdateDetailView(selectedBuildItem);
        }
    }

    void OnDisable()
    {
        ProgressionManager.OnProgressionChanged -= LoadItems;
        BuildController.OnBuildablesChanged -= HandleBuildablesChanged;
    }

    void Update()
    {
        SyncModeUi();
    }

    public void LoadItems()
    {
        UIRuntimeListUtility.ClearChildren(contentContainer);
        
        buildItems = Resources.LoadAll<ObjectBuildable>(targetFolderPath);
        
        if (buildItems.Length == 0)
        {
            UIRuntimeListUtility.RefreshLayout(contentContainer);
            return;
        }

        System.Array.Sort(buildItems, (a, b) => BuildController.GetBuildCost(a).CompareTo(BuildController.GetBuildCost(b)));

        foreach (var item in buildItems)
        {
            if (ProgressionManager.Instance != null && !ProgressionManager.Instance.IsUnlocked(item))
            {
                continue;
            }

            CreateButton(item);
        }

        UIRuntimeListUtility.RefreshLayout(contentContainer);
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
        selectedBuildItem = data;
        BuildController.Instance.ChangePreviewObject(data);
        UpdateDetailView(data);
    }
    
    private void UpdateDetailView(ObjectBuildable data)
    {
        if (detailIcon) detailIcon.sprite = data.GetIcon();
        if (detailName) detailName.text = data.objectName;
        if (detailDescription) detailDescription.text = data.description;
        if (detailCost) detailCost.text = "$" + BuildController.GetBuildCost(data);
    }

    private void HandleBuildablesChanged()
    {
        LoadItems();

        if (selectedBuildItem != null)
        {
            UpdateDetailView(selectedBuildItem);
        }
    }

    public void ActivateDemolishMode()
    {
        if (BuildController.Instance == null)
        {
            return;
        }

        if (BuildController.Instance.isDemolishMode)
        {
            BuildController.Instance.isDemolishMode = false;
            SyncModeUi();
            return;
        }

        BuildController.Instance.isBuildingMode = false;
        BuildController.Instance.isDemolishMode = true;
        SyncModeUi();
    }

    public void ToggleDecorationOverlay()
    {
        if (BuildController.Instance == null)
        {
            return;
        }

        BuildController.Instance.ToggleDecorationOverlay();
        SyncModeUi();
    }

    private void SyncModeUi()
    {
        EnsureModeUiReferences();

        if (BuildController.Instance == null)
        {
            return;
        }

        SetModeUiActive(demolishModeUI, BuildController.Instance.isDemolishMode);
        SetModeUiActive(decorationModeUI, BuildController.Instance.IsDecorationOverlayEnabled);
    }

    private void EnsureModeUiReferences()
    {
        if (demolishModeUI == null)
        {
            demolishModeUI = FindNamedUiObject("DemolishUI");
        }

        if (decorationModeUI == null)
        {
            decorationModeUI = FindNamedUiObject("DecorationUI");
        }

        if (demolishModeUI != configuredDemolishModeUi)
        {
            PointerUiUtility.DisableRaycastTargets(demolishModeUI);
            configuredDemolishModeUi = demolishModeUI;
        }

        if (decorationModeUI != configuredDecorationModeUi)
        {
            PointerUiUtility.DisableRaycastTargets(decorationModeUI);
            configuredDecorationModeUi = decorationModeUI;
        }
    }

    private GameObject FindNamedUiObject(string objectName)
    {
        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i] != null && childTransforms[i].name == objectName)
            {
                return childTransforms[i].gameObject;
            }
        }

        GameObject namedObject = GameObject.Find(objectName);
        return namedObject;
    }

    private void SetModeUiActive(GameObject targetUi, bool isActive)
    {
        if (targetUi != null && targetUi.activeSelf != isActive)
        {
            targetUi.SetActive(isActive);
        }
    }
}
