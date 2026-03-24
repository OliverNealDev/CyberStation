using UnityEngine;

[DisallowMultipleComponent]
public class ProgressionMenuController : MonoBehaviour
{
    private ProgressionTierView[] tierViews = System.Array.Empty<ProgressionTierView>();

    private void OnEnable()
    {
        ProgressionManager.OnProgressionChanged += RefreshAll;
        RefreshAll();
    }

    private void OnDisable()
    {
        ProgressionManager.OnProgressionChanged -= RefreshAll;
    }

    private void OnTransformChildrenChanged()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        RefreshAll();
    }

    [ContextMenu("Refresh Progression")]
    public void RefreshAll()
    {
        tierViews = GetComponentsInChildren<ProgressionTierView>(true);

        for (int i = 0; i < tierViews.Length; i++)
        {
            if (tierViews[i] != null)
            {
                tierViews[i].RefreshView();
                int tierNumber = tierViews[i].GetTierNumber(i + 1);
                int currentLevel = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 1;
                tierViews[i].SetUnlockedState(currentLevel >= tierNumber);
            }
        }
    }
}
