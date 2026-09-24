using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Builds the runtime HUD in code. The existing menus are authored in the scene; the newer
// HUD is built here instead so it can be added without rewiring a 50k-line scene file,
// and it borrows the scene's own font and colours so the two read as one interface.
public static class UiKit
{
    public static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.06f, 0.92f);
    public static readonly Color TextColor = new Color(0.93f, 0.97f, 1f, 1f);
    public static readonly Color MutedTextColor = new Color(0.58f, 0.66f, 0.74f, 1f);
    public static readonly Color Cyan = new Color(0.1f, 0.9f, 1f, 1f);
    public static readonly Color Magenta = new Color(1f, 0.18f, 0.56f, 1f);
    public static readonly Color Yellow = new Color(1f, 0.83f, 0.25f, 1f);
    public static readonly Color Green = new Color(0.25f, 1f, 0.55f, 1f);
    public static readonly Color Red = new Color(1f, 0.3f, 0.37f, 1f);
    public static readonly Color ButtonColor = new Color(0.12f, 0.14f, 0.18f, 1f);

    private static Sprite roundedSprite;
    private static Sprite ringSprite;
    private static Sprite whiteSprite;
    private static Sprite triangleSprite;
    private static Sprite starSprite;
    private static TMP_FontAsset cachedFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedFont = null;
        starSprite = null;
    }

    // The scene font has no star or arrow glyphs and no fallback font, so symbols are
    // drawn as sprites rather than risk rendering as empty boxes.
    public static Sprite TriangleSprite => triangleSprite != null ? triangleSprite : triangleSprite = CreateTriangleSprite();

    public static Sprite StarSprite
    {
        get
        {
            if (starSprite == null)
            {
                RatingMenuController[] ratingMenus = UnityEngine.Object.FindObjectsByType<RatingMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (ratingMenus.Length > 0)
                {
                    starSprite = ratingMenus[0].starFilled;
                }
            }

            return starSprite != null ? starSprite : TriangleSprite;
        }
    }

    public static TMP_FontAsset Font
    {
        get
        {
            if (cachedFont == null && UIController.Instance != null && UIController.Instance.moneyText != null)
            {
                cachedFont = UIController.Instance.moneyText.font;
            }

            return cachedFont != null ? cachedFont : TMP_Settings.defaultFontAsset;
        }
    }

    public static Sprite RoundedSprite => roundedSprite != null ? roundedSprite : roundedSprite = CreateRoundedSprite(false);
    public static Sprite RingSprite => ringSprite != null ? ringSprite : ringSprite = CreateRoundedSprite(true);

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(255, 255, 255, 255);
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            }

            return whiteSprite;
        }
    }

    public static Canvas CreateOverlayCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        // Same scaler settings as the scene's ScreenCanvas, so a position here lines up
        // with the same position on the top bar at every resolution.
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        return canvas;
    }

    public static RectTransform CreateRect(string name, Transform parent, Vector2 centerPosition, Vector2 size)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = centerPosition;
        rect.sizeDelta = size;
        return rect;
    }

    public static RectTransform CreateStretchRect(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.zero);
        Stretch(rect);
        return rect;
    }

    public static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    public static Image CreatePanel(string name, Transform parent, Vector2 centerPosition, Vector2 size, Color color, bool rounded = true)
    {
        RectTransform rect = CreateRect(name, parent, centerPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = rounded ? RoundedSprite : WhiteSprite;
        image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Image AddImage(RectTransform rect, Color color, bool rounded = true)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = rounded ? RoundedSprite : WhiteSprite;
        image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Image CreateAccentBar(Transform parent, Color color, float width = 4f)
    {
        RectTransform rect = CreateRect("Accent", parent, Vector2.zero, Vector2.zero);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(0f, 6f);
        rect.offsetMax = new Vector2(width, -6f);
        return AddImage(rect, color, false);
    }

    public static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center,
        Vector2? centerPosition = null,
        Vector2? size = null)
    {
        RectTransform rect = CreateRect(name, parent, centerPosition ?? Vector2.zero, size ?? new Vector2(200f, 40f));
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    public static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 centerPosition,
        Vector2 size,
        UnityAction onClick,
        float fontSize = 26f,
        Color? baseColor = null,
        Color? textColor = null)
    {
        Image background = CreatePanel(name, parent, centerPosition, size, baseColor ?? ButtonColor);
        background.raycastTarget = true;

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        Color normal = baseColor ?? ButtonColor;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.25f);
        colors.selectedColor = normal;
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.35f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        // Deselect after every click: a selected button would otherwise also fire on
        // Space, which is the pause key. The click sound comes from SoundEffectController,
        // which hooks every Button in the scene.
        button.onClick.AddListener(() =>
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        });

        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI text = CreateText("Label", background.transform, label, fontSize, textColor ?? TextColor);
            Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        return button;
    }

    public static TextMeshProUGUI GetButtonLabel(Button button)
    {
        return button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    // Uses the same tooltip components as the existing top-bar buttons, so hover text
    // looks identical whichever system built the button.
    public static void AddTooltip(GameObject target, string tooltip, float yOffset = -48f, float xOffset = 0f)
    {
        if (target == null || string.IsNullOrEmpty(tooltip))
        {
            return;
        }

        TextMeshProUGUI text = CreateText("Tooltip", target.transform, tooltip, 22f, TextColor,
            TextAlignmentOptions.Center, new Vector2(xOffset, yOffset), new Vector2(320f, 36f));
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.gameObject.AddComponent<TextBackground>();
        text.gameObject.SetActive(false);

        HoverShowChild hover = target.AddComponent<HoverShowChild>();
        hover.childObject = text.gameObject;
    }

    public static RectTransform CreateProgressBar(string name, Transform parent, Vector2 centerPosition, Vector2 size, Color fillColor, out RectTransform fill)
    {
        Image track = CreatePanel(name, parent, centerPosition, size, new Color(1f, 1f, 1f, 0.08f), false);
        RectTransform fillRect = CreateRect("Fill", track.transform, Vector2.zero, Vector2.zero);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        AddImage(fillRect, fillColor, false);
        fill = fillRect;
        return track.rectTransform;
    }

    public static void SetProgress(RectTransform fill, float progress01)
    {
        if (fill == null)
        {
            return;
        }

        fill.anchorMax = new Vector2(Mathf.Clamp01(progress01), 1f);
        fill.offsetMax = Vector2.zero;
    }

    public static VerticalLayoutGroup AddVerticalLayout(GameObject target, float spacing, RectOffset padding = null, TextAnchor alignment = TextAnchor.UpperCenter)
    {
        VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static LayoutElement SetLayoutSize(GameObject target, float preferredHeight, float preferredWidth = -1f)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = target.AddComponent<LayoutElement>();
        }

        element.preferredHeight = preferredHeight;
        element.minHeight = preferredHeight;
        if (preferredWidth >= 0f)
        {
            element.preferredWidth = preferredWidth;
        }

        return element;
    }

    public static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }

    public static string FormatMoney(int amount)
    {
        string sign = amount < 0 ? "-" : string.Empty;
        return sign + "$" + Mathf.Abs(amount).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static Color RatingColor(float rating)
    {
        if (rating >= 4f) return Green;
        if (rating >= 3f) return Yellow;
        if (rating >= 2f) return new Color(1f, 0.6f, 0.2f, 1f);
        return Red;
    }

    public static Color GradeColor(StationGrade grade)
    {
        switch (grade)
        {
            case StationGrade.S: return Magenta;
            case StationGrade.A: return Cyan;
            case StationGrade.B: return Green;
            case StationGrade.C: return Yellow;
            case StationGrade.D: return Red;
            default: return MutedTextColor;
        }
    }

    public static string Hex(Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(color);
    }

    public static Image CreateIcon(string name, Transform parent, Sprite sprite, Vector2 centerPosition, Vector2 size, Color color, float rotationDegrees = 0f)
    {
        RectTransform rect = CreateRect(name, parent, centerPosition, size);
        rect.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Points right; rotate the Image for other directions.
    private static Sprite CreateTriangleSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 a = new Vector2(4f, 3f);
        Vector2 b = new Vector2(4f, size - 3f);
        Vector2 c = new Vector2(size - 3f, size * 0.5f);

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float distance = Mathf.Min(EdgeDistance(p, a, b), Mathf.Min(EdgeDistance(p, b, c), EdgeDistance(p, c, a)));
                float alpha = Mathf.Clamp01(distance + 0.5f);
                pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Signed distance from p to the edge from->to, positive on the triangle's inner side
    // (the triangle is wound clockwise in texture space).
    private static float EdgeDistance(Vector2 p, Vector2 from, Vector2 to)
    {
        Vector2 edge = to - from;
        Vector2 normal = new Vector2(edge.y, -edge.x).normalized;
        return Vector2.Dot(p - from, normal);
    }

    // A 9-sliceable rounded rectangle, drawn once at startup so the HUD needs no texture assets.
    private static Sprite CreateRoundedSprite(bool ringOnly)
    {
        const int size = 32;
        const float radius = 10f;
        const float ringThickness = 2.5f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        float halfSize = size * 0.5f;
        float innerHalfExtent = halfSize - radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Signed distance to a rounded box: negative inside, positive outside.
                float qx = Mathf.Abs(x + 0.5f - halfSize) - innerHalfExtent;
                float qy = Mathf.Abs(y + 0.5f - halfSize) - innerHalfExtent;
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                float signedDistance = outside + inside - radius;

                float alpha = Mathf.Clamp01(0.5f - signedDistance);
                if (ringOnly)
                {
                    float inner = Mathf.Clamp01(0.5f - (signedDistance + ringThickness));
                    alpha = Mathf.Clamp01(alpha - inner);
                }

                pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Vector4 border = new Vector4(radius + 1, radius + 1, radius + 1, radius + 1);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
