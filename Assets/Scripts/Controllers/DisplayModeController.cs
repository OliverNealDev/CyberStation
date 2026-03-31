using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1000)]
public sealed class DisplayModeController : MonoBehaviour
{
    private const int WindowedWidth = 1280;
    private const int WindowedHeight = 720;
    private static bool isBootstrapped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        isBootstrapped = false;
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

        SetWindowedMode();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
        {
            ToggleFullscreen();
        }
    }

    private void ToggleFullscreen()
    {
        if (IsFullscreen())
        {
            SetWindowedMode();
        }
        else
        {
            Resolution displayResolution = Screen.currentResolution;
            Screen.SetResolution(displayResolution.width, displayResolution.height, FullScreenMode.FullScreenWindow);
        }
    }

    private static void SetWindowedMode()
    {
        Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
    }

    private static bool IsFullscreen()
    {
        return Screen.fullScreenMode != FullScreenMode.Windowed;
    }
}
