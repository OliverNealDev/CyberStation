using System;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance;
    
    private NavMeshSurface _navMeshManager;

    private void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        _navMeshManager = GetComponent<NavMeshSurface>();
        if (_navMeshManager == null)
        {
            Debug.LogError("NavMeshSurface component not found on this GameObject. Please add one.");
        }
        else
        {
            BuildNavMesh();
        }
    }
        
    public void BuildNavMesh()
    {
        if (_navMeshManager != null)
        {
            _navMeshManager.BuildNavMesh();
        }
        else
        {
            Debug.LogError("Cannot build NavMesh because NavMeshSurface component is missing.");
        }
    }
}
