using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    
    private GridCell[,] grid; // 2-Dimensional array to hold grid cells
    public int width = 128;
    public int height = 128; // This maps to your World Z-axis
    public float cellSize = 2f;
    
    void Awake()
    {
        Instance = this;
        grid = new GridCell[width, height];
    }

    // Converts world position to 2D grid coordinates (X, Z)
    public Vector2Int GetGridPosition(Vector3 worldPosition) 
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int z = Mathf.FloorToInt(worldPosition.z / cellSize);
        return new Vector2Int(x, z);
    }
    
    // Converts grid coordinates to world position, preserving a specific Y height
    public Vector3 GetWorldPositionCenter(int x, int z, float worldY = 0f) 
    {
        return new Vector3(x * cellSize + (cellSize * 0.5f), worldY, z * cellSize + (cellSize * 0.5f));
    }

    public bool IsTileFree(int x, int z)
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            return !grid[x, z].isOccupied; 
        }
        return false; // Out of bounds
    }

    public void OccupyTile(int x, int z) 
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            grid[x, z].isOccupied = true;
        }
    }
    
    public void VacateTile(int x, int z) 
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            grid[x, z].isOccupied = false;
        }
    }
    
    [System.Serializable]
    public struct GridCell
    {
        public bool isOccupied;
    }
}