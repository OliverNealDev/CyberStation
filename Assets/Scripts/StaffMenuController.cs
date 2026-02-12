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

    public Button hireStaffButton;
    
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
        
        Button btnComp = newButton.GetComponent<Button>();
        btnComp.onClick.AddListener(() => OnStaffButtonClicked(data));
    }
    
    private void OnStaffButtonClicked(StaffMember data)
    {
        Debug.Log("Clicked on staff member: " + data.name);

        selectedStaff = data;
        UpdateDetailView(data);
    }

    public void OnHireStaffButtonClicked()
    {
        if (EconomyManager.Instance.money >= selectedStaff.hiringCost)
        {
            EconomyManager.Instance.SpendMoney(selectedStaff.hiringCost);
            GameObject newStaffMember = Instantiate(selectedStaff.staffPrefab, GameObject.FindGameObjectWithTag("PassengerSpawnPoint").transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0, 0), Quaternion.identity);
        }
        else
        {
            Debug.Log("Not enough money to hire " + selectedStaff.name);
        }
    }
    
    private void UpdateDetailView(StaffMember data)
    {
        if (detailIcon) detailIcon.sprite = data.icon;
        if (detailName) detailName.text = data.name;
        if (detailDescription) detailDescription.text = data.description;
    }
}
