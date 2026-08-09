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

#if UNITY_WEBGL && !UNITY_EDITOR
    public static bool IsFullscreen => Screen.fullScreen;
#else
    public static bool IsFullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;
#endif

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

#if UNITY_WEBGL && !UNITY_EDITOR
        // On the web the page owns the canvas size. Restoring a saved display mode
        // here would call Screen.SetResolution and stamp a fixed pixel size over the
        // responsive canvas, which is what makes the game render at the wrong size
        // inside an itch.io embed. Report whatever the page has given us instead.
        OnFullscreenChanged?.Invoke(IsFullscreen);
#else
        SetFullscreen(PlayerPrefs.GetInt(FullscreenPrefsKey, 0) == 1);
#endif
    }

    // F11 is the browser's own fullscreen shortcut, and itch.io supplies a fullscreen
    // button of its own, so the game does not bind it on the web at all.
#if !UNITY_WEBGL || UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
        {
            ToggleFullscreen();
        }
    }

    private static void ToggleFullscreen()
    {
        SetFullscreen(!IsFullscreen);
    }
#endif

    public static void SetFullscreen(bool fullscreen)
    {
        PlayerPrefs.SetInt(FullscreenPrefsKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

#if UNITY_WEBGL && !UNITY_EDITOR
        // Never call Screen.SetResolution on the web: it overwrites the canvas size the
        // page laid out. Screen.fullScreen maps onto the browser Fullscreen API, which
        // only succeeds when this runs inside a real user gesture such as a button press.
        Screen.fullScreen = fullscreen;
#else
        if (fullscreen)
        {
            Resolution displayResolution = Screen.currentResolution;
            Screen.SetResolution(displayResolution.width, displayResolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            SetWindowedMode();
        }
#endif

        OnFullscreenChanged?.Invoke(fullscreen);
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private static void SetWindowedMode()
    {
        Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
    }
#endif
}
