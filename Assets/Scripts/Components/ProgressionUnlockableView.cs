using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class ProgressionUnlockableView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private ProgressionIconSource iconSource = new ProgressionIconSource();
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private bool hideWhenIconMissing;
    [SerializeField] private TextMeshProUGUI hoverText;
    [SerializeField] private string hoverTextOverride;
    [SerializeField] private float lockedIconAlphaMultiplier = 0.25f;
    [SerializeField] private float lockedBackgroundAlphaMultiplier = 0.6f;

    private bool isUnlocked = true;
    private bool cachedDefaultColors;
    private static readonly Color RuntimeIconBaseColor = Color.white;
    private Color defaultIconColor = RuntimeIconBaseColor;
    private Color defaultBackgroundColor = Color.white;

    private void OnEnable()
    {
        RefreshView();
    }

    [ContextMenu("Refresh Unlockable")]
    public void RefreshView()
    {
        if (iconImage == null)
        {
            iconImage = transform.Find("Icon")?.GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = transform.Find("Panel")?.GetComponent<Image>();
        }

        if (iconImage == null)
        {
            return;
        }

        ResetIconTintForRuntimeEntry();
        CacheDefaultColors();

        Sprite resolvedIcon = iconSource.GetIcon();
        iconImage.sprite = resolvedIcon;
        iconImage.preserveAspect = preserveAspect;
        iconImage.enabled = !hideWhenIconMissing || resolvedIcon != null;

        if (hoverText == null)
        {
            hoverText = transform.Find("HoverPanel/HoverText")?.GetComponent<TextMeshProUGUI>();
        }

        if (hoverText != null)
        {
            hoverText.text = GetHoverText();
            SetHoverVisible(false);
        }

        ApplyUnlockVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverText == null)
        {
            return;
        }

        string label = GetHoverText();
        hoverText.text = label;
        SetHoverVisible(!string.IsNullOrWhiteSpace(label));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverVisible(false);
    }

    private string GetHoverText()
    {
        if (!string.IsNullOrWhiteSpace(hoverTextOverride))
        {
            return hoverTextOverride;
        }

        return iconSource.GetDisplayName();
    }

    private void SetHoverVisible(bool isVisible)
    {
        if (hoverText == null)
        {
            return;
        }

        Transform hoverRoot = hoverText.transform.parent;
        GameObject target = hoverRoot != null && hoverRoot != transform ? hoverRoot.gameObject : hoverText.gameObject;
        target.SetActive(isVisible);
    }

    public void SetUnlocked(bool value)
    {
        isUnlocked = value;
        ApplyUnlockVisuals();
    }

    public void Configure(ObjectBuildable buildable, string hoverOverride = null)
    {
        iconSource.SetBuildable(buildable);
        hoverTextOverride = hoverOverride ?? string.Empty;
        gameObject.SetActive(buildable != null);
        RefreshView();
    }

    public void Configure(Train train, string hoverOverride = null)
    {
        iconSource.SetTrain(train);
        hoverTextOverride = hoverOverride ?? string.Empty;
        gameObject.SetActive(train != null);
        RefreshView();
    }

    public void Configure(StaffMember staffMember, string hoverOverride = null)
    {
        iconSource.SetStaff(staffMember);
        hoverTextOverride = hoverOverride ?? string.Empty;
        gameObject.SetActive(staffMember != null);
        RefreshView();
    }

    public void Configure(Expansion expansion, string hoverOverride = null)
    {
        iconSource.SetExpansion(expansion);
        hoverTextOverride = hoverOverride ?? string.Empty;
        gameObject.SetActive(expansion != null);
        RefreshView();
    }

    public void ClearDefinition()
    {
        iconSource.Clear();
        hoverTextOverride = string.Empty;
        SetHoverVisible(false);
        gameObject.SetActive(false);
    }

    private void ApplyUnlockVisuals()
    {
        if (iconImage != null)
        {
            iconImage.color = GetStateColor(defaultIconColor, isUnlocked ? 1f : lockedIconAlphaMultiplier);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = GetStateColor(defaultBackgroundColor, isUnlocked ? 1f : lockedBackgroundAlphaMultiplier);
        }
    }

    private void CacheDefaultColors()
    {
        if (cachedDefaultColors)
        {
            return;
        }

        if (iconImage != null)
        {
            defaultIconColor = RuntimeIconBaseColor;
        }

        if (backgroundImage != null)
        {
            defaultBackgroundColor = backgroundImage.color;
        }

        cachedDefaultColors = true;
    }

    private Color GetStateColor(Color baseColor, float alphaMultiplier)
    {
        baseColor.a *= alphaMultiplier;
        return baseColor;
    }

    private void ResetIconTintForRuntimeEntry()
    {
        if (iconImage == null)
        {
            return;
        }

        Color color = RuntimeIconBaseColor;
        color.a = iconImage.color.a;
        iconImage.color = color;
    }
}

public enum ProgressionIconSourceType
{
    None,
    SpriteOverride,
    Buildable,
    Train,
    Staff,
    Expansion,
    Prefab
}

[System.Serializable]
public class ProgressionIconSource
{
    [SerializeField] private ProgressionIconSourceType sourceType;
    [SerializeField] private Sprite spriteOverride;
    [SerializeField] private ObjectBuildable buildable;
    [SerializeField] private Train train;
    [SerializeField] private StaffMember staffMember;
    [SerializeField] private Expansion expansion;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite prefabFallbackIcon;
    [SerializeField] private PrefabIconView prefabView = PrefabIconView.BuildablesAndStaff;

    public Sprite GetIcon()
    {
        switch (sourceType)
        {
            case ProgressionIconSourceType.SpriteOverride:
                return spriteOverride;
            case ProgressionIconSourceType.Buildable:
                return buildable != null ? buildable.GetIcon() : null;
            case ProgressionIconSourceType.Train:
                return train != null ? train.GetIcon() : null;
            case ProgressionIconSourceType.Staff:
                return staffMember != null ? staffMember.GetIcon() : null;
            case ProgressionIconSourceType.Expansion:
                return expansion != null ? expansion.GetIcon() : null;
            case ProgressionIconSourceType.Prefab:
                return PrefabIconRenderer.GetIcon(prefab, prefabFallbackIcon, prefabView);
            default:
                return null;
        }
    }

    public string GetDisplayName()
    {
        switch (sourceType)
        {
            case ProgressionIconSourceType.SpriteOverride:
                return spriteOverride != null ? spriteOverride.name : string.Empty;
            case ProgressionIconSourceType.Buildable:
                return buildable != null ? buildable.objectName : string.Empty;
            case ProgressionIconSourceType.Train:
                return train != null ? train.trainName : string.Empty;
            case ProgressionIconSourceType.Staff:
                return staffMember != null ? staffMember.staffName : string.Empty;
            case ProgressionIconSourceType.Expansion:
                return expansion != null ? expansion.name : string.Empty;
            case ProgressionIconSourceType.Prefab:
                return prefab != null ? prefab.name : string.Empty;
            default:
                return string.Empty;
        }
    }

    public void Clear()
    {
        sourceType = ProgressionIconSourceType.None;
        spriteOverride = null;
        buildable = null;
        train = null;
        staffMember = null;
        expansion = null;
        prefab = null;
        prefabFallbackIcon = null;
    }

    public void SetBuildable(ObjectBuildable value)
    {
        Clear();
        sourceType = ProgressionIconSourceType.Buildable;
        buildable = value;
    }

    public void SetTrain(Train value)
    {
        Clear();
        sourceType = ProgressionIconSourceType.Train;
        train = value;
    }

    public void SetStaff(StaffMember value)
    {
        Clear();
        sourceType = ProgressionIconSourceType.Staff;
        staffMember = value;
    }

    public void SetExpansion(Expansion value)
    {
        Clear();
        sourceType = ProgressionIconSourceType.Expansion;
        expansion = value;
    }
}

public enum ProgressionUnlockableEntryType
{
    Buildable,
    Train,
    Staff,
    Expansion
}

public class ProgressionUnlockableEntry
{
    private readonly ProgressionUnlockableEntryType entryType;
    private readonly ObjectBuildable buildable;
    private readonly Train train;
    private readonly StaffMember staffMember;
    private readonly Expansion expansion;
    private readonly string hoverTextOverride;

    private ProgressionUnlockableEntry(
        ProgressionUnlockableEntryType entryType,
        ObjectBuildable buildable,
        Train train,
        StaffMember staffMember,
        Expansion expansion,
        string hoverTextOverride)
    {
        this.entryType = entryType;
        this.buildable = buildable;
        this.train = train;
        this.staffMember = staffMember;
        this.expansion = expansion;
        this.hoverTextOverride = hoverTextOverride;
    }

    public static ProgressionUnlockableEntry Buildable(ObjectBuildable buildable, string hoverTextOverride = null)
    {
        return new ProgressionUnlockableEntry(
            ProgressionUnlockableEntryType.Buildable,
            buildable,
            null,
            null,
            null,
            hoverTextOverride);
    }

    public static ProgressionUnlockableEntry Train(Train train, string hoverTextOverride = null)
    {
        return new ProgressionUnlockableEntry(
            ProgressionUnlockableEntryType.Train,
            null,
            train,
            null,
            null,
            hoverTextOverride);
    }

    public static ProgressionUnlockableEntry Staff(StaffMember staffMember, string hoverTextOverride = null)
    {
        return new ProgressionUnlockableEntry(
            ProgressionUnlockableEntryType.Staff,
            null,
            null,
            staffMember,
            null,
            hoverTextOverride);
    }

    public static ProgressionUnlockableEntry Expansion(Expansion expansion, string hoverTextOverride = null)
    {
        return new ProgressionUnlockableEntry(
            ProgressionUnlockableEntryType.Expansion,
            null,
            null,
            null,
            expansion,
            hoverTextOverride);
    }

    public void ApplyTo(ProgressionUnlockableView view)
    {
        if (view == null)
        {
            return;
        }

        switch (entryType)
        {
            case ProgressionUnlockableEntryType.Buildable:
                view.Configure(buildable, hoverTextOverride);
                break;
            case ProgressionUnlockableEntryType.Train:
                view.Configure(train, hoverTextOverride);
                break;
            case ProgressionUnlockableEntryType.Staff:
                view.Configure(staffMember, hoverTextOverride);
                break;
            case ProgressionUnlockableEntryType.Expansion:
                view.Configure(expansion, hoverTextOverride);
                break;
        }
    }
}
