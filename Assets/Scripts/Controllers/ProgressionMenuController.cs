using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ProgressionMenuController : MonoBehaviour
{
    private const float TierCardWidth = 1000f;
    private const float TierCardHeight = 240f;
    private const float TierCardSpacingY = 28f;

    private ProgressionTierView[] tierViews = System.Array.Empty<ProgressionTierView>();
    private bool isRefreshingAll;

    private void OnEnable()
    {
        ProgressionManager.OnProgressionChanged += RefreshUnlockStates;
        RefreshAll();
    }

    private void OnDisable()
    {
        ProgressionManager.OnProgressionChanged -= RefreshUnlockStates;
    }

    private void OnTransformChildrenChanged()
    {
        if (!isActiveAndEnabled || isRefreshingAll)
        {
            return;
        }

        RefreshAll();
    }

    [ContextMenu("Refresh Progression")]
    public void RefreshAll()
    {
        isRefreshingAll = true;
        try
        {
            tierViews = GetComponentsInChildren<ProgressionTierView>(true);
            System.Array.Sort(tierViews, CompareTierViews);
            ConfigureTierContainerLayout();

            Dictionary<string, ObjectBuildable> buildables = BuildLookup(Resources.LoadAll<ObjectBuildable>("BuildItems"));
            Dictionary<string, Train> trains = BuildLookup(Resources.LoadAll<Train>("Trains"));
            Dictionary<string, StaffMember> staffMembers = BuildLookup(Resources.LoadAll<StaffMember>("Staff"));
            Dictionary<string, Expansion> expansions = BuildLookup(Resources.LoadAll<Expansion>("Expansions"));

            int currentLevel = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 1;

            for (int i = 0; i < tierViews.Length; i++)
            {
                if (tierViews[i] != null)
                {
                    int tierNumber = tierViews[i].GetTierNumber(i + 1);
                    tierViews[i].SetUnlockableEntries(BuildEntriesForTier(
                        tierNumber,
                        buildables,
                        trains,
                        staffMembers,
                        expansions));
                    tierViews[i].RefreshView();
                    tierViews[i].SetUnlockedState(currentLevel >= tierNumber);
                }
            }
        }
        finally
        {
            isRefreshingAll = false;
        }
    }

    private void RefreshUnlockStates()
    {
        if (tierViews == null || tierViews.Length == 0)
        {
            RefreshAll();
            return;
        }

        int currentLevel = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 1;
        for (int i = 0; i < tierViews.Length; i++)
        {
            if (tierViews[i] == null)
            {
                continue;
            }

            int tierNumber = tierViews[i].GetTierNumber(i + 1);
            tierViews[i].SetUnlockedState(currentLevel >= tierNumber);
        }
    }

    private int CompareTierViews(ProgressionTierView left, ProgressionTierView right)
    {
        int leftTier = left != null ? left.GetTierNumber(0) : 0;
        int rightTier = right != null ? right.GetTierNumber(0) : 0;
        return leftTier.CompareTo(rightTier);
    }

    private void ConfigureTierContainerLayout()
    {
        if (tierViews.Length == 0 || tierViews[0] == null)
        {
            return;
        }

        RectTransform tiersRoot = tierViews[0].transform.parent as RectTransform;
        if (tiersRoot == null)
        {
            return;
        }

        GridLayoutGroup gridLayout = tiersRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 1;
            gridLayout.cellSize = new Vector2(TierCardWidth, TierCardHeight);
            gridLayout.spacing = new Vector2(0f, TierCardSpacingY);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
        }

        tiersRoot.anchorMin = new Vector2(0f, 1f);
        tiersRoot.anchorMax = new Vector2(1f, 1f);
        tiersRoot.pivot = new Vector2(0f, 1f);
        tiersRoot.anchoredPosition = Vector2.zero;

        float totalHeight = (tierViews.Length * TierCardHeight) + (Mathf.Max(0, tierViews.Length - 1) * TierCardSpacingY);
        if (gridLayout != null)
        {
            totalHeight += gridLayout.padding.top + gridLayout.padding.bottom;
        }

        Vector2 sizeDelta = tiersRoot.sizeDelta;
        sizeDelta.x = 0f;
        sizeDelta.y = totalHeight;
        tiersRoot.sizeDelta = sizeDelta;

        LayoutRebuilder.ForceRebuildLayoutImmediate(tiersRoot);
    }

    private List<ProgressionUnlockableEntry> BuildEntriesForTier(
        int tierNumber,
        Dictionary<string, ObjectBuildable> buildables,
        Dictionary<string, Train> trains,
        Dictionary<string, StaffMember> staffMembers,
        Dictionary<string, Expansion> expansions)
    {
        List<ProgressionUnlockableEntry> entries = new List<ProgressionUnlockableEntry>();

        switch (tierNumber)
        {
            case 1:
                AddTrain(entries, trains, "Orange");
                AddTrain(entries, trains, "Cyan");
                AddBuildable(entries, buildables, "TicketMachine");
                AddBuildable(entries, buildables, "Materializer");
                AddBuildable(entries, buildables, "NutrientDispenser");
                AddBuildable(entries, buildables, "HydratingObelisk");
                AddBuildable(entries, buildables, "LightStandard");
                AddBuildable(entries, buildables, "WallBlock");
                break;

            case 2:
                AddTrain(entries, trains, "Yellow");
                AddBuildable(entries, buildables, "EnergyBottleDispenser");
                AddBuildable(entries, buildables, "SnackPrinter");
                AddBuildable(entries, buildables, "BottleDispenser");
                AddExpansion(entries, expansions, "Platform2");
                AddStaff(entries, staffMembers, "Janitor");
                AddStaff(entries, staffMembers, "SecurityGuard");
                AddBuildable(entries, buildables, "TrainlineGlobe");
                break;

            case 3:
                AddTrain(entries, trains, "Pink");
                AddBuildable(entries, buildables, "PrivateLavatory");
                AddExpansion(entries, expansions, "LeftExpansion");
                AddExpansion(entries, expansions, "RightExpansion");
                AddBuildable(entries, buildables, "SquareBillboard");
                break;

            case 4:
                AddTrain(entries, trains, "Green");
                AddTrain(entries, trains, "Red");
                AddBuildable(entries, buildables, "CleansingShower");
                break;

            case 5:
                AddTrain(entries, trains, "Blue");
                AddTrain(entries, trains, "Purple");
                break;
        }

        return entries;
    }

    private static Dictionary<string, T> BuildLookup<T>(T[] items) where T : Object
    {
        Dictionary<string, T> lookup = new Dictionary<string, T>();

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                continue;
            }

            lookup[items[i].name] = items[i];
        }

        return lookup;
    }

    private void AddBuildable(
        List<ProgressionUnlockableEntry> entries,
        Dictionary<string, ObjectBuildable> buildables,
        string assetName)
    {
        if (buildables.TryGetValue(assetName, out ObjectBuildable buildable))
        {
            entries.Add(ProgressionUnlockableEntry.Buildable(buildable));
        }
    }

    private void AddTrain(
        List<ProgressionUnlockableEntry> entries,
        Dictionary<string, Train> trains,
        string assetName)
    {
        if (trains.TryGetValue(assetName, out Train train))
        {
            entries.Add(ProgressionUnlockableEntry.Train(train));
        }
    }

    private void AddStaff(
        List<ProgressionUnlockableEntry> entries,
        Dictionary<string, StaffMember> staffMembers,
        string assetName)
    {
        if (staffMembers.TryGetValue(assetName, out StaffMember staffMember))
        {
            entries.Add(ProgressionUnlockableEntry.Staff(staffMember));
        }
    }

    private void AddExpansion(
        List<ProgressionUnlockableEntry> entries,
        Dictionary<string, Expansion> expansions,
        string assetName)
    {
        if (expansions.TryGetValue(assetName, out Expansion expansion))
        {
            entries.Add(ProgressionUnlockableEntry.Expansion(expansion));
        }
    }
}
