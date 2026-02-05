using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Train", menuName = "Scriptable Objects/Train")]
public class Train : ScriptableObject
{
    public string name = "New Train";
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
    public float secondsBetweenArrivals = 30;
    public float secondsStationary = 10;
}
