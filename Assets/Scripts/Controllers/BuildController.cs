using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildController : MonoBehaviour
{
    public static BuildController Instance;

    public Material gridMaterial;
    public Material plainMaterial;
    public Material demolishHighlightMaterial; 

    public LayerMask buildFloorLayerMask; 
    public LayerMask demolishLayerMask;

    public float highlightPadding = 0.1f;

    [SerializeField] private bool _isBuildingMode = false;
    public bool isBuildingMode
    {
        get { return _isBuildingMode; }
        set
        {
            if (_isBuildingMode != value)
            {
                _isBuildingMode = value;
                if (_isBuildingMode)
                {
                    isDemolishMode = false;
                    UpdateFlooringMaterials(true);
                }
                else
                {
                    RemovePreviewObject();
                    if (!isDemolishMode) UpdateFlooringMaterials(false);
                }
            }
        }
    }

    [SerializeField] private bool _isDemolishMode = false;
    public bool isDemolishMode
    {
        get { return _isDemolishMode; }
        set
        {
            if (_isDemolishMode != value)
            {
                _isDemolishMode = value;
                if (_isDemolishMode)
                {
                    isBuildingMode = false;
                    UpdateFlooringMaterials(true);
                }
                else
                {
                    if (currentDemolishTarget != null)
                    {
                        currentDemolishTarget = null;
                        demolishHighlightBox.SetActive(false);
                    }
                    if (!isBuildingMode) UpdateFlooringMaterials(false);
                }
            }
        }
    }

    public GameObject selectedPreviewObject;
    public bool HasBuildSelection => objectBuildable != null && selectedPreviewObject != null;
    
    private GameObject previewObjectInstance;
    private ObjectBuildable objectBuildable; 
    private PlacedBuildable currentDemolishTarget;
    private GameObject demolishHighlightBox;

    private int objectRotation = 0; 
    private float defaultHighlightHeight = 5f;
    private Vector2Int currentPreviewGridPos;
    private Vector2Int currentPreviewSize = Vector2Int.one;
    private bool hasPreviewPlacement;
    private bool currentPreviewPlacementValid;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateFlooringMaterials(_isBuildingMode || _isDemolishMode);
        CreateDemolishHighlightBox();
    }

    void Update()
    {
        if (isBuildingMode)
        {
            HandleBuildPreview();

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                objectRotation += 90;
                if (objectRotation >= 360) objectRotation = 0;
                HandleBuildPreview();
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI()) 
            {
                TryPlaceCurrentPreview();
            }
        }
        else if (isDemolishMode)
        {
            UpdateDemolishTarget();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI() && currentDemolishTarget != null)
            {
                EconomyManager.Instance.AddMoney(currentDemolishTarget.cost);
                GridManager.Instance.VacateArea(currentDemolishTarget.gridPos.x, currentDemolishTarget.gridPos.y, currentDemolishTarget.size.x, currentDemolishTarget.size.y);
                Destroy(currentDemolishTarget.gameObject);
                SoundEffectController.Play(SoundEffectId.Demolish);
                
                currentDemolishTarget = null;
                demolishHighlightBox.SetActive(false);
            }
        }
    }

    private void HandleBuildPreview()
    {
        EnsurePreviewObject();

        if (previewObjectInstance == null)
        {
            ClearPreviewPlacement();
            return;
        }

        if (!TryGetBuildPreviewPlacement(out BuildPreviewPlacement placement))
        {
            ClearPreviewPlacement();
            return;
        }

        currentPreviewGridPos = placement.gridPos;
        currentPreviewSize = placement.size;
        hasPreviewPlacement = true;
        currentPreviewPlacementValid = placement.isValid;

        ApplyPreviewTransform(placement.gridPos, placement.size, placement.surfaceY);
    }

    private bool TryGetBuildSurfaceHit(out RaycastHit hitInfo)
    {
        hitInfo = default;

        if (Camera.main == null || Mouse.current == null || IsPointerOverUI())
        {
            return false;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out hitInfo, Mathf.Infinity, buildFloorLayerMask, QueryTriggerInteraction.Ignore) &&
               IsBuildSurface(hitInfo);
    }

    private void TryPlaceCurrentPreview()
    {
        if (objectBuildable == null)
        {
            return;
        }

        if (previewObjectInstance == null ||
            !previewObjectInstance.activeSelf ||
            !hasPreviewPlacement ||
            !currentPreviewPlacementValid)
        {
            SoundEffectController.Play(SoundEffectId.BuildInvalid);
            return;
        }

        if (EconomyManager.Instance == null || EconomyManager.Instance.money < objectBuildable.cost)
        {
            SoundEffectController.Play(SoundEffectId.BuildInvalid);
            return;
        }

        EconomyManager.Instance.SpendMoney(objectBuildable.cost);
        
        GameObject placedObject = Instantiate(selectedPreviewObject, previewObjectInstance.transform.position, previewObjectInstance.transform.rotation);
        PreviewableObject previewableObject = placedObject.GetComponent<PreviewableObject>();
        if (previewableObject != null)
        {
            previewableObject.ExitPreviewMode(objectBuildable.cost);
        }
        
        PlacedBuildable pb = placedObject.AddComponent<PlacedBuildable>();
        pb.Initialize(objectBuildable, currentPreviewGridPos, currentPreviewSize);

        GridManager.Instance.OccupyArea(currentPreviewGridPos.x, currentPreviewGridPos.y, currentPreviewSize.x, currentPreviewSize.y);
        SoundEffectController.Play(SoundEffectId.BuildPlaced);

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.RecordBuildPlaced();
        }
    }

    private void UpdateDemolishTarget()
    {
        if (Camera.main == null || Mouse.current == null || IsPointerOverUI())
        {
            ClearDemolishTarget();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, demolishLayerMask))
        {
            PlacedBuildable hitTarget = hitInfo.collider.GetComponentInParent<PlacedBuildable>();

            if (hitTarget != null)
            {
                if (currentDemolishTarget != hitTarget)
                {
                    currentDemolishTarget = hitTarget;
                    UpdateDemolishHighlight();
                }
                return;
            }
        }

        ClearDemolishTarget();
    }

    private void ClearDemolishTarget()
    {
        if (currentDemolishTarget != null)
        {
            currentDemolishTarget = null;
        }

        if (demolishHighlightBox != null)
        {
            demolishHighlightBox.SetActive(false);
        }
    }

    private void CreateDemolishHighlightBox()
    {
        demolishHighlightBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(demolishHighlightBox.GetComponent<Collider>());
        demolishHighlightBox.GetComponent<Renderer>().material = demolishHighlightMaterial;
        demolishHighlightBox.SetActive(false);
    }

    private void UpdateDemolishHighlight()
    {
        if (currentDemolishTarget == null) return;

        demolishHighlightBox.SetActive(true);
        
        float cellSize = GridManager.Instance.cellSize;
        
        float width = (currentDemolishTarget.size.x * cellSize) + highlightPadding;
        float depth = (currentDemolishTarget.size.y * cellSize) + highlightPadding;
        float height = defaultHighlightHeight + highlightPadding;

        demolishHighlightBox.transform.localScale = new Vector3(width, height, depth);
        
        Vector3 targetPos = currentDemolishTarget.transform.position;
        demolishHighlightBox.transform.position = new Vector3(targetPos.x, targetPos.y + (height / 2f) - (highlightPadding / 2f), targetPos.z);
        
        demolishHighlightBox.transform.rotation = Quaternion.identity;
    }

    private bool TryGetTileSurfaceHit(Vector2Int gridPos, float approximateWorldY, out RaycastHit hitInfo)
    {
        Vector3 tileCenter = GridManager.Instance.GetWorldPositionCenter(gridPos.x, gridPos.y, approximateWorldY);
        Ray ray = new Ray(tileCenter + Vector3.up * 5f, Vector3.down);
        return Physics.Raycast(ray, out hitInfo, 10f, buildFloorLayerMask, QueryTriggerInteraction.Ignore) &&
               IsBuildSurface(hitInfo);
    }

    private bool IsBuildSurface(RaycastHit hitInfo)
    {
        return hitInfo.collider != null &&
               hitInfo.collider.CompareTag("BuildableFlooring");
    }

    private Vector2Int GetRotatedSize()
    {
        if (objectBuildable == null) return Vector2Int.one;
        
        if (objectRotation == 90 || objectRotation == 270)
        {
            return new Vector2Int(objectBuildable.size.y, objectBuildable.size.x);
        }
        return objectBuildable.size;
    }

    private void UpdateFlooringMaterials(bool buildModeActive)
    {
        GameObject[] floorTiles = GameObject.FindGameObjectsWithTag("BuildableFlooring");
        Material targetMaterial = buildModeActive ? gridMaterial : plainMaterial;

        foreach (GameObject tile in floorTiles)
        {
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material = targetMaterial;
            }
        }
    }
    
    public void RemovePreviewObject()
    {
        if (previewObjectInstance != null)
        {
            Destroy(previewObjectInstance);
            previewObjectInstance = null;
        }

        ClearPreviewPlacement();
    }

    public void ExitBuildModes()
    {
        isBuildingMode = false;
        isDemolishMode = false;
    }

    public void ResumeBuildMode()
    {
        isBuildingMode = HasBuildSelection;
    }

    public void ChangePreviewObject(ObjectBuildable objectBuildable)
    {
        if (objectBuildable == null || objectBuildable.prefab == null)
        {
            return;
        }
        
        if (previewObjectInstance != null)
        {
            Destroy(previewObjectInstance);
            previewObjectInstance = null;
        }

        selectedPreviewObject = objectBuildable.prefab;
        this.objectBuildable = objectBuildable;
        ClearPreviewPlacement();
        isBuildingMode = true;
    }

    private void ClearPreviewPlacement()
    {
        hasPreviewPlacement = false;
        currentPreviewPlacementValid = false;

        if (previewObjectInstance != null && previewObjectInstance.activeSelf)
        {
            previewObjectInstance.SetActive(false);
        }
    }

    private bool IsPointerOverUI()
    {
        return PointerUiUtility.IsPointerOverBlockingUi();
    }

    private void EnsurePreviewObject()
    {
        if (previewObjectInstance != null || selectedPreviewObject == null)
        {
            return;
        }

        previewObjectInstance = Instantiate(selectedPreviewObject);

        PreviewableObject previewableObject = previewObjectInstance.GetComponent<PreviewableObject>();
        if (previewableObject != null)
        {
            previewableObject.EnterPreviewMode();
        }

        previewObjectInstance.SetActive(false);
    }

    private bool TryGetBuildPreviewPlacement(out BuildPreviewPlacement placement)
    {
        placement = default;

        if (!TryGetBuildSurfaceHit(out RaycastHit cursorHit))
        {
            return false;
        }

        Vector2Int size = GetRotatedSize();
        Vector2Int gridPos = GridManager.Instance.GetGridPosition(cursorHit.point);

        if (!GridManager.Instance.IsAreaWithinBounds(gridPos.x, gridPos.y, size.x, size.y))
        {
            return false;
        }

        if (!TryGetFootprintSurfaceY(gridPos, size, cursorHit.point.y, out float surfaceY))
        {
            return false;
        }

        placement = new BuildPreviewPlacement
        {
            gridPos = gridPos,
            size = size,
            surfaceY = surfaceY,
            isValid = GridManager.Instance.IsAreaFree(gridPos.x, gridPos.y, size.x, size.y)
        };

        return true;
    }

    private void ApplyPreviewTransform(Vector2Int gridPos, Vector2Int size, float worldY)
    {
        if (previewObjectInstance == null)
        {
            return;
        }

        Vector3 previewPosition = GridManager.Instance.GetWorldPositionForArea(
            gridPos.x,
            gridPos.y,
            size.x,
            size.y,
            worldY);

        previewObjectInstance.transform.SetPositionAndRotation(
            previewPosition,
            Quaternion.Euler(0, objectRotation, 0));

        if (!previewObjectInstance.activeSelf)
        {
            previewObjectInstance.SetActive(true);
        }
    }

    private bool TryGetFootprintSurfaceY(Vector2Int startGridPos, Vector2Int size, float approximateWorldY, out float surfaceY)
    {
        surfaceY = 0f;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                if (!TryGetTileSurfaceHit(new Vector2Int(startGridPos.x + x, startGridPos.y + z), approximateWorldY, out RaycastHit tileHit))
                {
                    return false;
                }

                if (x == 0 && z == 0)
                {
                    surfaceY = tileHit.point.y;
                }
            }
        }

        return true;
    }

    private struct BuildPreviewPlacement
    {
        public Vector2Int gridPos;
        public Vector2Int size;
        public float surfaceY;
        public bool isValid;
    }
}

public static class PointerUiUtility
{
    private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();
    private static EventSystem cachedEventSystem;
    private static PointerEventData pointerEventData;

    public static bool IsPointerOverBlockingUi()
    {
        if (EventSystem.current == null || Mouse.current == null)
        {
            return false;
        }

        if (pointerEventData == null || cachedEventSystem != EventSystem.current)
        {
            cachedEventSystem = EventSystem.current;
            pointerEventData = new PointerEventData(EventSystem.current);
        }

        pointerEventData.position = Mouse.current.position.ReadValue();

        RaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, RaycastResults);

        for (int i = 0; i < RaycastResults.Count; i++)
        {
            if (ShouldBlockPointer(RaycastResults[i].gameObject))
            {
                return true;
            }
        }

        return false;
    }

    public static void DisableRaycastTargets(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].raycastTarget = false;
            }
        }
    }

    public static void DisableWorldSpaceCanvasInteraction(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        DisableRaycastTargets(root);

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            {
                continue;
            }

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
        }
    }

    private static bool ShouldBlockPointer(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return true;
        }

        return canvas.renderMode != RenderMode.WorldSpace;
    }
}
