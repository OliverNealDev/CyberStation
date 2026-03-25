using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaffMenuController : MonoBehaviour
{
    public string targetFolderPath = "Staff";
    public GameObject StaffButtonPrefab;
    public Transform contentContainer;

    public StaffMember[] staff;
    
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public TextMeshProUGUI hiringCostText;
    public TextMeshProUGUI costPerMinuteText;

    public Button hireStaffButton;
    public Button fireStaffButton;
    
    private StaffMember selectedStaff;
    
    void Start()
    {
        LoadItems();
    }

    void OnEnable()
    {
        UIController.OnDetailsViewUpdate += CheckButtonInteractabilities;
        ProgressionManager.OnProgressionChanged += LoadItems;
        LoadItems();
    }
    
    void OnDisable()
    {
        UIController.OnDetailsViewUpdate -= CheckButtonInteractabilities;
        ProgressionManager.OnProgressionChanged -= LoadItems;
    }

    public void LoadItems()
    {
        UIRuntimeListUtility.ClearChildren(contentContainer);
        
        staff = Resources.LoadAll<StaffMember>(targetFolderPath);
        
        if (staff.Length == 0)
        {
            UIRuntimeListUtility.RefreshLayout(contentContainer);
            return;
        }

        System.Array.Sort(staff, (a, b) => a.hiringCost.CompareTo(b.hiringCost));

        foreach (var staffMember in staff)
        {
            if (ProgressionManager.Instance != null && !ProgressionManager.Instance.IsUnlocked(staffMember))
            {
                continue;
            }

            CreateButton(staffMember);
        }

        UIRuntimeListUtility.RefreshLayout(contentContainer);
    }

    private void CreateButton(StaffMember data)
    {
        GameObject newButton = Instantiate(StaffButtonPrefab, contentContainer);
        Sprite icon = data.GetIcon();
        
        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage) iconImage.sprite = icon;
        
        TextMeshProUGUI nameText = newButton.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        if (nameText) nameText.text = data.staffName;
        
        TextMeshProUGUI amountHiredText = newButton.transform.Find("AmountHired").GetComponent<TextMeshProUGUI>();
        if (amountHiredText) amountHiredText.text = "x" + StaffManager.Instance.GetHiredStaffAmount(data);
        
        Button btnComp = newButton.GetComponent<Button>();
        btnComp.onClick.AddListener(() => OnStaffButtonClicked(data));
    }
    
    private void OnStaffButtonClicked(StaffMember data)
    {
        selectedStaff = data;
        UpdateDetailView(data);
    }

    private void checkFireButtonInteractability(StaffMember data)
    {
        bool canFire = StaffManager.Instance.GetHiredStaffAmount(data) > 0;
        fireStaffButton.interactable = canFire;
    }

    private void checkHireButtonInteractability(StaffMember data)
    {
        bool hasMaterializer = PassengerManager.Instance != null && PassengerManager.Instance.HasMaterializer();
        bool canHire = hasMaterializer && EconomyManager.Instance.money >= data.hiringCost;
        hireStaffButton.interactable = canHire;
    }

    public void OnHireStaffButtonClicked()
    {
        bool hasMaterializer = PassengerManager.Instance != null && PassengerManager.Instance.HasMaterializer();
        if (hasMaterializer && EconomyManager.Instance.money >= selectedStaff.hiringCost)
        {
            EconomyManager.Instance.SpendMoney(selectedStaff.hiringCost);
            
            Vector3 spawnPosition = Vector3.zero;
            if (PassengerManager.Instance != null)
            {
                spawnPosition = PassengerManager.Instance.GetRandomSpawnPoint();
            }

            Staff newStaffMember = Instantiate(selectedStaff.staffPrefab, spawnPosition, Quaternion.identity).GetComponent<Staff>();
            newStaffMember.salaryPerMinute = selectedStaff.salaryPerMinute;
            newStaffMember.staffType = selectedStaff;
            
            if (newStaffMember.navAgent != null) newStaffMember.navAgent.enabled = false;

            MaterializeAnimator animator = newStaffMember.GetComponent<MaterializeAnimator>();
            if (animator != null)
            {
                animator.Materialize(() => {
                    if (newStaffMember != null && newStaffMember.navAgent != null) newStaffMember.navAgent.enabled = true;
                });
            }
            else
            {
                if (newStaffMember.navAgent != null) newStaffMember.navAgent.enabled = true;
            }

            StaffManager.Instance.HireStaff(newStaffMember);
            
            checkFireButtonInteractability(selectedStaff);
            
            LoadItems();
        }
    }

    public void OnFireStaffButtonClicked()
    {
        if (StaffManager.Instance.GetHiredStaffAmount(selectedStaff) > 0)
        {
            StaffManager.Instance.FireStaffMember(selectedStaff);
            LoadItems();
        }
    }
    
    void CheckButtonInteractabilities()
    {
        if (selectedStaff != null)
        {
            checkFireButtonInteractability(selectedStaff);
            checkHireButtonInteractability(selectedStaff);
        }
    }
    
    private void UpdateDetailView(StaffMember data)
    {
        if (detailIcon) detailIcon.sprite = data.GetIcon();
        if (detailName) detailName.text = data.staffName;
        if (detailDescription) detailDescription.text = data.description;
        if (hiringCostText) hiringCostText.text = "Hire $" + data.hiringCost;
        if (costPerMinuteText) costPerMinuteText.text = "$" + data.salaryPerMinute + "/min";
        
        checkFireButtonInteractability(data);
        checkHireButtonInteractability(data);
    }
}
