using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
                
                currentDemolishTarget = null;
                demolishHighlightBox.SetActive(false);
            }
        }
    }

    private void HandleBuildPreview()
    {
        if (previewObjectInstance == null && selectedPreviewObject != null)
        {
            previewObjectInstance = Instantiate(selectedPreviewObject);
            previewObjectInstance.SetActive(false); 
        }

        if (previewObjectInstance == null)
        {
            ClearPreviewPlacement();
            return;
        }

        if (TryGetBuildSurfaceHit(out RaycastHit hitInfo))
        {
            if (!previewObjectInstance.activeSelf)
            {
                previewObjectInstance.SetActive(true);
            }

            currentPreviewSize = GetRotatedSize();
            currentPreviewGridPos = GridManager.Instance.GetGridPosition(hitInfo.point);
            hasPreviewPlacement = true;

            Vector3 previewPosition = GridManager.Instance.GetWorldPositionForArea(
                currentPreviewGridPos.x,
                currentPreviewGridPos.y,
                currentPreviewSize.x,
                currentPreviewSize.y,
                hitInfo.point.y);

            previewObjectInstance.transform.SetPositionAndRotation(
                previewPosition,
                Quaternion.Euler(0, objectRotation, 0));

            currentPreviewPlacementValid = CanPlaceAt(currentPreviewGridPos, currentPreviewSize, hitInfo.point.y);
        }
        else
        {
            ClearPreviewPlacement();
        }
    }

    private bool TryGetBuildSurfaceHit(out RaycastHit hitInfo)
    {
        hitInfo = default;

        if (Camera.main == null || Mouse.current == null || IsPointerOverUI())
        {
            return false;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out hitInfo, Mathf.Infinity, buildFloorLayerMask))
        {
            return false;
        }

        return hitInfo.collider.CompareTag("BuildableFlooring") && hitInfo.normal.y > 0.9f;
    }

    private void TryPlaceCurrentPreview()
    {
        if (previewObjectInstance == null ||
            !previewObjectInstance.activeSelf ||
            !hasPreviewPlacement ||
            !currentPreviewPlacementValid ||
            objectBuildable == null ||
            EconomyManager.Instance.money < objectBuildable.cost)
        {
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

    private bool IsFootprintOnFloor(Vector2Int startGridPos, Vector2Int size, float currentY)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector3 tileCenter = GridManager.Instance.GetWorldPositionCenter(startGridPos.x + x, startGridPos.y + z, currentY);
                
                Ray ray = new Ray(tileCenter + Vector3.up * 5f, Vector3.down);
                
                if (Physics.Raycast(ray, out RaycastHit hit, 10f, buildFloorLayerMask))
                {
                    if (!hit.collider.CompareTag("BuildableFlooring") || hit.normal.y < 0.9f)
                    {
                        return false;
                    }
                }
                else
                {
                    return false; 
                }
            }
        }
        return true;
    }

    private bool CanPlaceAt(Vector2Int gridPos, Vector2Int size, float worldY)
    {
        return GridManager.Instance.IsAreaFree(gridPos.x, gridPos.y, size.x, size.y) &&
               IsFootprintOnFloor(gridPos, size, worldY);
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

    public void ChangePreviewObject(ObjectBuildable objectBuildable)
    {
        isBuildingMode = true;
        
        if (previewObjectInstance != null)
        {
            Destroy(previewObjectInstance);
            previewObjectInstance = null;
        }

        selectedPreviewObject = objectBuildable.prefab;
        this.objectBuildable = objectBuildable;
        ClearPreviewPlacement();
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
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
