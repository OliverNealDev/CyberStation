using UnityEngine;

[CreateAssetMenu(fileName = "Train", menuName = "Scriptable Objects/Train")]
public class Train : ScriptableObject
{
    public string trainName = "New Train";
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

    [Header("Economy")]
    public int upfrontCost = 100;
    public int costPerMinute = 10;
}