using UnityEngine;

public class PlacedBuildable : MonoBehaviour
{
    [SerializeField] private ObjectBuildable buildableData;
    [SerializeField] private bool isRuntimePlaced;

    public int cost;
    public Vector2Int gridPos;
    public Vector2Int size;
    public float decorationStrength;

    public bool HasDecoration => decorationStrength > 0f;
    public ObjectBuildable BuildableData => buildableData;
    public bool IsRuntimePlaced => isRuntimePlaced;

    public void Initialize(ObjectBuildable buildable, Vector2Int position, Vector2Int buildSize, int placedCost)
    {
        buildableData = buildable;
        isRuntimePlaced = true;
        cost = Mathf.Max(0, placedCost);
        gridPos = position;
        size = buildSize;
        decorationStrength = buildable != null ? Mathf.Max(0f, buildable.decorationStrength) : 0f;
    }
}
