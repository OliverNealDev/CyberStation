using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Train", menuName = "Scriptable Objects/Train")]
public class Train : ScriptableObject
{
    public string name = "New Train";
    [TextArea]
    public string description = "Description of the train.";
    public Texture2D icon;
    
    public GameObject trainPrefab;
    public GameObject carriagePrefab;
    
    public float speed;
    public int carriageCount;
    public float carriageLength;
    public int capacityPerCarriage;
    public int costPerRide;
    public float secondsBetweenArrivals;
}
