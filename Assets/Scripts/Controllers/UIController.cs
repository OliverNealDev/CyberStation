using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Button References")]
    public Button settingsButton;
    public Button cameraSwitchButton;
    public Button trainMenuButton;
    public Button buildMenuButton;
    public Button staffMenuButton;
    public Button manageStationButton;

    [Header("Panel References")]
    public GameObject settingsPanel;
    public GameObject trainPanel;
    public GameObject buildPanel;
    public GameObject staffPanel;
    public GameObject managePanel;

    [Header("System References")]
    [SerializeField] private BuildController buildController;

    private GameObject currentActivePanel;

    void Start()
    {
        CloseAllPanels();

        if (settingsButton) settingsButton.onClick.AddListener(() => TogglePanel(settingsPanel));
        if (trainMenuButton) trainMenuButton.onClick.AddListener(() => TogglePanel(trainPanel));
        if (staffMenuButton) staffMenuButton.onClick.AddListener(() => TogglePanel(staffPanel));
        if (manageStationButton) manageStationButton.onClick.AddListener(() => TogglePanel(managePanel));

        if (cameraSwitchButton) cameraSwitchButton.onClick.AddListener(OnCameraSwitchClicked);

        if (buildMenuButton) buildMenuButton.onClick.AddListener(OnBuildMenuClicked);
    }

    private void TogglePanel(GameObject panelToToggle)
    {
        if (currentActivePanel == panelToToggle)
        {
            CloseAllPanels();
            return;
        }

        if (currentActivePanel != null)
        {
            currentActivePanel.SetActive(false);
        }

        if (panelToToggle != null)
        {
            panelToToggle.SetActive(true);
            currentActivePanel = panelToToggle;
        }

        if (buildController != null)
        {
            buildController.isBuildingMode = false;
        }
    }

    private void OnBuildMenuClicked()
    {
        if (currentActivePanel == buildPanel)
        {
            CloseAllPanels();
        }
        else
        {
            if (currentActivePanel != null) currentActivePanel.SetActive(false);

            buildPanel.SetActive(true);
            currentActivePanel = buildPanel;

            if (buildController != null) 
            {
                buildController.isBuildingMode = true;
            }
        }
    }

    private void OnCameraSwitchClicked()
    {
        Debug.Log("Switched Camera View");
    }

    public void CloseAllPanels()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (trainPanel) trainPanel.SetActive(false);
        if (buildPanel) buildPanel.SetActive(false);
        if (staffPanel) staffPanel.SetActive(false);
        if (managePanel) managePanel.SetActive(false);

        currentActivePanel = null;

        if (buildController != null) 
        {
            buildController.isBuildingMode = false;
        }
    }

    private void OnDestroy()
    {
        if (settingsButton) settingsButton.onClick.RemoveAllListeners();
        if (cameraSwitchButton) cameraSwitchButton.onClick.RemoveAllListeners();
        if (trainMenuButton) trainMenuButton.onClick.RemoveAllListeners();
        if (buildMenuButton) buildMenuButton.onClick.RemoveAllListeners();
        if (staffMenuButton) staffMenuButton.onClick.RemoveAllListeners();
        if (manageStationButton) manageStationButton.onClick.RemoveAllListeners();
    }
}