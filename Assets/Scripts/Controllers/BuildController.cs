using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildController : MonoBehaviour
{
    public bool isBuildingMode = false;
    private GameObject currentPreviewObject;
    
    // Tile-centered objects
    [SerializeField] private GameObject ticketMachinePrefab;
    [SerializeField] private GameObject ticketBarrierPrefab;
    
    // Inter-Tile objects
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject railingPrefab;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            rayTest();
        }
    }
    
    private void rayTest()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Vector3 buildPosition = hitInfo.point;
            Vector3Int gridPos = GridManager.Instance.GetGridPosition(buildPosition);
            Debug.Log(gridPos);
            Debug.Log(GridManager.Instance.GetWorldPositionCenter(gridPos.x, gridPos.y, gridPos.z));
        }
    }
}