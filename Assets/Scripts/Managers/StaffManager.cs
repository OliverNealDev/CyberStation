using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StaffManager : MonoBehaviour
{
    public static StaffManager Instance;
    
    public List<Staff> hiredStaff = new List<Staff>();
    
    private void Awake()
    {
        Instance = this;
    }

    public void HireStaff(Staff staff)
    {
        if (staff == null) return;
        if (PassengerManager.Instance == null || !PassengerManager.Instance.HasMaterializer())
        {
            Destroy(staff.gameObject);
            return;
        }

        AddStaff(staff, true, true);
    }

    public bool RestoreHiredStaff(StaffMember staffType, Vector3 position, Quaternion rotation)
    {
        if (staffType == null || staffType.staffPrefab == null)
        {
            return false;
        }

        GameObject staffObject = Instantiate(staffType.staffPrefab, position, rotation);
        Staff restoredStaff = staffObject.GetComponent<Staff>();
        if (restoredStaff == null)
        {
            Destroy(staffObject);
            return false;
        }

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            staffObject.transform.position = hit.position;
        }

        restoredStaff.salaryPerMinute = staffType.salaryPerMinute;
        restoredStaff.staffType = staffType;

        if (restoredStaff.navAgent != null)
        {
            restoredStaff.navAgent.enabled = false;
            restoredStaff.navAgent.enabled = true;
        }

        return AddStaff(restoredStaff, false, false);
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

    private bool AddStaff(Staff staff, bool playSound, bool awardProgression)
    {
        if (staff == null || hiredStaff.Contains(staff))
        {
            return false;
        }

        hiredStaff.Add(staff);

        if (playSound)
        {
            SoundEffectController.Play(SoundEffectId.HireAndroid);
        }

        if (awardProgression && ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.RecordStaffHired();
        }

        return true;
    }
}
