using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ProgressionTopBarUI : MonoBehaviour
{
    [SerializeField] private Slider xpBar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private string levelPrefix = "Tier ";

    private void OnEnable()
    {
        ProgressionManager.OnProgressionChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        ProgressionManager.OnProgressionChanged -= Refresh;
    }

    public void Refresh()
    {
        if (ProgressionManager.Instance == null)
        {
            return;
        }

        if (xpBar != null)
        {
            xpBar.minValue = 0f;
            xpBar.maxValue = 1f;
            xpBar.value = ProgressionManager.Instance.LevelProgress01;
        }

        if (levelText != null)
        {
            levelText.text = levelPrefix + ProgressionManager.Instance.CurrentLevel;
        }

        if (xpText != null)
        {
            if (ProgressionManager.Instance.IsMaxLevel)
            {
                xpText.text = "MAX";
            }
            else
            {
                xpText.text = ProgressionManager.Instance.XpIntoCurrentLevel + "/" + ProgressionManager.Instance.XpNeededForNextLevel + " XP";
            }
        }
    }
}
