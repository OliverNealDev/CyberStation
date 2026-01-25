using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class BuildController : MonoBehaviour
{
    public static BuildController Instance;
    
    public bool isBuildingMode = false;
    public GameObject selectedPreviewObject;
    
    private GameObject previewObjectInstance;

    private int objectRotation = 0; // Rotation state (0, 90, 180, 270 degrees)
    
    // Tile-centered objects
    //[SerializeField] private GameObject ticketMachinePrefab;
    //[SerializeField] private GameObject ticketBarrierPrefab;
    
    // Inter-Tile objects
    //[SerializeField] private GameObject wallPrefab;
    //[SerializeField] private GameObject railingPrefab;

    private void Awake()
    {
        Instance = this;
    }
    
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (previewObjectInstance != null)
            {
                Vector3Int gridPos = GridManager.Instance.GetGridPosition(previewObjectInstance.transform.position);
                
                if (GridManager.Instance.IsTileFree(gridPos.x, gridPos.y, gridPos.z))
                {
                    Instantiate(selectedPreviewObject, previewObjectInstance.transform.position, previewObjectInstance.transform.rotation);
                    GridManager.Instance.OccupyTile(gridPos.x, gridPos.y, gridPos.z);
                }
                else
                {
                    Debug.Log("Cannot build here, tile is occupied." + " Position: " + gridPos);
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
            }

            if (previewObjectInstance) // If the preview object exists, update its position to the mouse position (tile-aligned)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hitInfo))
                {
                    Vector3 buildPosition = hitInfo.point;
                    Vector3Int gridPos = GridManager.Instance.GetGridPosition(buildPosition);
                    previewObjectInstance.transform.position = GridManager.Instance.GetWorldPositionCenter(gridPos.x, gridPos.y, gridPos.z);
                    
                    previewObjectInstance.transform.rotation = Quaternion.Euler(0, objectRotation, 0);
                }
                else
                {
                    Destroy(previewObjectInstance);
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
    
    private Vector3 GetPreviewObjectPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Vector3 buildPosition = hitInfo.point;
            Vector3Int gridPos = GridManager.Instance.GetGridPosition(buildPosition);
            return GridManager.Instance.GetWorldPositionCenter(gridPos.x, gridPos.y, gridPos.z);
        }
        else
        {
            return new Vector3(0, -1000, 0);
        }
    }
    
    
}