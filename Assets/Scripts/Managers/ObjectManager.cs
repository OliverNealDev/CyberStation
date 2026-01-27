using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance;
    
    public ObjectBuildable[] buildItems;
    
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        //LoadBuildItems();
    }
    
    private void LoadBuildItems()
    {
        buildItems = Resources.LoadAll<ObjectBuildable>("BuildItems");
        if (buildItems.Length == 0)
        {
            Debug.LogError("No ObjectBuildable items found in Resources/BuildItems. Check folder name and file types!");
        }
    }
}
