using UnityEngine;
using UnityEngine.AI;

public class MaterializerController : MonoBehaviour
{
    [Tooltip("Size of the pad for passengers to spawn/exit.")]
    public Vector2 padSize = new Vector2(3f, 3f);

    void Start()
    {
        if (PassengerManager.Instance != null)
        {
            PassengerManager.Instance.RegisterMaterializer(this);
        }
    }

    void OnDestroy()
    {
        if (PassengerManager.Instance != null)
        {
            PassengerManager.Instance.DeregisterMaterializer(this);
        }
    }

    public Vector3 GetRandomPointOnPad()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-padSize.x / 2f, padSize.x / 2f),
            0f,
            Random.Range(-padSize.y / 2f, padSize.y / 2f)
        );
        
        Vector3 targetPos = transform.position + randomOffset;
        
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        
        return transform.position; 
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.1f, new Vector3(padSize.x, 0.2f, padSize.y));
    }
}