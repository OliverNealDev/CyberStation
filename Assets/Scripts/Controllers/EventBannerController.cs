using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The card at the top of the left-hand notification column that announces an event,
// counts down to it, and then shows it running. Warning and active states share one
// card so the player's eye never has to go looking for where the event went. The
// centre of the screen is left to the station and to the existing bill feed.
public class EventBannerController : MonoBehaviour
{
    private static readonly Vector2 ShownPosition = new Vector2(-740f, 272f);
    private static readonly Vector2 HiddenPosition = new Vector2(-800f, 272f);
    private static readonly Vector2 BannerSize = new Vector2(400f, 100f);

    private RectTransform banner;
    private CanvasGroup group;
    private Image accent;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI timerText;
    private RectTransform timerFill;
    private Image timerFillImage;
    private Coroutine transition;
    private bool isShown;

    public void Initialise(RectTransform root)
    {
        Image background = UiKit.CreatePanel("EventBanner", root, HiddenPosition, BannerSize, UiKit.PanelColor);
        banner = background.rectTransform;
        group = UiKit.EnsureCanvasGroup(banner.gameObject);
        group.alpha = 0f;
        group.blocksRaycasts = false;

        accent = UiKit.CreateAccentBar(banner, UiKit.Yellow, 5f);

        titleText = UiKit.CreateText("Title", banner, string.Empty, 20f, UiKit.Yellow, TextAlignmentOptions.TopLeft,
            new Vector2(-38f, 30f), new Vector2(300f, 28f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.characterSpacing = 4f;

        bodyText = UiKit.CreateText("Body", banner, string.Empty, 16f, UiKit.TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(0f, -8f), new Vector2(376f, 46f));

        timerText = UiKit.CreateText("Timer", banner, string.Empty, 22f, UiKit.TextColor, TextAlignmentOptions.Right,
            new Vector2(150f, 30f), new Vector2(76f, 28f));
        timerText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform bar = UiKit.CreateProgressBar("Timer", banner, new Vector2(0f, -BannerSize.y * 0.5f + 5f), new Vector2(BannerSize.x - 24f, 4f), UiKit.Yellow, out timerFill);
        bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
        timerFillImage = timerFill.GetComponent<Image>();
    }

    private void OnEnable()
    {
        StationEventManager.OnEventStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        StationEventManager.OnEventStageChanged -= HandleStageChanged;
    }

    private void HandleStageChanged()
    {
        StationEventManager events = StationEventManager.Instance;
        if (events == null || banner == null)
        {
            return;
        }

        if (events.Stage == StationEventManager.EventStage.Idle || events.CurrentEvent == null)
        {
            SetShown(false);
            return;
        }

        StationEventDefinition definition = events.CurrentEvent;
        bool warning = events.Stage == StationEventManager.EventStage.Warning;
        Color color = warning ? UiKit.Yellow : (definition.isPositive ? UiKit.Cyan : UiKit.Magenta);

        accent.color = color;
        titleText.color = color;
        timerFillImage.color = color;
        titleText.text = (warning ? "INCOMING: " : string.Empty) + definition.title.ToUpperInvariant();
        bodyText.text = warning ? definition.warning : events.GetActiveDescription();

        if (warning)
        {
            ProceduralSfx.Play(HudSound.EventWarning);
        }

        SetShown(true);
        StartCoroutine(UiTween.Punch(banner, 0.04f, 0.3f));
    }

    private void Update()
    {
        StationEventManager events = StationEventManager.Instance;
        if (!isShown || events == null || events.Stage == StationEventManager.EventStage.Idle)
        {
            return;
        }

        float remaining = events.StageRemaining;
        timerText.text = Mathf.CeilToInt(remaining) + "s";
        UiKit.SetProgress(timerFill, 1f - events.StageProgress01);

        StationEventDefinition definition = events.CurrentEvent;
        if (definition != null && definition.isInspection && events.Stage == StationEventManager.EventStage.Active)
        {
            float average = events.InspectionAverage;
            bool passing = average >= events.InspectionThreshold;
            string averageText = average > 0f ? average.ToString("0.0") : "-";
            bodyText.text = events.GetActiveDescription() + "  <color=" + UiKit.Hex(passing ? UiKit.Green : UiKit.Red) + ">Now: " + averageText + "</color>";
        }
    }

    private void SetShown(bool shown)
    {
        if (isShown == shown)
        {
            return;
        }

        isShown = shown;
        if (transition != null)
        {
            StopCoroutine(transition);
        }

        transition = StartCoroutine(Animate(shown));
    }

    private IEnumerator Animate(bool shown)
    {
        Vector2 from = banner.anchoredPosition;
        Vector2 to = shown ? ShownPosition : HiddenPosition;
        float startAlpha = group.alpha;
        float endAlpha = shown ? 1f : 0f;
        const float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = UiTween.EaseOutCubic(elapsed / duration);
            banner.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        banner.anchoredPosition = to;
        group.alpha = endAlpha;
        transition = null;
    }
}
