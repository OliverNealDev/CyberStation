using UnityEngine;

public class PlacedBuildable : MonoBehaviour
{
    public int cost;
    public Vector2Int gridPos;
    public Vector2Int size;
    
    private Renderer[] renderers;
    private Material[][] originalMaterials;

    public void Initialize(int buildCost, Vector2Int position, Vector2Int buildSize)
    {
        cost = buildCost;
        gridPos = position;
        size = buildSize;

        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
        }
    }

    public void SetHighlight(Material highlightMat)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = new Material[renderers[i].sharedMaterials.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                mats[j] = highlightMat;
            }
            renderers[i].sharedMaterials = mats;
        }
    }

    public void RemoveHighlight()
    {
        if (originalMaterials == null) return;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterials = originalMaterials[i];
            }
        }
    }
}