using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    
    private GridCell[,] grid; 
    public int width = 128;
    public int height = 128; 
    public float cellSize = 2f;
    
    void Awake()
    {
        Instance = this;
        grid = new GridCell[width, height];
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition) 
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int z = Mathf.FloorToInt(worldPosition.z / cellSize);
        return new Vector2Int(x, z);
    }
    
    public Vector3 GetWorldPositionCenter(int x, int z, float worldY = 0f) 
    {
        return new Vector3(x * cellSize + (cellSize * 0.5f), worldY, z * cellSize + (cellSize * 0.5f));
    }

    public Vector3 GetWorldPositionForArea(int startX, int startZ, int areaWidth, int areaHeight, float worldY = 0f)
    {
        float centerX = (startX + (areaWidth * 0.5f)) * cellSize;
        float centerZ = (startZ + (areaHeight * 0.5f)) * cellSize;
        return new Vector3(centerX, worldY, centerZ);
    }

    public bool IsAreaWithinBounds(int startX, int startZ, int areaWidth, int areaHeight)
    {
        if (areaWidth <= 0 || areaHeight <= 0)
        {
            return false;
        }

        return startX >= 0 &&
               startZ >= 0 &&
               startX + areaWidth <= width &&
               startZ + areaHeight <= height;
    }

    public bool IsTileFree(int x, int z)
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            return !grid[x, z].isOccupied; 
        }
        return false; 
    }

    public void OccupyTile(int x, int z) 
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            grid[x, z].occupancyCount++;
        }
    }
    
    public void VacateTile(int x, int z) 
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            if (grid[x, z].occupancyCount > 0)
            {
                grid[x, z].occupancyCount--;
            }
        }
    }

    public bool IsAreaFree(int startX, int startZ, int areaWidth, int areaHeight)
    {
        if (!IsAreaWithinBounds(startX, startZ, areaWidth, areaHeight))
        {
            return false;
        }

        for (int x = 0; x < areaWidth; x++)
        {
            for (int z = 0; z < areaHeight; z++)
            {
                if (!IsTileFree(startX + x, startZ + z)) return false;
            }
        }
        return true;
    }

    public void OccupyArea(int startX, int startZ, int areaWidth, int areaHeight)
    {
        for (int x = 0; x < areaWidth; x++)
        {
            for (int z = 0; z < areaHeight; z++)
            {
                OccupyTile(startX + x, startZ + z);
            }
        }
    }

    public void VacateArea(int startX, int startZ, int areaWidth, int areaHeight)
    {
        for (int x = 0; x < areaWidth; x++)
        {
            for (int z = 0; z < areaHeight; z++)
            {
                VacateTile(startX + x, startZ + z);
            }
        }
    }
    
    [System.Serializable]
    public struct GridCell
    {
        public int occupancyCount;
        public bool isOccupied => occupancyCount > 0;
    }
}
