using System;
using UnityEngine;

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
        RefreshMaxLevel();
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

    public int XpNeededForNextLevel => GetXpRequiredForLevel(currentLevel);

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

    public bool IsUnlocked(ObjectBuildable buildable)
    {
        return buildable == null || currentLevel >= Mathf.Max(1, buildable.requiredTier);
    }

    public bool IsUnlocked(Train train)
    {
        return train == null || currentLevel >= Mathf.Max(1, train.requiredTier);
    }

    public bool IsUnlocked(StaffMember staffMember)
    {
        return staffMember == null || currentLevel >= Mathf.Max(1, staffMember.requiredTier);
    }

    public bool IsUnlocked(Expansion expansion)
    {
        return expansion == null || currentLevel >= Mathf.Max(1, expansion.requiredTier);
    }

    public void AddXp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentXp += amount;
        UpdateLevelFromXp();
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

    private void RefreshMaxLevel()
    {
        maxLevel = 1;

        UpdateMaxLevel(Resources.LoadAll<ObjectBuildable>("BuildItems"));
        UpdateMaxLevel(Resources.LoadAll<Train>("Trains"));
        UpdateMaxLevel(Resources.LoadAll<StaffMember>("Staff"));
        UpdateMaxLevel(Resources.LoadAll<Expansion>("Expansions"));

        UpdateLevelFromXp();
    }

    private void UpdateMaxLevel(ObjectBuildable[] buildables)
    {
        for (int i = 0; i < buildables.Length; i++)
        {
            if (buildables[i] != null)
            {
                maxLevel = Mathf.Max(maxLevel, Mathf.Max(1, buildables[i].requiredTier));
            }
        }
    }

    private void UpdateMaxLevel(Train[] trains)
    {
        for (int i = 0; i < trains.Length; i++)
        {
            if (trains[i] != null)
            {
                maxLevel = Mathf.Max(maxLevel, Mathf.Max(1, trains[i].requiredTier));
            }
        }
    }

    private void UpdateMaxLevel(StaffMember[] staffMembers)
    {
        for (int i = 0; i < staffMembers.Length; i++)
        {
            if (staffMembers[i] != null)
            {
                maxLevel = Mathf.Max(maxLevel, Mathf.Max(1, staffMembers[i].requiredTier));
            }
        }
    }

    private void UpdateMaxLevel(Expansion[] expansions)
    {
        for (int i = 0; i < expansions.Length; i++)
        {
            if (expansions[i] != null)
            {
                maxLevel = Mathf.Max(maxLevel, Mathf.Max(1, expansions[i].requiredTier));
            }
        }
    }

    private void UpdateLevelFromXp()
    {
        currentLevel = 1;

        while (currentLevel < maxLevel && currentXp >= GetXpRequiredToReachLevel(currentLevel + 1))
        {
            currentLevel++;
        }
    }

    private void NotifyProgressChanged()
    {
        OnProgressionChanged?.Invoke();
    }
}
