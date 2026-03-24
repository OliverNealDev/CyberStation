using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Litter : MonoBehaviour
{
    public float timeToClean = 5f;

    [SerializeField] private GameObject interactablePromptPrefab;
    [SerializeField] private float manualCleanHoldDuration = 3f;
    [SerializeField] private float manualCleanShrinkDuration = 0.2f;
    [SerializeField] private string hoverPromptText = "Hold LMB to Clean";
    [SerializeField] private float hoverPromptHeight = 1.1f;
    [SerializeField] private float fallbackColliderRadius = 0.7f;

    private static Litter hoveredLitter;
    private static int hoveredLitterFrame = -1;

    private float heldCleanTime;
    private bool isCleaning;
    private GameObject hoverPromptRoot;
    private RectTransform hoverPromptRect;
    private TextMeshProUGUI hoverPrompt;

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
        bool isHovered = !isCleaning && IsHoveredByPlayer();
        bool canShowPrompt = isHovered && UpdatePromptScreenPosition();
        SetPromptVisible(canShowPrompt);

        if (!canShowPrompt)
        {
            heldCleanTime = 0f;
            UpdatePromptText(hoverPromptText);
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            heldCleanTime += Time.deltaTime;
            UpdatePromptText(GetCountdownPromptText());
            if (heldCleanTime >= manualCleanHoldDuration)
            {
                BeginCleanup(manualCleanShrinkDuration);
            }
        }
        else
        {
            heldCleanTime = 0f;
            UpdatePromptText(hoverPromptText);
        }
    }

    private void OnDisable()
    {
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

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (BuildController.Instance != null && (BuildController.Instance.isBuildingMode || BuildController.Instance.isDemolishMode))
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

        hoverPrompt = promptInstance.GetComponentInChildren<TextMeshProUGUI>(true);
        if (hoverPrompt != null)
        {
            hoverPrompt.alignment = TextAlignmentOptions.Center;
            hoverPrompt.color = Color.white;
            hoverPrompt.textWrappingMode = TextWrappingModes.NoWrap;
            UpdatePromptText(hoverPromptText);
        }

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

    private string GetCountdownPromptText()
    {
        float remaining = Mathf.Max(0f, manualCleanHoldDuration - heldCleanTime);
        float roundedRemaining = Mathf.Ceil(remaining * 10f) / 10f;
        return $"{hoverPromptText} ({roundedRemaining:0.0}s)";
    }

    private void UpdatePromptText(string promptText)
    {
        if (hoverPrompt != null)
        {
            hoverPrompt.text = promptText;
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
