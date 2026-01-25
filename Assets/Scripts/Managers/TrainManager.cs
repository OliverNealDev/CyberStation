using System.Collections.Generic;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance;

    private Train[] allTrains;
    private List<Train> activeTrains = new List<Train>();
    public bool unlockAllTrains = false;
    
    [SerializeField]
    private List<Platform> activePlatforms = new List<Platform>();

    void Awake()
    {
        Instance = this;
        
        allTrains = Resources.LoadAll<Train>("Trains");
        if (unlockAllTrains)
        {
            foreach (var train in allTrains)
            {
                activeTrains.Add(train);
            }
        }
    }

    void Start()
    {
        var newPlatform = new Platform();
        newPlatform.platformVector3 = new Vector3(-8, 0, 68.5f);
        newPlatform.isOccupied = false;
        newPlatform.platformNumber = 1;
        newPlatform.maxTrainLength = 5;
        activePlatforms.Add(newPlatform);
    }

    private struct Platform
    {
        public Vector3 platformVector3; // Specifically where the train stops
        public bool isOccupied;
        public int platformNumber;
        public int maxTrainLength;
    }
}
