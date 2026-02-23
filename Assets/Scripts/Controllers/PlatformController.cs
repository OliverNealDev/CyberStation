using UnityEngine;

public class PlatformController : MonoBehaviour
{
    public int platformNumber;
    public Transform trainStopPoint; 
    public BoxCollider passengerWaitArea; 
    public bool isOccupied = false;

    void Start()
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.RegisterPlatform(this);
        }
    }

    public Vector3 GetRandomWaitPosition()
    {
        if (passengerWaitArea == null) return transform.position;

        Bounds bounds = passengerWaitArea.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, 0, randomZ);
    }
}