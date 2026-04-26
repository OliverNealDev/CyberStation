using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ProgressionTierView : MonoBehaviour
{
    [SerializeField] private string tierTitle = "Tier 0";
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI unlocksLabelText;
    [SerializeField] private ProgressionUnlockableView[] unlockables = System.Array.Empty<ProgressionUnlockableView>();
    [SerializeField] private bool autoDiscoverUnlockables = true;
    [SerializeField] private Image panelImage;
    [SerializeField] private Transform unlocksRoot;
    [SerializeField] private Color unlockedPanelColor = new Color(0.08f, 0.2f, 0.12f, 0.6f);

    private Color defaultPanelColor = Color.white;
    private bool hasDefaultPanelColor;
    private bool isConfiguringUnlockables;

    private void OnEnable()
    {
        RefreshView();
    }

    private void OnTransformChildrenChanged()
    {
        if (!isActiveAndEnabled || isConfiguringUnlockables)
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

        Image sourceImage = gameObject.GetComponentInChildren<Image>(true);
        if (panelImage == null && sourceImage != null)
        {
            panelImage = sourceImage;
        }

        if (panelImage != null && sourceImage != null)
        {
            panelImage.sprite = sourceImage.sprite;

            if (!hasDefaultPanelColor)
            {
                defaultPanelColor = panelImage.color;
                hasDefaultPanelColor = true;
            }
        }
    }

    public int GetTierNumber(int fallbackTierNumber)
    {
        EnsureReferences();

        int runtimeTierNumber = ExtractTierNumber(titleText != null ? titleText.text : string.Empty, 0);
        if (runtimeTierNumber <= 0)
        {
            runtimeTierNumber = ExtractTierNumber(tierTitle, 0);
        }

        if (runtimeTierNumber <= 0)
        {
            runtimeTierNumber = ExtractTierNumber(gameObject.name, fallbackTierNumber);
        }

        return runtimeTierNumber;
    }

    public void SetUnlockedState(bool isUnlocked)
    {
        EnsureReferences();

        for (int i = 0; i < unlockables.Length; i++)
        {
            if (unlockables[i] != null)
            {
                unlockables[i].SetUnlocked(isUnlocked);
            }
        }

        if (panelImage != null)
        {
            panelImage.color = isUnlocked ? unlockedPanelColor : defaultPanelColor;
        }
    }

    public void SetUnlockableEntries(IReadOnlyList<ProgressionUnlockableEntry> entries)
    {
        EnsureReferences();

        if (unlocksRoot == null)
        {
            return;
        }

        isConfiguringUnlockables = true;
        try
        {
            List<ProgressionUnlockableView> unlockableViews = new List<ProgressionUnlockableView>(
                unlocksRoot.GetComponentsInChildren<ProgressionUnlockableView>(true));

            ProgressionUnlockableView template = unlockableViews.Count > 0 ? unlockableViews[0] : null;
            while (template != null && unlockableViews.Count < entries.Count)
            {
                ProgressionUnlockableView clone = Instantiate(template, unlocksRoot);
                clone.gameObject.SetActive(true);
                unlockableViews.Add(clone);
            }

            for (int i = 0; i < unlockableViews.Count; i++)
            {
                if (i < entries.Count)
                {
                    entries[i].ApplyTo(unlockableViews[i]);
                }
                else
                {
                    unlockableViews[i].ClearDefinition();
                }
            }

            unlockables = unlockableViews.ToArray();
        }
        finally
        {
            isConfiguringUnlockables = false;
        }
    }

    private void EnsureReferences()
    {
        if (titleText == null)
        {
            Transform directTitleTransform = FindDescendantByName(transform, "Text (TMP)");
            if (directTitleTransform != null)
            {
                titleText = directTitleTransform.GetComponent<TextMeshProUGUI>();
            }

            if (titleText == null)
            {
                TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < textComponents.Length; i++)
                {
                    if (textComponents[i] == null)
                    {
                        continue;
                    }

                    if (textComponents[i].text.IndexOf("tier", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        titleText = textComponents[i];
                        break;
                    }
                }

                if (titleText == null && textComponents.Length > 0)
                {
                    titleText = textComponents[0];
                }
            }
        }

        if (unlocksLabelText == null)
        {
            Transform directUnlocksLabelTransform = FindDescendantByName(transform, "Text (TMP) (1)");
            if (directUnlocksLabelTransform != null)
            {
                unlocksLabelText = directUnlocksLabelTransform.GetComponent<TextMeshProUGUI>();
            }

            TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < textComponents.Length; i++)
            {
                if (textComponents[i] == null || textComponents[i] == titleText)
                {
                    continue;
                }

                if (textComponents[i].text.IndexOf("unlock", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    unlocksLabelText = textComponents[i];
                    break;
                }
            }

            if (unlocksLabelText == null)
            {
                for (int i = 0; i < textComponents.Length; i++)
                {
                    if (textComponents[i] != null && textComponents[i] != titleText)
                    {
                        unlocksLabelText = textComponents[i];
                        break;
                    }
                }
            }
        }

        if (autoDiscoverUnlockables || unlockables == null || unlockables.Length == 0)
        {
            unlockables = GetComponentsInChildren<ProgressionUnlockableView>(true);
        }

        if (unlocksRoot == null)
        {
            Transform directUnlocksRoot = FindDescendantByName(transform, "Unlocks");
            if (directUnlocksRoot != null)
            {
                unlocksRoot = directUnlocksRoot;
            }
        }

        if (panelImage == null)
        {
            panelImage = GetComponent<Image>();
        }

        if (panelImage == null)
        {
            panelImage = GetComponentInChildren<Image>(true);
        }

        if (panelImage != null && !hasDefaultPanelColor)
        {
            defaultPanelColor = panelImage.color;
            hasDefaultPanelColor = true;
        }
    }

    private static Transform FindDescendantByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i] != null && descendants[i].name == childName)
            {
                return descendants[i];
            }
        }

        return null;
    }

    private int ExtractTierNumber(string source, int fallbackTierNumber)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return fallbackTierNumber;
        }

        int value = 0;
        bool foundDigit = false;

        for (int i = 0; i < source.Length; i++)
        {
            char character = source[i];
            if (!char.IsDigit(character))
            {
                continue;
            }

            foundDigit = true;
            value = (value * 10) + (character - '0');
        }

        return foundDigit ? Mathf.Max(1, value) : fallbackTierNumber;
    }
}
