using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BuildController : MonoBehaviour
{
    public static BuildController Instance;

    public Material gridMaterial;
    public Material plainMaterial;

    [SerializeField] private bool _isBuildingMode = false;
    public bool isBuildingMode
    {
        get { return _isBuildingMode; }
        set
        {
            if (_isBuildingMode != value)
            {
                _isBuildingMode = value;
                UpdateFlooringMaterials(_isBuildingMode);
                
                if (!_isBuildingMode)
                {
                    RemovePreviewObject();
                }
            }
        }
    }

    public GameObject selectedPreviewObject;
    
    private GameObject previewObjectInstance;
    private ObjectBuildable objectBuildable; 

    private int objectRotation = 0; 

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateFlooringMaterials(_isBuildingMode);
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject()) 
        {
            if (previewObjectInstance != null && previewObjectInstance.activeSelf)
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
                    
                    GridManager.Instance.OccupyArea(gridPos.x, gridPos.y, size.x, size.y);
                }
            }
        }
        
        if (Keyboard.current.rKey.wasPressedThisFrame)
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
                
                if (Physics.Raycast(ray, out RaycastHit hitInfo) && !EventSystem.current.IsPointerOverGameObject()) 
                {
                    if (hitInfo.collider.CompareTag("BuildableFlooring"))
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
        else
        {
            if (previewObjectInstance != null)
            {
                Destroy(previewObjectInstance);
            }
        }
    }

    private bool IsFootprintOnFloor(Vector2Int startGridPos, Vector2Int size, float currentY)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector3 tileCenter = GridManager.Instance.GetWorldPositionCenter(startGridPos.x + x, startGridPos.y + z, currentY);
                
                Ray ray = new Ray(tileCenter + Vector3.up * 5f, Vector3.down);
                
                if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                {
                    if (!hit.collider.CompareTag("BuildableFlooring"))
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
        if (previewObjectInstance != null)
        {
            Destroy(previewObjectInstance);
        }

        selectedPreviewObject = objectBuildable.prefab;
        this.objectBuildable = objectBuildable;
    }
}