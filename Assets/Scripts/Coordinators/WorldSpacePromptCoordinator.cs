using System;
using TMPro;
using UnityEngine;

public class WorldSpacePromptCoordinator : MonoBehaviour
{
    public static WorldSpacePromptCoordinator Instance;
    
    public Canvas worldSpaceCanvas;
    
    public GameObject promptPrefab;

    private void Awake()
    {
        Instance = this;
    }
    
    public void CreateWorldPrompt(string message, Vector3 worldPosition, Color color)
    {
        if (promptPrefab == null || worldSpaceCanvas == null)
        {
            Debug.LogWarning("Prompt Prefab or World Space Canvas is not assigned.");
            return;
        }

        GameObject promptInstance = Instantiate(promptPrefab, worldSpaceCanvas.transform);
        promptInstance.transform.position = worldPosition;
        PointerUiUtility.DisableRaycastTargets(promptInstance);

        TextMeshProUGUI tmp_text = promptInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp_text != null)
        {
            tmp_text.color = color;
            tmp_text.text = message;
        }
    }
}
