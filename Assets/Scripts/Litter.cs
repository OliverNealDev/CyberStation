using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Litter : MonoBehaviour
{
    public float timeToClean = 5f;

    [SerializeField] private GameObject interactablePromptPrefab;
    [SerializeField] private float manualCleanHoldDuration = 3f;
    [SerializeField] private float manualCleanShrinkDuration = 0.2f;
    [SerializeField] private float manualCleanMaxCursorDistance = 4f;
    [SerializeField] private string hoverPromptText = "[LMB] Hold";
    [SerializeField] private string activePromptText = "Cleaning...";
    [SerializeField] private float hoverPromptHeight = 1.1f;
    [SerializeField] private float fallbackColliderRadius = 0.7f;

    private static Litter hoveredLitter;
    private static int hoveredLitterFrame = -1;
    private static Litter heldLitter;

    private float heldCleanTime;
    private bool isCleaning;
    private GameObject hoverPromptRoot;
    private RectTransform hoverPromptRect;
    private TaskProgressBarController hoverPrompt;

    private void Awake()
    {
        EnsureInteractableCollider();
        CreateHoverPrompt();
    }

    private void Start()
    {
        if (JanitorCoordinator.Instance != null)
        {
            JanitorCoordinator.Instance.ReportLitter(this);
        }
    }

    private void Update()
    {
        bool isHeldByPlayer = !isCleaning && IsHeldByPlayer();
        bool isHovered = !isCleaning && IsHoveredByPlayer();

        if (!isHeldByPlayer && isHovered && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            heldLitter = this;
            isHeldByPlayer = true;
        }

        bool canShowPrompt = (isHovered || isHeldByPlayer) && UpdatePromptScreenPosition();
        SetPromptVisible(canShowPrompt);

        if (!canShowPrompt)
        {
            heldCleanTime = 0f;
            ShowIdlePrompt();
            return;
        }

        if (isHeldByPlayer && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            heldCleanTime += Time.deltaTime;
            ShowActivePrompt(heldCleanTime);
            if (heldCleanTime >= manualCleanHoldDuration)
            {
                BeginCleanup(manualCleanShrinkDuration);
            }
        }
        else
        {
            heldCleanTime = 0f;
            ShowIdlePrompt();
        }
    }

    private void OnDisable()
    {
        if (heldLitter == this)
        {
            heldLitter = null;
        }

        SetPromptVisible(false);
    }

    private void OnDestroy()
    {
        if (JanitorCoordinator.Instance != null)
        {
            JanitorCoordinator.Instance.ResolveClean(this);
        }

        if (hoveredLitter == this)
        {
            hoveredLitter = null;
        }

        if (heldLitter == this)
        {
            heldLitter = null;
        }
    }

    public bool BeginCleanup(float cleanDuration)
    {
        if (isCleaning)
        {
            return false;
        }

        StartCoroutine(CleanupRoutine(cleanDuration));
        return true;
    }

    private IEnumerator CleanupRoutine(float cleanDuration)
    {
        isCleaning = true;
        heldCleanTime = 0f;
        if (heldLitter == this)
        {
            heldLitter = null;
        }
        SetPromptVisible(false);
        SetCollidersEnabled(false);

        if (JanitorCoordinator.Instance != null)
        {
            JanitorCoordinator.Instance.ResolveClean(this);
        }

        Vector3 startScale = transform.localScale;
        float duration = Mathf.Max(0.01f, cleanDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.zero;
        Destroy(gameObject);
    }

    private bool IsHoveredByPlayer()
    {
        RefreshHoveredLitter();
        return hoveredLitter == this;
    }

    private bool IsHeldByPlayer()
    {
        if (heldLitter != this)
        {
            return false;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.isPressed || !IsCursorWithinManualCleanDistance())
        {
            heldLitter = null;
            return false;
        }

        return true;
    }

    private bool IsCursorWithinManualCleanDistance()
    {
        if (Camera.main == null || Mouse.current == null)
        {
            return false;
        }

        float maxCursorDistance = Mathf.Max(0f, manualCleanMaxCursorDistance);
        if (maxCursorDistance <= 0f)
        {
            return true;
        }

        Ray cursorRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 litterPoint = GetManualCleanAnchorPoint();
        Vector3 rayToPoint = litterPoint - cursorRay.origin;
        float projectedDistance = Mathf.Max(0f, Vector3.Dot(rayToPoint, cursorRay.direction));
        Vector3 closestPointOnRay = cursorRay.origin + (cursorRay.direction * projectedDistance);
        return Vector3.Distance(litterPoint, closestPointOnRay) <= maxCursorDistance;
    }

    private Vector3 GetManualCleanAnchorPoint()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }

            return combinedBounds.center;
        }

        return transform.position;
    }

    private static void RefreshHoveredLitter()
    {
        if (hoveredLitterFrame == Time.frameCount)
        {
            return;
        }

        hoveredLitterFrame = Time.frameCount;
        hoveredLitter = null;

        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        if (PointerUiUtility.IsPointerOverBlockingUi())
        {
            return;
        }

        if (BuildController.Instance != null && BuildController.Instance.IsAnyBuildModeActive)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            hoveredLitter = hit.collider.GetComponentInParent<Litter>();
        }
    }

    private void CreateHoverPrompt()
    {
        if (interactablePromptPrefab == null)
        {
            return;
        }

        Transform promptParent = FindPromptParent();
        if (promptParent == null)
        {
            return;
        }

        GameObject promptInstance = Instantiate(interactablePromptPrefab, promptParent);
        promptInstance.transform.localScale = Vector3.one;

        hoverPromptRoot = promptInstance;
        hoverPromptRect = promptInstance.transform as RectTransform;

        MoveAndFade moveAndFade = promptInstance.GetComponent<MoveAndFade>();
        if (moveAndFade != null)
        {
            moveAndFade.enabled = false;
        }

        hoverPrompt = promptInstance.GetComponent<TaskProgressBarController>();
        if (hoverPrompt == null)
        {
            hoverPrompt = promptInstance.AddComponent<TaskProgressBarController>();
        }

        ShowIdlePrompt();
        SetPromptVisible(false);
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (hoverPromptRoot != null && hoverPromptRoot.activeSelf != isVisible)
        {
            hoverPromptRoot.SetActive(isVisible);
        }
    }

    private void EnsureInteractableCollider()
    {
        if (GetComponentInChildren<Collider>(true) != null)
        {
            return;
        }

        SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            sphereCollider.radius = fallbackColliderRadius;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        sphereCollider.center = transform.InverseTransformPoint(bounds.center);

        float largestScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        sphereCollider.radius = largestScale > 0f
            ? Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) / largestScale
            : fallbackColliderRadius;
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider litterCollider in colliders)
        {
            litterCollider.enabled = isEnabled;
        }
    }

    private void ShowIdlePrompt()
    {
        if (hoverPrompt != null)
        {
            hoverPrompt.ShowIdle(hoverPromptText);
        }
    }

    private void ShowActivePrompt(float heldDuration)
    {
        if (hoverPrompt != null)
        {
            float normalizedProgress = manualCleanHoldDuration > 0f
                ? heldDuration / manualCleanHoldDuration
                : 1f;
            hoverPrompt.ShowProgress(activePromptText, normalizedProgress);
        }
    }

    private bool UpdatePromptScreenPosition()
    {
        if (hoverPromptRoot == null || hoverPromptRect == null || Camera.main == null)
        {
            return false;
        }

        Vector3 worldPoint = transform.position + Vector3.up * hoverPromptHeight;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);

        if (screenPoint.z <= 0f)
        {
            return false;
        }

        hoverPromptRect.position = screenPoint;
        return true;
    }

    private static Transform FindPromptParent()
    {
        GameObject taggedPromptContainer = GameObject.FindGameObjectWithTag("InteractablePrompts");
        if (taggedPromptContainer != null)
        {
            return taggedPromptContainer.transform;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.isActiveAndEnabled)
            {
                Transform namedContainer = canvas.transform.Find("InteractablePrompts");
                if (namedContainer != null)
                {
                    return namedContainer;
                }
            }
        }

        return null;
    }
}
