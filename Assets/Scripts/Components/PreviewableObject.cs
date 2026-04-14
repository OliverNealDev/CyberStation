using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PreviewableObject : MonoBehaviour
{
    public bool isInPreviewMode = true;
    public List<Component> componentsToEnable = new List<Component>();

    public void EnterPreviewMode()
    {
        isInPreviewMode = true;
        SetManagedComponentsEnabled(false);
    }

    public void ExitPreviewMode(int cost)
    {
        isInPreviewMode = false;
        SetManagedComponentsEnabled(true);
        WorldSpacePromptCoordinator.Instance.CreateWorldPrompt( 
            "-$" + cost, 
            transform.position + Vector3.up * 7f, 
            Color.softRed);
    }

    public void ExitPreviewModeSilently()
    {
        isInPreviewMode = false;
        SetManagedComponentsEnabled(true);
    }

    private void SetManagedComponentsEnabled(bool isEnabled)
    {
        if (componentsToEnable.Count == 0)
        {
            return;
        }

        foreach (Component component in componentsToEnable)
        {
            if (component == null)
            {
                continue;
            }

            if (component is Behaviour behaviour)
            {
                behaviour.enabled = isEnabled;
            }
            else if (component is Collider collider)
            {
                collider.enabled = isEnabled;
            }
        }
    }
}
