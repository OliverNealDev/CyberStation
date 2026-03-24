using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    public static event Action OnProgressionChanged;

    [Header("XP Curve")]
    public int baseXpPerLevel = 100;
    public int xpIncreasePerLevel = 50;

    [Header("XP Rewards")]
    public int buildPlacedXp = 10;
    public int ticketSoldXp = 5;
    public int passengerBoardedXp = 3;
    public int needFulfilledXp = 4;
    public int trainUnlockedXp = 15;
    public int staffHiredXp = 12;
    public int expansionBuiltXp = 20;

    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXp;

    private readonly Dictionary<UnityEngine.Object, int> unlockTierByObject = new Dictionary<UnityEngine.Object, int>();
    private readonly List<ProgressionTierView> tierViews = new List<ProgressionTierView>();
    private readonly List<UnityEngine.Object> scratchUnlockTargets = new List<UnityEngine.Object>();

    private int maxLevel = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("ProgressionManager");
        managerObject.AddComponent<ProgressionManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        RefreshUnlocksFromScene();
    }

    public int CurrentLevel => currentLevel;
    public int CurrentXp => currentXp;
    public int MaxLevel => maxLevel;
    public bool IsMaxLevel => currentLevel >= maxLevel;

    public int XpIntoCurrentLevel
    {
        get
        {
            if (IsMaxLevel)
            {
                return GetXpRequiredForLevel(currentLevel);
            }

            return currentXp - GetXpRequiredToReachLevel(currentLevel);
        }
    }

    public int XpNeededForNextLevel
    {
        get
        {
            if (IsMaxLevel)
            {
                return GetXpRequiredForLevel(currentLevel);
            }

            return GetXpRequiredForLevel(currentLevel);
        }
    }

    public float LevelProgress01
    {
        get
        {
            if (IsMaxLevel)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)XpIntoCurrentLevel / Mathf.Max(1, XpNeededForNextLevel));
        }
    }

    public void RefreshUnlocksFromScene()
    {
        unlockTierByObject.Clear();
        tierViews.Clear();

        ProgressionTierView[] foundTierViews = FindObjectsByType<ProgressionTierView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (foundTierViews == null || foundTierViews.Length == 0)
        {
            maxLevel = 1;
            UpdateLevelFromXp();
            NotifyProgressChanged();
            return;
        }

        Array.Sort(foundTierViews, CompareTierViews);

        for (int i = 0; i < foundTierViews.Length; i++)
        {
            ProgressionTierView tierView = foundTierViews[i];
            if (tierView == null)
            {
                continue;
            }

            int fallbackTierNumber = i + 1;
            int tierNumber = tierView.GetTierNumber(fallbackTierNumber);

            tierViews.Add(tierView);

            ProgressionUnlockableView[] unlockables = tierView.GetUnlockables();
            for (int j = 0; j < unlockables.Length; j++)
            {
                ProgressionUnlockableView unlockable = unlockables[j];
                if (unlockable == null)
                {
                    continue;
                }

                scratchUnlockTargets.Clear();
                unlockable.GetUnlockTargets(scratchUnlockTargets);

                for (int k = 0; k < scratchUnlockTargets.Count; k++)
                {
                    RegisterUnlockTarget(scratchUnlockTargets[k], tierNumber);
                }
            }
        }

        maxLevel = Mathf.Max(1, GetHighestRegisteredTier());
        UpdateLevelFromXp();
        UpdateTierVisuals();
        NotifyProgressChanged();
    }

    public bool IsUnlocked(ObjectBuildable buildable)
    {
        return IsUnlockedInternal(buildable, buildable != null ? buildable.prefab : null);
    }

    public bool IsUnlocked(Train train)
    {
        return IsUnlockedInternal(train, train != null ? train.trainPrefab : null);
    }

    public bool IsUnlocked(StaffMember staffMember)
    {
        return IsUnlockedInternal(staffMember, staffMember != null ? staffMember.staffPrefab : null);
    }

    public bool IsUnlocked(Expansion expansion)
    {
        return IsUnlockedInternal(expansion, expansion != null ? expansion.expansionPrefab : null);
    }

    public void AddXp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentXp += amount;
        UpdateLevelFromXp();
        UpdateTierVisuals();
        NotifyProgressChanged();
    }

    public void RecordBuildPlaced()
    {
        AddXp(buildPlacedXp);
    }

    public void RecordTicketSold()
    {
        AddXp(ticketSoldXp);
    }

    public void RecordPassengerBoarded()
    {
        AddXp(passengerBoardedXp);
    }

    public void RecordNeedFulfilled()
    {
        AddXp(needFulfilledXp);
    }

    public void RecordTrainUnlocked()
    {
        AddXp(trainUnlockedXp);
    }

    public void RecordStaffHired()
    {
        AddXp(staffHiredXp);
    }

    public void RecordExpansionBuilt()
    {
        AddXp(expansionBuiltXp);
    }

    public int GetXpRequiredForLevel(int level)
    {
        return baseXpPerLevel + (Mathf.Max(1, level) - 1) * xpIncreasePerLevel;
    }

    public int GetXpRequiredToReachLevel(int level)
    {
        int requiredXp = 0;

        for (int current = 1; current < level; current++)
        {
            requiredXp += GetXpRequiredForLevel(current);
        }

        return requiredXp;
    }

    private void RegisterUnlockTarget(UnityEngine.Object target, int tierNumber)
    {
        if (target == null)
        {
            return;
        }

        if (unlockTierByObject.TryGetValue(target, out int existingTier))
        {
            unlockTierByObject[target] = Mathf.Min(existingTier, tierNumber);
            return;
        }

        unlockTierByObject[target] = tierNumber;
    }

    private bool IsUnlockedInternal(UnityEngine.Object mainObject, UnityEngine.Object fallbackObject)
    {
        int requiredTier = int.MaxValue;
        bool foundTier = false;

        if (mainObject != null && unlockTierByObject.TryGetValue(mainObject, out int mainTier))
        {
            requiredTier = Mathf.Min(requiredTier, mainTier);
            foundTier = true;
        }

        if (fallbackObject != null && unlockTierByObject.TryGetValue(fallbackObject, out int fallbackTier))
        {
            requiredTier = Mathf.Min(requiredTier, fallbackTier);
            foundTier = true;
        }

        if (!foundTier)
        {
            return true;
        }

        return currentLevel >= requiredTier;
    }

    private void UpdateLevelFromXp()
    {
        currentLevel = 1;

        while (currentLevel < maxLevel && currentXp >= GetXpRequiredToReachLevel(currentLevel + 1))
        {
            currentLevel++;
        }
    }

    private void UpdateTierVisuals()
    {
        for (int i = 0; i < tierViews.Count; i++)
        {
            if (tierViews[i] == null)
            {
                continue;
            }

            int tierNumber = tierViews[i].GetTierNumber(i + 1);
            tierViews[i].SetUnlockedState(currentLevel >= tierNumber);
        }
    }

    private int GetHighestRegisteredTier()
    {
        int highestTier = 1;

        foreach (var pair in unlockTierByObject)
        {
            highestTier = Mathf.Max(highestTier, pair.Value);
        }

        return highestTier;
    }

    private int CompareTierViews(ProgressionTierView left, ProgressionTierView right)
    {
        if (left == right)
        {
            return 0;
        }

        int leftTierNumber = left != null ? left.GetTierNumber(GetHierarchyOrder(left.transform)) : int.MaxValue;
        int rightTierNumber = right != null ? right.GetTierNumber(GetHierarchyOrder(right.transform)) : int.MaxValue;

        if (leftTierNumber != rightTierNumber)
        {
            return leftTierNumber.CompareTo(rightTierNumber);
        }

        return GetHierarchyOrder(left.transform).CompareTo(GetHierarchyOrder(right.transform));
    }

    private int GetHierarchyOrder(Transform target)
    {
        int order = 0;
        int multiplier = 1;

        while (target != null)
        {
            order += target.GetSiblingIndex() * multiplier;
            multiplier *= 100;
            target = target.parent;
        }

        return order;
    }

    private void NotifyProgressChanged()
    {
        OnProgressionChanged?.Invoke();
    }
}
