using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour, IPreviewInitializable
{
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    public Train trainData;
    public TrainService trainService;
    public Transform trainStopPoint;
    public int platformNumber;

    private float timeStationary;
    private float currentSpeed;
    private float acceleration;
    private float deceleration;
    private bool extraCarriagesSpawned;
    private bool serviceColorApplied;

    [SerializeField] private List<MeshRenderer> changeableMeshRenderers = new List<MeshRenderer>();
    [SerializeField] private GameObject carriagePrefab;

    private TrainDoorController[] trainDoors;

    private trainStates currentState = trainStates.Approaching;

    private enum trainStates
    {
        Approaching,
        Stationary,
        Departing
    }

    void Start()
    {
        if (trainData == null)
        {
            return;
        }

        currentSpeed = trainData.speed;
        acceleration = trainData.speed / 16f;
        deceleration = trainData.speed / 16f;

        if (trainStopPoint != null)
        {
            transform.rotation = trainStopPoint.rotation;
        }

        InitializeRuntimeVisuals();
        trainDoors = GetComponentsInChildren<TrainDoorController>();
    }

    private void InitializeRuntimeVisuals()
    {
        SpawnCarriages();
        ApplyServiceColor();
    }

    public void InitializePreviewVisuals()
    {
        ApplyServiceColor();
    }

    void Update()
    {
        if (trainStopPoint == null) return;

        switch (currentState)
        {
            case trainStates.Approaching:
                float distToStop = Vector3.Distance(transform.position, trainStopPoint.position);

                if (distToStop > 0.01f)
                {
                    float maxAllowedSpeed = Mathf.Sqrt(2 * deceleration * distToStop);
                    float targetSpeed = Mathf.Min(trainData.speed, maxAllowedSpeed);

                    float speedChangeRate = currentSpeed > targetSpeed ? deceleration : acceleration;
                    currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

                    transform.position = Vector3.MoveTowards(transform.position, trainStopPoint.position, currentSpeed * Time.deltaTime);
                }
                else
                {
                    transform.position = trainStopPoint.position;
                    currentSpeed = 0f;
                    currentState = trainStates.Stationary;
                    PassengerManager.Instance.TrainArrived(trainService);
                }
                break;

            case trainStates.Stationary:
                timeStationary += Time.deltaTime;

                if (timeStationary >= trainData.secondsStationary)
                {
                    if (IsReadyToDepart())
                    {
                        foreach (TrainDoorController door in trainDoors)
                        {
                            door.CloseDoors();
                        }

                        if (PassengerManager.Instance != null)
                        {
                            PassengerManager.Instance.ResetWaitingPassengersForNextTrain(trainService);
                        }

                        currentState = trainStates.Departing;
                    }
                }
                break;

            case trainStates.Departing:
                Vector3 departTarget = trainStopPoint.position + (trainStopPoint.forward * 1000f);

                if (Vector3.Distance(transform.position, departTarget) > 0.1f)
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, trainData.speed, acceleration * Time.deltaTime);
                    transform.position = Vector3.MoveTowards(transform.position, departTarget, currentSpeed * Time.deltaTime);
                }
                else
                {
                    TrainManager.Instance.FreePlatform(platformNumber);
                    Destroy(gameObject);
                }
                break;
        }
    }

    public bool IsAtStation()
    {
        return currentState == trainStates.Stationary;
    }

    private bool IsReadyToDepart()
    {
        foreach (TrainDoorController door in trainDoors)
        {
            if (!door.IsAvailable) return false;
        }

        if (timeStationary >= trainData.secondsStationary + 15f)
        {
            return true;
        }

        if (PassengerManager.Instance != null && PassengerManager.Instance.ArePassengersWaitingForTrain(trainService))
        {
            return false;
        }

        return true;
    }

    public List<Vector3> GetDoorPositions()
    {
        List<Vector3> doorPositions = new List<Vector3>();

        foreach (Transform child in transform)
        {
            if (child.CompareTag("TrainCarriage"))
            {
                foreach (Transform door in child)
                {
                    if (door.CompareTag("TrainDoor"))
                    {
                        doorPositions.Add(door.position);
                    }
                }
            }
        }

        return doorPositions;
    }

    private void SpawnCarriages()
    {
        if (extraCarriagesSpawned || carriagePrefab == null || trainData == null || trainData.carriageCount <= 1)
        {
            return;
        }

        extraCarriagesSpawned = true;

        for (int i = 2; i < trainData.carriageCount + 1; i++)
        {
            Vector3 carriagePosition = transform.position - (transform.forward * (i * trainData.carriageLength)) + new Vector3(0, 3, 0);
            Instantiate(carriagePrefab, carriagePosition, transform.rotation, transform);
        }
    }

    private void ApplyServiceColor()
    {
        if (serviceColorApplied || trainData == null)
        {
            return;
        }

        serviceColorApplied = true;
        List<MeshRenderer> renderersToColor = CollectChangeableMeshRenderers();
        foreach (MeshRenderer renderer in renderersToColor)
        {
            if (renderer == null)
            {
                continue;
            }

            foreach (Material material in renderer.materials)
            {
                ApplyColor(material, trainData.trainColor);
            }
        }
    }

    private List<MeshRenderer> CollectChangeableMeshRenderers()
    {
        List<MeshRenderer> renderers = new List<MeshRenderer>();

        AddRenderers(renderers, changeableMeshRenderers);

        GenericTrainCarriageData[] carriageDataObjects = GetComponentsInChildren<GenericTrainCarriageData>(true);
        foreach (GenericTrainCarriageData carriageData in carriageDataObjects)
        {
            AddRenderers(renderers, carriageData.ChangeableMeshRenderers);
        }

        return renderers;
    }

    private void AddRenderers(List<MeshRenderer> targetList, IEnumerable<MeshRenderer> renderersToAdd)
    {
        if (renderersToAdd == null)
        {
            return;
        }

        foreach (MeshRenderer renderer in renderersToAdd)
        {
            if (renderer != null && !targetList.Contains(renderer))
            {
                targetList.Add(renderer);
            }
        }
    }

    private void ApplyColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty(BaseColorPropertyId))
        {
            material.SetColor(BaseColorPropertyId, color);
        }

        if (material.HasProperty(ColorPropertyId))
        {
            material.SetColor(ColorPropertyId, color);
        }
    }
}
