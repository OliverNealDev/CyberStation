using UnityEngine;

public class PlacedBuildable : MonoBehaviour
{
    public int cost;
    public Vector2Int gridPos;
    public Vector2Int size;

    public void Initialize(int buildCost, Vector2Int position, Vector2Int buildSize)
    {
        cost = buildCost;
        gridPos = position;
        size = buildSize;
    }
}