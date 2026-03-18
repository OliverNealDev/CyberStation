using UnityEngine;

public class DropShadow : MonoBehaviour
{
    public LayerMask groundLayer;
    public float maxDistance = 20f;
    public float floorOffset = 0.02f;
    public float raycastStartOffset = 1.0f;

    private Transform parentTransform;

    void Start()
    {
        parentTransform = transform.parent;
    }

    void LateUpdate()
    {
        if (parentTransform == null) return;

        Vector3 rayOrigin = parentTransform.position + (Vector3.up * raycastStartOffset);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, maxDistance, groundLayer))
        {
            transform.position = hit.point + (Vector3.up * floorOffset);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        else
        {
            transform.position = Vector3.down * 1000f;
        }
    }
}