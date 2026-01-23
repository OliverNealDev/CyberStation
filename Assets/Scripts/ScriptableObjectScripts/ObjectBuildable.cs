using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "ObjectBuildable", menuName = "Scriptable Objects/ObjectBuildable")]
public class ObjectBuildable : ScriptableObject
{
    public string name = "New Buildable Object";
    [TextArea]
    public string description = "Description of the buildable object.";
    public Image icon;
    public GameObject prefab;
    public int cost;
    public Vector2 size = Vector2.one;
}
