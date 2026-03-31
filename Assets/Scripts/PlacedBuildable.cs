using UnityEngine;

public class PlacedBuildable : MonoBehaviour
{
    public int cost;
    public Vector2Int gridPos;
    public Vector2Int size;
    public float decorationStrength;

    public bool HasDecoration => decorationStrength > 0f;

    public void Initialize(ObjectBuildable buildable, Vector2Int position, Vector2Int buildSize, int placedCost)
    {
        cost = Mathf.Max(0, placedCost);
        gridPos = position;
        size = buildSize;
        decorationStrength = buildable != null ? Mathf.Max(0f, buildable.decorationStrength) : 0f;
    }
}
