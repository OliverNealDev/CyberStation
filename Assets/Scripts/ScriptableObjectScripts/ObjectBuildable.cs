using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ObjectBuildable", menuName = "Scriptable Objects/ObjectBuildable")]
public class ObjectBuildable : ScriptableObject
{
    public string name = "New Buildable Object";
    [TextArea]
    public string description = "Description of the buildable object.";
    public Sprite icon;
    public GameObject prefab;
    public int cost;
    public Vector2 size = Vector2.one;
}
