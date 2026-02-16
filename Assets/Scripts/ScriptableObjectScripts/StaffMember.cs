using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "StaffMember", menuName = "Scriptable Objects/StaffMember")]
public class StaffMember : ScriptableObject
{
    public string staffName = "New StaffMember";
    [TextArea]
    public string description = "Description of the staff.";
    public Sprite icon;
    
    public GameObject staffPrefab;

    public int hiringCost;
    public int salaryPerMinute;
}
