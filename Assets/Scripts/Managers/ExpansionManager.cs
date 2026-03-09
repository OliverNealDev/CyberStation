using UnityEngine;
using UnityEngine.AI;

public class ExpansionManager : MonoBehaviour // Script is badly coded, improvements can be made
{
    public static ExpansionManager Instance;
    public Transform LevelParent; // Parent object for all expansions, helps with organization in the hierarchy

    void Awake()
    {
        Instance = this;
    }
    
    public void BuildExpansion(GameObject expansionPrefab)
    {
        Instantiate(expansionPrefab, LevelParent);
        NavMeshManager.Instance.BuildNavMesh();
    }
}
