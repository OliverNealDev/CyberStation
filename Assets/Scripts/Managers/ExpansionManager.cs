using System;
using System.Collections.Generic;
using UnityEngine;

public class ExpansionManager : MonoBehaviour
{
    public static ExpansionManager Instance;
    public Transform levelParent;
    
    public Expansion[] allExpansions;
    public List<Expansion> builtExpansions = new List<Expansion>();

    public static event Action OnExpansionBuilt;

    void Awake()
    {
        Instance = this;
        allExpansions = Resources.LoadAll<Expansion>("Expansions");
        System.Array.Sort(allExpansions, (a, b) => a.upfrontCost.CompareTo(b.upfrontCost));
    }
    
    public bool TryBuyExpansion(Expansion expansion)
    {
        if (builtExpansions.Contains(expansion)) return false;
        if (EconomyManager.Instance.money < expansion.upfrontCost) return false;

        EconomyManager.Instance.SpendMoney(expansion.upfrontCost);
        builtExpansions.Add(expansion);
        
        if (expansion.expansionPrefab != null)
        {
            Instantiate(expansion.expansionPrefab, levelParent);
            NavMeshManager.Instance.BuildNavMesh();
        }

        OnExpansionBuilt?.Invoke();
        return true;
    }

    public bool IsExpansionBuilt(Expansion expansion)
    {
        return builtExpansions.Contains(expansion);
    }
}
