using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Root of the runtime HUD. Owns the overlay canvas and the top-bar additions (speed,
// live rating, clock, help), and creates the larger HUD pieces, each of which lives in
// its own controller.
public class StationHudController : MonoBehaviour
{
    public static StationHudController Instance { get; private set; }

    // Positions are in the scene canvas's 1920x1080 space. The existing top-bar buttons
    // sit on a row centred at y = 509, between the right-hand menu group (ends x = 335)
    // and the settings cog (x = 925).
    public const float TopBarY = 509f;
    private const float RefreshInterval = 0.2f;
    private const float RatingTrendWindow = 20f;

    private static readonly string[] SpeedTooltips = { "Pause [Space]", "Normal speed [1]", "Fast [2]", "Fastest [3]" };

    private Canvas canvas;
    private RectTransform root;
    private readonly Image[] speedBackgrounds = new Image[4];
    private readonly List<Image>[] speedGlyphs = new List<Image>[4];
    private TextMeshProUGUI ratingText;
    private Image ratingTrend;
    private TextMeshProUGUI clockText;
    private TextMeshProUGUI phaseText;
    private float refreshTimer;
    private float trendSampleTimer;
    private float trendReferenceRating = -1f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    private void OnEnable()
    {
        GameSpeedManager.OnSpeedChanged += RefreshSpeedButtons;
    }

    private void OnDisable()
    {
        GameSpeedManager.OnSpeedChanged -= RefreshSpeedButtons;
    }

    private void Start()
    {
        canvas = UiKit.CreateOverlayCanvas("StationHUD", 50);
        canvas.transform.SetParent(transform, false);
        root = UiKit.CreateStretchRect("Root", canvas.transform);

        BuildSpeedControls();
        BuildRatingChip();
        BuildClock();
        BuildHelpButton();

        gameObject.AddComponent<TopBarHighlighter>().Initialise(root);
        gameObject.AddComponent<DirectiveTrackerController>().Initialise(root);
        gameObject.AddComponent<EventBannerController>().Initialise(root);
        gameObject.AddComponent<ToastFeedController>().Initialise(root);
        gameObject.AddComponent<DayReportController>();
        gameObject.AddComponent<HelpOverlayController>();
        gameObject.AddComponent<TitleScreenController>();

        RefreshSpeedButtons();
        RefreshStatus();
    }

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= RefreshInterval)
        {
            refreshTimer = 0f;
            RefreshStatus();
        }

        trendSampleTimer += Time.deltaTime;
        if (trendSampleTimer >= RatingTrendWindow && RatingManager.Instance != null)
        {
            trendSampleTimer = 0f;
            trendReferenceRating = RatingManager.Instance.stationRating;
        }
    }

    private void BuildSpeedControls()
    {
        const float size = 30f;
        const float spacing = 5f;
        const float firstX = 380f;

        for (int i = 0; i < speedBackgrounds.Length; i++)
        {
            int speed = i;
            Vector2 position = new Vector2(firstX + (i * (size + spacing)), TopBarY);
            Button button = UiKit.CreateButton("Speed" + i, root, null, position, new Vector2(size, size),
                () => { if (GameSpeedManager.Instance != null) GameSpeedManager.Instance.SetSpeed(speed); },
                baseColor: UiKit.ButtonColor);
            speedBackgrounds[i] = button.targetGraphic as Image;
            speedGlyphs[i] = BuildSpeedGlyph(button.transform, i);
            UiKit.AddTooltip(button.gameObject, SpeedTooltips[i]);
        }
    }

    private static List<Image> BuildSpeedGlyph(Transform parent, int speed)
    {
        List<Image> glyphs = new List<Image>();
        if (speed == 0)
        {
            glyphs.Add(UiKit.CreatePanel("BarLeft", parent, new Vector2(-4.5f, 0f), new Vector2(5f, 15f), UiKit.TextColor, false));
            glyphs.Add(UiKit.CreatePanel("BarRight", parent, new Vector2(4.5f, 0f), new Vector2(5f, 15f), UiKit.TextColor, false));
            return glyphs;
        }

        const float triangleWidth = 8f;
        float totalWidth = triangleWidth * speed;
        for (int i = 0; i < speed; i++)
        {
            float x = -(totalWidth * 0.5f) + (triangleWidth * (i + 0.5f));
            glyphs.Add(UiKit.CreateIcon("Arrow" + i, parent, UiKit.TriangleSprite, new Vector2(x, 0f), new Vector2(10f, 16f), UiKit.TextColor));
        }

        return glyphs;
    }

    private void BuildRatingChip()
    {
        RectTransform chip = UiKit.CreateRect("RatingChip", root, new Vector2(556f, TopBarY), new Vector2(84f, 34f));
        Image hitArea = UiKit.AddImage(chip, new Color(0f, 0f, 0f, 0f));
        hitArea.raycastTarget = true;

        // Clicking the live rating opens the full ratings breakdown.
        Button button = chip.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() =>
        {
            if (UIController.Instance != null && UIController.Instance.ratingsMenuButton != null)
            {
                UIController.Instance.ratingsMenuButton.onClick.Invoke();
            }
        });

        UiKit.CreateIcon("Star", chip, UiKit.StarSprite, new Vector2(-30f, 0f), new Vector2(30f, 30f), UiKit.Yellow);
        ratingText = UiKit.CreateText("Rating", chip, "-", 26f, UiKit.TextColor, TextAlignmentOptions.Left, new Vector2(12f, 0f), new Vector2(52f, 30f));
        ratingText.textWrappingMode = TextWrappingModes.NoWrap;
        ratingTrend = UiKit.CreateIcon("Trend", chip, UiKit.TriangleSprite, new Vector2(38f, 0f), new Vector2(10f, 10f), UiKit.Green, 90f);
        UiKit.AddTooltip(chip.gameObject, "Station rating. Click for the breakdown.");
    }

    private void BuildClock()
    {
        RectTransform clock = UiKit.CreateRect("Clock", root, new Vector2(700f, TopBarY), new Vector2(160f, 50f));
        Image hitArea = UiKit.AddImage(clock, new Color(0f, 0f, 0f, 0f));
        hitArea.raycastTarget = true;

        clockText = UiKit.CreateText("Time", clock, "DAY 1  09:00", 22f, UiKit.TextColor, TextAlignmentOptions.Center, new Vector2(0f, 9f), new Vector2(160f, 26f));
        clockText.textWrappingMode = TextWrappingModes.NoWrap;
        phaseText = UiKit.CreateText("Phase", clock, "DAYTIME", 14f, UiKit.MutedTextColor, TextAlignmentOptions.Center, new Vector2(0f, -12f), new Vector2(160f, 20f));
        phaseText.textWrappingMode = TextWrappingModes.NoWrap;
        phaseText.characterSpacing = 6f;
        UiKit.AddTooltip(clock.gameObject, "Day ends at midnight with a report", -52f);
    }

    private void BuildHelpButton()
    {
        Button help = UiKit.CreateButton("HelpButton", root, "?", new Vector2(880f, TopBarY), new Vector2(30f, 30f),
            () => { if (HelpOverlayController.Instance != null) HelpOverlayController.Instance.Toggle(); }, 22f);
        UiKit.AddTooltip(help.gameObject, "How to play [F1]", -48f, -60f);
    }

    private void RefreshSpeedButtons()
    {
        if (GameSpeedManager.Instance == null)
        {
            return;
        }

        int selected = GameSpeedManager.Instance.SpeedIndex;
        for (int i = 0; i < speedBackgrounds.Length; i++)
        {
            if (speedBackgrounds[i] == null)
            {
                continue;
            }

            bool isSelected = i == selected;
            Color color = isSelected ? (i == 0 ? UiKit.Magenta : UiKit.Cyan) : UiKit.ButtonColor;
            speedBackgrounds[i].color = color;

            Button button = speedBackgrounds[i].GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            button.colors = colors;

            Color glyphColor = isSelected ? new Color(0.02f, 0.03f, 0.05f, 1f) : UiKit.TextColor;
            for (int glyph = 0; glyph < speedGlyphs[i].Count; glyph++)
            {
                speedGlyphs[i][glyph].color = glyphColor;
            }
        }
    }

    private void RefreshStatus()
    {
        if (RatingManager.Instance != null && ratingText != null)
        {
            float rating = RatingManager.Instance.stationRating;
            ratingText.text = rating.ToString("0.0");
            ratingText.color = UiKit.RatingColor(rating);

            if (trendReferenceRating < 0f)
            {
                trendReferenceRating = rating;
            }

            float trend = rating - trendReferenceRating;
            bool showTrend = Mathf.Abs(trend) >= 0.05f;
            ratingTrend.enabled = showTrend;
            if (showTrend)
            {
                ratingTrend.color = trend > 0f ? UiKit.Green : UiKit.Red;
                ratingTrend.rectTransform.localRotation = Quaternion.Euler(0f, 0f, trend > 0f ? 90f : -90f);
            }
        }

        StationClockManager clock = StationClockManager.Instance;
        if (clock != null && clockText != null)
        {
            clockText.text = "DAY " + clock.CurrentDay + "  " + StationClockMath.FormatTime(clock.HourOfDay);
            phaseText.text = StationClockMath.GetPhaseLabel(clock.CurrentPhase).ToUpperInvariant();
            phaseText.color = GetPhaseColor(clock.CurrentPhase);
        }
    }

    public static Color GetPhaseColor(DayPhase phase)
    {
        switch (phase)
        {
            case DayPhase.MorningRush:
            case DayPhase.EveningRush:
                return UiKit.Magenta;
            case DayPhase.Night:
            case DayPhase.LateNight:
                return new Color(0.45f, 0.55f, 1f, 1f);
            default:
                return UiKit.MutedTextColor;
        }
    }
}
