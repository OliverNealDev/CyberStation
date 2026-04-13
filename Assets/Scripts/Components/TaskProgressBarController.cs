using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class TaskProgressBarController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private string defaultHoldText = "[LMB] Hold";

    private void Awake()
    {
        CacheReferences();
        PointerUiUtility.DisableRaycastTargets(gameObject);
        ShowIdle(defaultHoldText);
    }

    private void OnValidate()
    {
        CacheReferences();
    }

    public void ShowIdle(string promptText)
    {
        SetPromptState(promptText, 0f);
    }

    public void ShowProgress(string promptText, float normalizedProgress)
    {
        SetPromptState(promptText, normalizedProgress);
    }

    private void SetPromptState(string promptText, float normalizedProgress)
    {
        CacheReferences();

        if (statusLabel != null)
        {
            statusLabel.text = promptText;
        }

        if (progressBar != null)
        {
            float clampedProgress = Mathf.Clamp01(normalizedProgress);
            float sliderValue = Mathf.Lerp(progressBar.minValue, progressBar.maxValue, clampedProgress);
            progressBar.SetValueWithoutNotify(sliderValue);
        }
    }

    private void CacheReferences()
    {
        if (statusLabel == null)
        {
            statusLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (progressBar == null)
        {
            progressBar = GetComponentInChildren<Slider>(true);
        }
    }
}
