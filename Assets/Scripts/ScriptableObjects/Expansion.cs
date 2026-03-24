using UnityEngine;

[CreateAssetMenu(fileName = "Expansion", menuName = "Scriptable Objects/Expansion")]
public class Expansion : ScriptableObject
{
    public string name = "New Expansion";
    public int requiredTier = 1;
    [TextArea]
    public string description = "Description of the expansion.";
    public Sprite icon;
    
    public GameObject expansionPrefab;

    [Header("Economy")]
    public int upfrontCost = 100;

    [System.NonSerialized] private Sprite runtimeIcon;

    public Sprite GetIcon()
    {
        if (runtimeIcon == null)
        {
            runtimeIcon = PrefabIconRenderer.GetIcon(expansionPrefab, icon, PrefabIconView.BuildablesAndStaff);
        }

        return runtimeIcon;
    }
}
