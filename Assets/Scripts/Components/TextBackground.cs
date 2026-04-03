using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TMP_Text))]
public class TextBackground : MonoBehaviour
{
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.82f);
    [SerializeField] private Vector2 padding = new Vector2(20f, 12f);
    [SerializeField] private Vector2 minimumSize = new Vector2(0f, 0f);

    private static Sprite sharedSprite;

    private TMP_Text tooltipText;
    private RectTransform textRect;
    private RenderUIOnTop renderUIOnTop;
    private RectTransform backgroundRect;
    private Image backgroundImage;
    private Canvas backgroundCanvas;

    private void Awake()
    {
        CacheComponents();
        EnsureBackgroundObject();
        RefreshBackground();
        SetBackgroundVisible(gameObject.activeInHierarchy);
    }

    private void OnEnable()
    {
        CacheComponents();
        EnsureBackgroundObject();
        RefreshBackground();
        SetBackgroundVisible(true);
    }

    private void LateUpdate()
    {
        RefreshBackground();
    }

    private void OnDisable()
    {
        SetBackgroundVisible(false);
    }

    private void OnDestroy()
    {
        if (backgroundRect == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(backgroundRect.gameObject);
        }
        else
        {
            DestroyImmediate(backgroundRect.gameObject);
        }
    }

    private void OnTransformParentChanged()
    {
        EnsureBackgroundObject();
        RefreshBackground();
    }

    private void CacheComponents()
    {
        if (tooltipText == null)
        {
            tooltipText = GetComponent<TMP_Text>();
        }

        if (textRect == null)
        {
            textRect = transform as RectTransform;
        }

        if (renderUIOnTop == null)
        {
            renderUIOnTop = GetComponent<RenderUIOnTop>();
        }
    }

    private void EnsureBackgroundObject()
    {
        CacheComponents();

        if (textRect == null || textRect.parent is not RectTransform parentRect)
        {
            return;
        }

        if (backgroundRect == null)
        {
            GameObject backgroundObject = new GameObject(name + " Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Canvas));
            backgroundObject.layer = gameObject.layer;

            backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundCanvas = backgroundObject.GetComponent<Canvas>();

            backgroundRect.SetParent(parentRect, false);
            backgroundImage.sprite = GetSharedSprite();
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.raycastTarget = false;
            backgroundCanvas.overrideSorting = true;
        }
        else if (backgroundRect.parent != parentRect)
        {
            backgroundRect.SetParent(parentRect, false);
        }

        if (backgroundImage == null && backgroundRect != null)
        {
            backgroundImage = backgroundRect.GetComponent<Image>();
        }

        if (backgroundCanvas == null && backgroundRect != null)
        {
            backgroundCanvas = backgroundRect.GetComponent<Canvas>();
        }
    }

    private void RefreshBackground()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureBackgroundObject();

        if (tooltipText == null || textRect == null || backgroundRect == null || backgroundImage == null || backgroundCanvas == null || textRect.parent is not RectTransform parentRect)
        {
            return;
        }

        if (renderUIOnTop != null)
        {
            renderUIOnTop.ApplySorting();
            backgroundCanvas.sortingLayerID = renderUIOnTop.SortingLayerId;
            backgroundCanvas.sortingOrder = renderUIOnTop.SortingOrder - 1;
        }
        else
        {
            Canvas rootCanvas = GetRootCanvas();
            backgroundCanvas.sortingLayerID = rootCanvas != null ? rootCanvas.sortingLayerID : 0;
            backgroundCanvas.sortingOrder = rootCanvas != null ? rootCanvas.sortingOrder + 99 : 99;
        }

        tooltipText.ForceMeshUpdate();

        Bounds textBounds = tooltipText.textBounds;
        Vector2 textSize = new Vector2(textBounds.size.x, textBounds.size.y);

        if (textSize.x <= 0f || textSize.y <= 0f)
        {
            textSize = tooltipText.GetRenderedValues(false);
        }

        Vector2 backgroundSize = new Vector2(
            Mathf.Max(textSize.x + (padding.x * 2f), minimumSize.x),
            Mathf.Max(textSize.y + (padding.y * 2f), minimumSize.y));

        Vector3 worldCenter = textRect.TransformPoint(textBounds.center);
        Vector3 worldPivot = textRect.TransformPoint(Vector3.zero);
        Vector2 anchoredOffset = (Vector2)parentRect.InverseTransformVector(worldCenter - worldPivot);

        backgroundRect.anchorMin = textRect.anchorMin;
        backgroundRect.anchorMax = textRect.anchorMax;
        backgroundRect.pivot = textRect.pivot;
        backgroundRect.anchoredPosition = textRect.anchoredPosition + anchoredOffset;
        backgroundRect.localRotation = textRect.localRotation;
        backgroundRect.localScale = textRect.localScale;
        backgroundRect.sizeDelta = backgroundSize;

        backgroundImage.color = backgroundColor;
    }

    private void SetBackgroundVisible(bool isVisible)
    {
        if (backgroundRect != null && backgroundRect.gameObject.activeSelf != isVisible)
        {
            backgroundRect.gameObject.SetActive(isVisible);
        }
    }

    private Canvas GetRootCanvas()
    {
        Canvas[] canvases = GetComponentsInParent<Canvas>(true);

        for (int index = canvases.Length - 1; index >= 0; index--)
        {
            if (canvases[index].gameObject != (backgroundCanvas != null ? backgroundCanvas.gameObject : null))
            {
                return canvases[index];
            }
        }

        return null;
    }

    private static Sprite GetSharedSprite()
    {
        if (sharedSprite != null)
        {
            return sharedSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "TextBackgroundSprite",
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        sharedSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        sharedSprite.name = "TextBackgroundSprite";
        sharedSprite.hideFlags = HideFlags.HideAndDontSave;
        return sharedSprite;
    }
}
