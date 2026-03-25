using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ProgressionTierView : MonoBehaviour
{
    [SerializeField] private string tierTitle = "Tier 0";
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ProgressionUnlockableView[] unlockables = System.Array.Empty<ProgressionUnlockableView>();
    [SerializeField] private bool autoDiscoverUnlockables = true;
    [SerializeField] private Image panelImage;
    [SerializeField] private Color unlockedPanelColor = new Color(0.08f, 0.2f, 0.12f, 0.6f);

    private Color defaultPanelColor = Color.white;
    private bool hasDefaultPanelColor;

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
