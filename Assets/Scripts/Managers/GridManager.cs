using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    
    private GridCell[,,] grid; // 3-Dimensional array to hold grid cells
    public int width = 128;
    public int height = 128;
    public int floors = 1;
    public float cellSize = 2f;
    
    void Awake()
    {
        Instance = this;
        grid = new GridCell[width, floors, height];
    }

    void Update()
    {
        
    }
    
    public Vector3Int GetGridPosition(Vector3 worldPosition) // Converts world position to grid coordinates
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int y = Mathf.FloorToInt(worldPosition.y / cellSize);
        int z = Mathf.FloorToInt(worldPosition.z / cellSize);
        return new Vector3Int(x, y, z);
    }
    
    public Vector3 GetWorldPositionCenter(int x, int y, int z) // Converts grid coordinates to world position (center of cell)
    {
        return new Vector3(x * cellSize, y * cellSize, z * cellSize) + new Vector3(cellSize * 0.5f, cellSize * 0.5f, cellSize * 0.5f);
    }

    public bool IsTileFree(int x, int y, int z)
    {
        if (x >= 0 && y >= 0 && x < width && y < floors && z >= 0 && z < height)
        {
            return !grid[x, y, z].isOccupied; 
        }
        else
        {
            return false;
        }
    }

    public void OccupyTile(int x, int y, int z) // Marks a tile as occupied
    {
        if (x >= 0 && y >= 0 && x < width && y < floors && z >= 0 && z < height)
        {
            grid[x, y, z].isOccupied = true;
        }
    }
    
    public void VacateTile(int x, int y, int z) // Marks a tile as free
    {
        if (x >= 0 && y >= 0 && x < width && y < height && z >= 0 && z < floors)
        {
            grid[x, y, z].isOccupied = false;
            grid[x, y, z].occupyingObject = null;
        }
    }
    
    [System.Serializable]
    public struct GridCell
    {
        public bool isOccupied;
        public GameObject occupyingObject;
        public enum OccupationType
        {
            Floor,
            Object
        }
    }
}
