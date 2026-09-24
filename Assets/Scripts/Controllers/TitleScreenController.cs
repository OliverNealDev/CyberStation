using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A title card over the live station. The game used to drop straight into the concourse,
// which is fine for a returning player and baffling for a new one: this gives both a
// moment to land, and a way to start over that is not buried in Settings.
public class TitleScreenController : MonoBehaviour
{
    private const float ConfirmWindowSeconds = 3f;

    // Set before a "New Station" reload, so the fresh station opens straight into play.
    private static bool skipNextTitle;
    private static bool shownThisSession;

    private Canvas canvas;
    private CanvasGroup group;
    private Button newStationButton;
    private TextMeshProUGUI newStationLabel;
    private float confirmUntil;
    private bool isOpen;
    private Behaviour[] cameraControllers;
    private bool[] cameraControllerStates;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        skipNextTitle = false;
        shownThisSession = false;
    }

    private void Start()
    {
        if (skipNextTitle || shownThisSession)
        {
            skipNextTitle = false;
            return;
        }

        shownThisSession = true;
        Build();
        Open();
    }

    private void OnDestroy()
    {
        if (isOpen)
        {
            ReleaseGame();
        }

        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    private void Update()
    {
        if (!isOpen || newStationLabel == null)
        {
            return;
        }

        if (confirmUntil > 0f && Time.unscaledTime > confirmUntil)
        {
            confirmUntil = 0f;
            newStationLabel.text = "NEW STATION";
            newStationLabel.color = UiKit.TextColor;
        }
    }

    private void Open()
    {
        isOpen = true;
        if (GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.AddPauseLock(this);
        }

        // The camera reads the keyboard directly, so it would otherwise drift about
        // underneath the title while the player presses keys.
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraControllers = new Behaviour[]
            {
                mainCamera.GetComponent<OrthographicCameraController>(),
                mainCamera.GetComponent<CameraController>()
            };

            cameraControllerStates = new bool[cameraControllers.Length];
            for (int i = 0; i < cameraControllers.Length; i++)
            {
                if (cameraControllers[i] != null)
                {
                    cameraControllerStates[i] = cameraControllers[i].enabled;
                    cameraControllers[i].enabled = false;
                }
            }
        }
    }

    private void ReleaseGame()
    {
        isOpen = false;
        if (GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.RemovePauseLock(this);
        }

        if (cameraControllers != null)
        {
            for (int i = 0; i < cameraControllers.Length; i++)
            {
                if (cameraControllers[i] != null)
                {
                    cameraControllers[i].enabled = cameraControllerStates[i];
                }
            }
        }
    }

    private void Play()
    {
        if (!isOpen)
        {
            return;
        }

        ReleaseGame();
        StartCoroutine(FadeOutAndHide());
    }

    private IEnumerator FadeOutAndHide()
    {
        group.blocksRaycasts = false;
        yield return UiTween.Fade(group, 1f, 0f, 0.45f);
        canvas.gameObject.SetActive(false);
    }

    private void OnNewStationClicked()
    {
        if (!SaveManager.HasSaveFile)
        {
            Play();
            return;
        }

        if (confirmUntil <= 0f)
        {
            confirmUntil = Time.unscaledTime + ConfirmWindowSeconds;
            newStationLabel.text = "CLICK AGAIN TO WIPE";
            newStationLabel.color = UiKit.Red;
            return;
        }

        skipNextTitle = true;
        ReleaseGame();
        SaveManager.ResetSaveDataAndRestartScene();
    }

    private void Build()
    {
        canvas = UiKit.CreateOverlayCanvas("TitleScreen", 200);
        canvas.transform.SetParent(transform, false);
        RectTransform root = UiKit.CreateStretchRect("Root", canvas.transform);
        group = UiKit.EnsureCanvasGroup(root.gameObject);

        Image dim = UiKit.AddImage(UiKit.CreateStretchRect("Dim", root), new Color(0.01f, 0.015f, 0.03f, 0.82f), false);
        dim.raycastTarget = true;

        // Neon glow faked with stacked copies: the scene font's material has no glow
        // keyword compiled in, and adding one would risk it being stripped from the build.
        CreateTitleLayer(root, new Vector2(6f, -5f), new Color(UiKit.Magenta.r, UiKit.Magenta.g, UiKit.Magenta.b, 0.55f));
        CreateTitleLayer(root, new Vector2(-3f, 3f), new Color(UiKit.Cyan.r, UiKit.Cyan.g, UiKit.Cyan.b, 0.35f));
        CreateTitleLayer(root, Vector2.zero, UiKit.TextColor);

        TextMeshProUGUI tagline = UiKit.CreateText("Tagline", root, "Build the hub. Keep it moving.", 28f, UiKit.MutedTextColor,
            TextAlignmentOptions.Center, new Vector2(0f, 140f), new Vector2(900f, 40f));
        tagline.characterSpacing = 4f;

        Image badge = UiKit.CreatePanel("UpdateBadge", root, new Vector2(0f, 84f), new Vector2(620f, 36f), new Color(UiKit.Magenta.r, UiKit.Magenta.g, UiKit.Magenta.b, 0.16f));
        TextMeshProUGUI badgeText = UiKit.CreateText("Text", badge.transform,
            "<color=" + UiKit.Hex(UiKit.Magenta) + ">RUSH HOUR UPDATE</color>   day cycle  ·  events  ·  directives  ·  reports",
            17f, UiKit.TextColor, TextAlignmentOptions.Center);
        UiKit.Stretch(badgeText.rectTransform);
        badgeText.textWrappingMode = TextWrappingModes.NoWrap;

        bool hasSave = SaveManager.HasSaveFile;
        float y = -10f;
        UiKit.CreateButton("Play", root, hasSave ? "CONTINUE" : "OPEN YOUR STATION", new Vector2(0f, y), new Vector2(360f, 58f), Play, 26f,
            UiKit.Cyan, new Color(0.02f, 0.03f, 0.05f, 1f));
        y -= 70f;

        if (hasSave)
        {
            newStationButton = UiKit.CreateButton("NewStation", root, "NEW STATION", new Vector2(0f, y), new Vector2(360f, 50f), OnNewStationClicked, 22f);
            newStationLabel = UiKit.GetButtonLabel(newStationButton);
            y -= 62f;
        }

        UiKit.CreateButton("HowToPlay", root, "HOW TO PLAY", new Vector2(0f, y), new Vector2(360f, 50f),
            () => { if (HelpOverlayController.Instance != null) HelpOverlayController.Instance.SetOpen(true); }, 22f);
        y -= 62f;

#if !UNITY_WEBGL
        UiKit.CreateButton("Quit", root, "QUIT", new Vector2(0f, y), new Vector2(360f, 50f), Application.Quit, 22f);
#endif

        UiKit.CreateText("Footer", root, "Made by Oliver Neal   ·   Music by Michael Peacock", 16f, UiKit.MutedTextColor,
            TextAlignmentOptions.Center, new Vector2(0f, -470f), new Vector2(1000f, 30f));
    }

    private static void CreateTitleLayer(RectTransform root, Vector2 offset, Color color)
    {
        TextMeshProUGUI title = UiKit.CreateText("Title", root, "CYBER STATION", 124f, color, TextAlignmentOptions.Center,
            new Vector2(0f, 250f) + offset, new Vector2(1400f, 160f));
        title.characterSpacing = 18f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
