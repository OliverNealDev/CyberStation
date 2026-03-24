using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ProgressionTierView : MonoBehaviour
{
    [SerializeField] private string tierTitle = "Tier 0";
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ProgressionUnlockableView[] unlockables = System.Array.Empty<ProgressionUnlockableView>();
    [SerializeField] private bool autoDiscoverUnlockables = true;

    private void OnEnable()
    {
        RefreshView();
    }

    private void OnTransformChildrenChanged()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        RefreshView();
    }

    [ContextMenu("Refresh Tier")]
    public void RefreshView()
    {
        EnsureReferences();

        if (titleText != null)
        {
            titleText.text = tierTitle;
        }

        for (int i = 0; i < unlockables.Length; i++)
        {
            if (unlockables[i] != null)
            {
                unlockables[i].RefreshView();
            }
        }
    }

    private void EnsureReferences()
    {
        if (titleText == null)
        {
            TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (textComponents.Length > 0)
            {
                titleText = textComponents[0];
            }
        }

        if (autoDiscoverUnlockables || unlockables == null || unlockables.Length == 0)
        {
            unlockables = GetComponentsInChildren<ProgressionUnlockableView>(true);
        }
    }
}
