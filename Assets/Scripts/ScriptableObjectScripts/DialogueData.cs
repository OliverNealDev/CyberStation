using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Game Data/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    // Person Dialogue Lines
    [Header("Low Comfort Lines")]
    [TextArea(2, 5)]
    public List<string> lowComfort = new List<string>();
    
    [Header("Low Satiation Lines")]
    [TextArea(2, 5)]
    public List<string> lowSatiation = new List<string>();
    
    [Header("Low Hydration Lines")]
    [TextArea(2, 5)]
    public List<string> lowHydration = new List<string>();
    
    [Header("Low Hygeine Lines")]
    [TextArea(2, 5)]
    public List<string> lowHygeine = new List<string>();
    //
    
    // Passenger Dialogue Lines
    [Header("Caught By Security Lines")]
    [TextArea(2, 5)]
    public List<string> caughtBySecurity = new List<string>();
    
    // Security Dialogue Lines
    [Header("Catching Evader Lines")]
    [TextArea(2, 5)]
    public List<string> caughtEvader = new List<string>();
    
    
    
    public string GetRandomLine(DialogueType type)
    {
        List<string> targetList = type switch
        {
            DialogueType.CaughtBySecurity => caughtBySecurity,
            DialogueType.LowComfort => lowComfort,
            DialogueType.LowSatiation => lowSatiation,
            DialogueType.LowHydration => lowHydration,
            DialogueType.LowHygeine => lowHygeine,
            DialogueType.CaughtEvader => caughtEvader,
            _ => null
        };

        if (targetList == null || targetList.Count == 0) return "...";
        return targetList[Random.Range(0, targetList.Count)];
    }
}

public enum DialogueType
{
    CaughtBySecurity,
    LowComfort,
    LowSatiation,
    LowHydration,
    LowHygeine,
    CaughtEvader
}