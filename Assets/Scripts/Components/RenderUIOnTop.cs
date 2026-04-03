using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class RenderUIOnTop : MonoBehaviour
{
    [SerializeField] private int sortingOrderOffset = 100;
    [FormerlySerializedAs("disableTextRaycastTarget")]
    [SerializeField] private bool disableGraphicRaycastTarget = true;
    [SerializeField] private bool moveToFrontWithinParent = true;

    private Canvas overlayCanvas;
    private Graphic graphic;
    private Coroutine moveToFrontCoroutine;

    public int SortingLayerId => overlayCanvas != null ? overlayCanvas.sortingLayerID : 0;
    public int SortingOrder => overlayCanvas != null ? overlayCanvas.sortingOrder : 0;

    private void Awake()
    {
        EnsureOverlayCanvas();
        RefreshSorting();
    }

    private void OnEnable()
    {
        EnsureOverlayCanvas();
        RefreshSorting();
        QueueMoveToFront();
    }

    private void OnTransformParentChanged()
    {
        RefreshSorting();
        QueueMoveToFront();
    }

    private void OnCanvasHierarchyChanged()
    {
        RefreshSorting();
        QueueMoveToFront();
    }

    private void OnDisable()
    {
        StopQueuedMoveToFront();
    }

    private void OnValidate()
    {
        RefreshSorting();
    }

    private void EnsureOverlayCanvas()
    {
        if (overlayCanvas == null)
        {
            overlayCanvas = GetComponent<Canvas>();
        }

        if (overlayCanvas == null)
        {
            overlayCanvas = gameObject.AddComponent<Canvas>();
        }

        if (graphic == null)
        {
            graphic = GetComponent<Graphic>();
        }
    }

    private void RefreshSorting()
    {
        EnsureOverlayCanvas();

        if (overlayCanvas == null)
        {
            return;
        }

        Canvas rootCanvas = GetRootCanvas();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingLayerID = rootCanvas != null ? rootCanvas.sortingLayerID : 0;
        overlayCanvas.sortingOrder = (rootCanvas != null ? rootCanvas.sortingOrder : 0) + sortingOrderOffset;

        if (disableGraphicRaycastTarget && graphic != null)
        {
            graphic.raycastTarget = false;
        }

    }

    public void ApplySorting()
    {
        RefreshSorting();
    }

    private Canvas GetRootCanvas()
    {
        Canvas[] parentCanvases = GetComponentsInParent<Canvas>(true);

        for (int index = parentCanvases.Length - 1; index >= 0; index--)
        {
            if (parentCanvases[index] != overlayCanvas)
            {
                return parentCanvases[index];
            }
        }

        return null;
    }

    private void QueueMoveToFront()
    {
        if (!moveToFrontWithinParent || !Application.isPlaying || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        StopQueuedMoveToFront();
        moveToFrontCoroutine = StartCoroutine(MoveToFrontNextFrame());
    }

    private IEnumerator MoveToFrontNextFrame()
    {
        yield return null;

        moveToFrontCoroutine = null;

        if (!isActiveAndEnabled || transform.parent == null)
        {
            yield break;
        }

        transform.SetAsLastSibling();
    }

    private void StopQueuedMoveToFront()
    {
        if (moveToFrontCoroutine == null)
        {
            return;
        }

        StopCoroutine(moveToFrontCoroutine);
        moveToFrontCoroutine = null;
    }
}
