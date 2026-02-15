using System.Collections.Generic;
using UnityEngine;

public class StaffManager : MonoBehaviour
{
    public static StaffManager Instance;
    
    public List<Staff> hiredStaff = new List<Staff>();
    
    private void Awake()
    {
        Instance = this;
    }

    public void PaySalary(Staff staff)
    {
        if (hiredStaff.Contains(staff))
        {
            EconomyManager.Instance.SpendMoney(staff.salaryPerMinute);
            /*WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                $"-${staff.salaryPerMinute}", 
                staff.transform.position + Vector3.up * 7f, 
                Color.red);*/
        }
    }
    
    public void HireStaff(Staff staff)
    {
        if (!hiredStaff.Contains(staff))
        {
            hiredStaff.Add(staff);
        }
    }
    
    public int GetHiredStaffAmount(StaffMember type)
    {
        int count = 0;
        foreach (Staff staff in hiredStaff)
        {
            if (staff.staffType == type)
            {
                count++;
            }
        }
        return count;
    }
    
    public void FireStaffMember(StaffMember type)
    {
        for (int i = hiredStaff.Count - 1; i >= 0; i--)
        {
            if (hiredStaff[i].staffType == type)
            {
                Staff staffToFire = hiredStaff[i];
                hiredStaff.RemoveAt(i);
                Destroy(staffToFire.gameObject);
                break;
            }
        }
    }
}
