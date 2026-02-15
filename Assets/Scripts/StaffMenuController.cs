using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StaffMenuController : MonoBehaviour
{
    public string targetFolderPath = "Staff";
    public GameObject StaffButtonPrefab;
    public Transform contentContainer;

    public StaffMember[] staff;
    
    // Detail View References
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

    public void LoadItems()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
        
        staff = Resources.LoadAll<StaffMember>(targetFolderPath);
        
        // Debug check to help you verify if the path is correct
        if (staff.Length == 0)
        {
            Debug.LogError($"No Staff found in Resources/{targetFolderPath}. Check folder name and file types!");
            return;
        }

        foreach (var staffMember in staff)
        {
            CreateButton(staffMember);
        }
    }

    private void CreateButton(StaffMember data)
    {
        GameObject newButton = Instantiate(StaffButtonPrefab, contentContainer);
        
        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage) iconImage.sprite = data.icon;
        
        TextMeshProUGUI nameText = newButton.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        if (nameText) nameText.text = data.name;
        
        TextMeshProUGUI amountHiredText = newButton.transform.Find("AmountHired").GetComponent<TextMeshProUGUI>();
        if (amountHiredText) amountHiredText.text = "x" + StaffManager.Instance.GetHiredStaffAmount(data);
        
        Button btnComp = newButton.GetComponent<Button>();
        btnComp.onClick.AddListener(() => OnStaffButtonClicked(data));
    }
    
    private void OnStaffButtonClicked(StaffMember data)
    {
        Debug.Log("Clicked on staff member: " + data.name);

        selectedStaff = data;
        UpdateDetailView(data);
        
        checkFireButtonInteractability(data);
    }

    private void checkFireButtonInteractability(StaffMember data)
    {
        bool canFire = StaffManager.Instance.GetHiredStaffAmount(data) > 0;
        fireStaffButton.interactable = canFire;
    }

    public void OnHireStaffButtonClicked()
    {
        if (EconomyManager.Instance.money >= selectedStaff.hiringCost)
        {
            EconomyManager.Instance.SpendMoney(selectedStaff.hiringCost);
            Staff newStaffMember = Instantiate(selectedStaff.staffPrefab, GameObject.FindGameObjectWithTag("PassengerSpawnPoint").transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0, 0), Quaternion.identity).GetComponent<Staff>();
            newStaffMember.salaryPerMinute = selectedStaff.salaryPerMinute;
            newStaffMember.staffType = selectedStaff;
            StaffManager.Instance.HireStaff(newStaffMember);
            
            checkFireButtonInteractability(selectedStaff);
            
            LoadItems();
        }
        else
        {
            Debug.Log("Not enough money to hire " + selectedStaff.name);
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
    
    private void UpdateDetailView(StaffMember data)
    {
        if (detailIcon) detailIcon.sprite = data.icon;
        if (detailName) detailName.text = data.name;
        if (detailDescription) detailDescription.text = data.description;
        if (hiringCostText) hiringCostText.text = "Hire $" + data.hiringCost;
        if (costPerMinuteText) costPerMinuteText.text = "$" + data.salaryPerMinute + "/min";
    }
}
