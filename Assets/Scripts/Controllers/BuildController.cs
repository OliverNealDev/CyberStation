using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BuildController : MonoBehaviour
{
    public static BuildController Instance;

    [Header("Flooring Materials")]
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
            // Only allow building if the preview is currently visible
            if (previewObjectInstance != null && previewObjectInstance.activeSelf)
            {
                Vector2Int gridPos = GridManager.Instance.GetGridPosition(previewObjectInstance.transform.position);
                
                if (GridManager.Instance.IsTileFree(gridPos.x, gridPos.y) &&
                    EconomyManager.Instance.money >= objectBuildable.cost)
                {
                    EconomyManager.Instance.SpendMoney(objectBuildable.cost);
                    
                    GameObject placedObject = Instantiate(selectedPreviewObject, previewObjectInstance.transform.position, previewObjectInstance.transform.rotation);
                    placedObject.GetComponent<PreviewableObject>().ExitPreviewMode(objectBuildable.cost); 
                    
                    GridManager.Instance.OccupyTile(gridPos.x, gridPos.y);
                }
                else
                {
                    Debug.Log("Cannot build here, tile is occupied. Position: " + gridPos);
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
                previewObjectInstance.SetActive(false); // Start hidden
            }

            if (previewObjectInstance) 
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                
                // Standard raycast: hits everything, allowing walls to block the ray
                if (Physics.Raycast(ray, out RaycastHit hitInfo) && !EventSystem.current.IsPointerOverGameObject()) 
                {
                    // Check if the FIRST thing we hit is the floor
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
                        // We hit a wall, a prop, or something else. Hide the preview!
                        if (previewObjectInstance.activeSelf) previewObjectInstance.SetActive(false);
                    }
                }
                else
                {
                    // Raycast hit nothing
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