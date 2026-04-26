using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuController : MonoBehaviour
{
    private const string ResetSaveDataText = "RESET SAVE DATA";
    private const string ConfirmResetText = "CONFIRM";

    [Header("Controls")]
    public Toggle fullscreenToggle;
    public Slider audioVolumeSlider;
    public Slider musicVolumeSlider;
    public Button resetSaveDataButton;
    public TextMeshProUGUI resetSaveDataButtonText;

    private bool isResetConfirmationPending;
    private int lastResetClickFrame = -1;

    private void Awake()
    {
        FindResetButtonTextIfNeeded();
    }

    private void OnEnable()
    {
        FindResetButtonTextIfNeeded();
        DisplayModeController.OnFullscreenChanged += HandleFullscreenChanged;
        HookEvents();
        RefreshControlValues();
        ResetSaveButtonState();
    }

    private void OnDisable()
    {
        DisplayModeController.OnFullscreenChanged -= HandleFullscreenChanged;
        UnhookEvents();
        ResetSaveButtonState();
    }

    private void HookEvents()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (audioVolumeSlider != null)
        {
            audioVolumeSlider.onValueChanged.RemoveListener(SetAudioVolume);
            audioVolumeSlider.onValueChanged.AddListener(SetAudioVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (resetSaveDataButton != null)
        {
            resetSaveDataButton.onClick.RemoveListener(ResetSaveDataClicked);
            resetSaveDataButton.onClick.AddListener(ResetSaveDataClicked);
        }
    }

    private void UnhookEvents()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        }

        if (audioVolumeSlider != null)
        {
            audioVolumeSlider.onValueChanged.RemoveListener(SetAudioVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        }

        if (resetSaveDataButton != null)
        {
            resetSaveDataButton.onClick.RemoveListener(ResetSaveDataClicked);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        DisplayModeController.SetFullscreen(isFullscreen);
    }

    public void SetAudioVolume(float volume)
    {
        SoundEffectController.SetVolume(volume);
    }

    public void SetMusicVolume(float volume)
    {
        MusicController.SetVolume(volume);
    }

    public void ResetSaveDataClicked()
    {
        if (lastResetClickFrame == Time.frameCount)
        {
            return;
        }

        lastResetClickFrame = Time.frameCount;

        if (!isResetConfirmationPending)
        {
            isResetConfirmationPending = true;
            SetResetButtonText(ConfirmResetText);
            return;
        }

        SaveManager.ResetSaveDataAndRestartScene();
    }

    public void HandleResetSaveDataClicked()
    {
        ResetSaveDataClicked();
    }

    private void RefreshControlValues()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(DisplayModeController.IsFullscreen);
        }

        if (audioVolumeSlider != null)
        {
            audioVolumeSlider.SetValueWithoutNotify(SoundEffectController.Volume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(MusicController.Volume);
        }
    }

    private void ResetSaveButtonState()
    {
        isResetConfirmationPending = false;
        SetResetButtonText(ResetSaveDataText);
    }

    private void SetResetButtonText(string text)
    {
        FindResetButtonTextIfNeeded();

        if (resetSaveDataButtonText != null)
        {
            resetSaveDataButtonText.text = text;
        }
    }

    private void FindResetButtonTextIfNeeded()
    {
        if (resetSaveDataButtonText == null && resetSaveDataButton != null)
        {
            resetSaveDataButtonText = resetSaveDataButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void HandleFullscreenChanged(bool isFullscreen)
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        }
    }
}
