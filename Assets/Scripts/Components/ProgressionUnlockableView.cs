using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class ProgressionUnlockableView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private ProgressionIconSource iconSource = new ProgressionIconSource();
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private bool hideWhenIconMissing;
    [SerializeField] private TextMeshProUGUI hoverText;
    [SerializeField] private string hoverTextOverride;

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

        if (iconImage == null)
        {
            return;
        }

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
                return buildable != null ? buildable.name : string.Empty;
            case ProgressionIconSourceType.Train:
                return train != null ? train.name : string.Empty;
            case ProgressionIconSourceType.Staff:
                return staffMember != null ? staffMember.name : string.Empty;
            case ProgressionIconSourceType.Expansion:
                return expansion != null ? expansion.name : string.Empty;
            case ProgressionIconSourceType.Prefab:
                return prefab != null ? prefab.name : string.Empty;
            default:
                return string.Empty;
        }
    }
}
