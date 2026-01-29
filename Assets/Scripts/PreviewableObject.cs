using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PreviewableObject : MonoBehaviour
{
    public bool isInPreviewMode = true;
    public List<Component> componentsToEnable = new List<Component>();
    
    public void ExitPreviewMode()
    {
        isInPreviewMode = false;
        if (componentsToEnable.Count == 0) return;
        foreach (var component in componentsToEnable)
        {
            if (component is MonoBehaviour monoBehaviour)
            {
                monoBehaviour.enabled = true;
            }
            else if (component is Collider collider)
            {
                collider.enabled = true;
            }
            else if (component is NavMeshObstacle navMeshObstacle)
            {
                navMeshObstacle.enabled = true;
            }
        }
    }
}
