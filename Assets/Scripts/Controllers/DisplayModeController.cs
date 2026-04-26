using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1000)]
public sealed class DisplayModeController : MonoBehaviour
{
    private const int WindowedWidth = 1280;
    private const int WindowedHeight = 720;
    private const string FullscreenPrefsKey = "Settings.Fullscreen";

    private static bool isBootstrapped;

    public static event Action<bool> OnFullscreenChanged;

    public static bool IsFullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        isBootstrapped = false;
        OnFullscreenChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (isBootstrapped)
        {
            return;
        }

        isBootstrapped = true;

        GameObject controllerObject = new GameObject(nameof(DisplayModeController));
        DontDestroyOnLoad(controllerObject);
        controllerObject.AddComponent<DisplayModeController>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        SetFullscreen(PlayerPrefs.GetInt(FullscreenPrefsKey, 0) == 1);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
        {
            ToggleFullscreen();
        }
    }

    public static void SetFullscreen(bool fullscreen)
    {
        PlayerPrefs.SetInt(FullscreenPrefsKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        if (fullscreen)
        {
            Resolution displayResolution = Screen.currentResolution;
            Screen.SetResolution(displayResolution.width, displayResolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            SetWindowedMode();
        }

        OnFullscreenChanged?.Invoke(fullscreen);
    }

    private static void ToggleFullscreen()
    {
        SetFullscreen(!IsFullscreen);
    }

    private static void SetWindowedMode()
    {
        Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
    }
}
