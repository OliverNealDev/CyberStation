using UnityEngine;

[CreateAssetMenu(fileName = "Expansion", menuName = "Scriptable Objects/Expansion")]
public class Expansion : ScriptableObject
{
    public string name = "New Expansion";
    [TextArea]
    public string description = "Description of the expansion.";
    public Sprite icon;
    
    public GameObject expansionPrefab;

    [Header("Economy")]
    public int upfrontCost = 100;
}
