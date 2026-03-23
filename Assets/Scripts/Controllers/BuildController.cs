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
        if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject()) 
        {
            if (isBuildingMode && previewObjectInstance != null && previewObjectInstance.activeSelf)
            {
                Vector2Int gridPos = GridManager.Instance.GetGridPosition(previewObjectInstance.transform.position);
                Vector2Int size = GetRotatedSize();
                
                if (GridManager.Instance.IsAreaFree(gridPos.x, gridPos.y, size.x, size.y) &&
                    IsFootprintOnFloor(gridPos, size, previewObjectInstance.transform.position.y) &&
                    EconomyManager.Instance.money >= objectBuildable.cost)
                {
                    EconomyManager.Instance.SpendMoney(objectBuildable.cost);
                    
                    GameObject placedObject = Instantiate(selectedPreviewObject, previewObjectInstance.transform.position, previewObjectInstance.transform.rotation);
                    placedObject.GetComponent<PreviewableObject>().ExitPreviewMode(objectBuildable.cost); 
                    
                    PlacedBuildable pb = placedObject.AddComponent<PlacedBuildable>();
                    pb.Initialize(objectBuildable.cost, gridPos, size);

                    GridManager.Instance.OccupyArea(gridPos.x, gridPos.y, size.x, size.y);
                }
            }
            else if (isDemolishMode && currentDemolishTarget != null)
            {
                EconomyManager.Instance.AddMoney(currentDemolishTarget.cost);
                GridManager.Instance.VacateArea(currentDemolishTarget.gridPos.x, currentDemolishTarget.gridPos.y, currentDemolishTarget.size.x, currentDemolishTarget.size.y);
                Destroy(currentDemolishTarget.gameObject);
                
                currentDemolishTarget = null;
                demolishHighlightBox.SetActive(false);
            }
        }
        
        if (isBuildingMode && Keyboard.current.rKey.wasPressedThisFrame)
        {
            objectRotation += 90;
            if (objectRotation >= 360) objectRotation = 0;
            if (previewObjectInstance != null)
            {
                previewObjectInstance.transform.rotation = Quaternion.Euler(0, objectRotation, 0);
            }
        }
    }

    void FixedUpdate()
    {
        if (isBuildingMode)
        {
            if (previewObjectInstance == null && selectedPreviewObject != null)
            {
                previewObjectInstance = Instantiate(selectedPreviewObject);
                previewObjectInstance.SetActive(false); 
            }

            if (previewObjectInstance) 
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                
                if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, buildFloorLayerMask) && !EventSystem.current.IsPointerOverGameObject()) 
                {
                    if (hitInfo.collider.CompareTag("BuildableFlooring") && hitInfo.normal.y > 0.9f)
                    {
                        if (!previewObjectInstance.activeSelf) previewObjectInstance.SetActive(true);

                        Vector3 buildPosition = hitInfo.point;
                        Vector2Int gridPos = GridManager.Instance.GetGridPosition(buildPosition);
                        
                        previewObjectInstance.transform.position = GridManager.Instance.GetWorldPositionCenter(gridPos.x, gridPos.y, buildPosition.y);
                        previewObjectInstance.transform.rotation = Quaternion.Euler(0, objectRotation, 0);
                    }
                    else
                    {
                        if (previewObjectInstance.activeSelf) previewObjectInstance.SetActive(false);
                    }
                }
                else
                {
                    if (previewObjectInstance.activeSelf) previewObjectInstance.SetActive(false);
                }
            }
        }
        else if (isDemolishMode)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, demolishLayerMask) && !EventSystem.current.IsPointerOverGameObject())
            {
                PlacedBuildable hitTarget = hitInfo.collider.GetComponentInParent<PlacedBuildable>();

                if (hitTarget != null)
                {
                    if (currentDemolishTarget != hitTarget)
                    {
                        currentDemolishTarget = hitTarget;
                        UpdateDemolishHighlight();
                    }
                }
                else
                {
                    if (currentDemolishTarget != null)
                    {
                        currentDemolishTarget = null;
                        demolishHighlightBox.SetActive(false);
                    }
                }
            }
            else
            {
                if (currentDemolishTarget != null)
                {
                    currentDemolishTarget = null;
                    demolishHighlightBox.SetActive(false);
                }
            }
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
        }
    }

    public void ChangePreviewObject(ObjectBuildable objectBuildable)
    {
        isBuildingMode = true;
        
        if (previewObjectInstance != null)
        {
            Destroy(previewObjectInstance);
        }

        selectedPreviewObject = objectBuildable.prefab;
        this.objectBuildable = objectBuildable;
    }
}
