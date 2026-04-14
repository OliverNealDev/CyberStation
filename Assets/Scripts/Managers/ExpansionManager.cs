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
        if (!CanBuyExpansion(expansion)) return false;

        EconomyManager.Instance.SpendMoney(expansion.upfrontCost);
        builtExpansions.Add(expansion);
        
        if (expansion.expansionPrefab != null)
        {
            Instantiate(expansion.expansionPrefab, levelParent);
            NavMeshManager.Instance.BuildNavMesh();
        }

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.RecordExpansionBuilt();
        }

        OnExpansionBuilt?.Invoke();
        return true;
    }

    public bool IsExpansionBuilt(Expansion expansion)
    {
        return builtExpansions.Contains(expansion);
    }

    public bool CanBuyExpansion(Expansion expansion)
    {
        if (expansion == null)
        {
            return false;
        }

        if (builtExpansions.Contains(expansion))
        {
            return false;
        }

        if (ProgressionManager.Instance != null && !ProgressionManager.Instance.IsUnlocked(expansion))
        {
            return false;
        }

        if (TryGetMissingPlatformRequirement(expansion, out _))
        {
            return false;
        }

        return EconomyManager.Instance != null && EconomyManager.Instance.money >= expansion.upfrontCost;
    }

    public bool TryGetMissingPlatformRequirement(Expansion expansion, out Expansion requiredExpansion)
    {
        requiredExpansion = GetRequiredPreviousPlatformExpansion(expansion);
        return requiredExpansion != null && !builtExpansions.Contains(requiredExpansion);
    }

    private Expansion GetRequiredPreviousPlatformExpansion(Expansion expansion)
    {
        if (expansion == null || expansion.platformNumber <= 2 || allExpansions == null)
        {
            return null;
        }

        int requiredPlatformNumber = expansion.platformNumber - 1;
        for (int i = 0; i < allExpansions.Length; i++)
        {
            Expansion candidate = allExpansions[i];
            if (candidate != null && candidate.platformNumber == requiredPlatformNumber)
            {
                return candidate;
            }
        }

        return null;
    }
}
