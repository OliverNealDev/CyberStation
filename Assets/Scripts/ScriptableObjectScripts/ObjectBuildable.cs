using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ObjectBuildable", menuName = "Scriptable Objects/ObjectBuildable")]
public class ObjectBuildable : ScriptableObject
{
    public string objectName = "New Buildable Object";
    [TextArea]
    public string description = "Description of the buildable object.";
    public Sprite icon;
    public GameObject prefab;
    public int cost;
    
    // Changed to Vector2Int so it plays perfectly with our GridManager!
    public Vector2Int size = Vector2Int.one; 
}