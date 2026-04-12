using UnityEngine;

[CreateAssetMenu(fileName = "Train", menuName = "Scriptable Objects/Train")]
public class Train : ScriptableObject
{
    public string trainName = "New Train";
    public int requiredTier = 1;
    [TextArea]
    public string description = "Description of the train.";
    public Sprite icon;
    
    public GameObject trainPrefab;
    public GameObject carriagePrefab;
    
    public float speed = 10;
    public int carriageCount = 2;
    public float carriageLength = 34;
    public int capacityPerCarriage = 50;
    public int costPerRide = 5;
    
    public float secondsStationary = 10;
    
    public Color trainColor = Color.white;

    public bool isWarm = false;

    [Header("Passenger Profile")]
    [Tooltip("Chance that passengers on this line arrive hungry. Set to 0 to disable hunger for the line.")]
    [Range(0f, 1f)] public float hungerNeedChance = 0.4f;
    [Tooltip("Chance that passengers on this line arrive thirsty. Set to 0 to disable thirst for the line.")]
    [Range(0f, 1f)] public float thirstNeedChance = 0.4f;
    [Tooltip("Chance that passengers on this line arrive needing energy. Set to 0 to disable energy for the line.")]
    [Range(0f, 1f)] public float energyNeedChance = 0.15f;
    [Tooltip("Chance that passengers on this line arrive needing hygiene. Set to 0 to disable hygiene for the line.")]
    [Range(0f, 1f)] public float hygieneNeedChance = 0.2f;
    [Min(0f)] public float evasionChanceMultiplier = 1f;
    [Range(0f, 1f)] public float connectingTrainChance = 0.2f;

    [Header("Economy")]
    public int upfrontCost = 100;
    public int costPerMinute = 10;

    [System.NonSerialized] private Sprite runtimeIcon;

    public Sprite GetIcon()
    {
        if (runtimeIcon == null)
        {
            runtimeIcon = PrefabIconRenderer.GetIcon(
                trainPrefab,
                icon,
                PrefabIconView.TrainFront,
                GetInstanceID().ToString(),
                previewInstance =>
                {
                    TrainController controller = previewInstance.GetComponent<TrainController>();
                    if (controller != null)
                    {
                        controller.trainData = this;
                    }
                });
        }

        return runtimeIcon;
    }
}
