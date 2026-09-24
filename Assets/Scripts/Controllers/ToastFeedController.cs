using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Non-blocking notifications, plus the big tier-up announcement. Nothing here ever needs
// dismissing: management players quickly learn to hate a stack of "OK" buttons.
public class ToastFeedController : MonoBehaviour
{
    public static ToastFeedController Instance { get; private set; }

    private const int MaxToasts = 4;
    private const float ToastLifetime = 5f;
    private const float ToastWidth = 330f;
    private const float ToastHeight = 64f;
    private const float ToastSpacing = 8f;
    // Under the event card, in the left-hand notification column.
    private static readonly Vector2 FirstToastPosition = new Vector2(-775f, 172f);

    private class Toast
    {
        public RectTransform rect;
        public CanvasGroup group;
        public float age;
        public Coroutine move;
    }

    private RectTransform root;
    private readonly List<Toast> toasts = new List<Toast>();

    private RectTransform tierBanner;
    private CanvasGroup tierGroup;
    private TextMeshProUGUI tierTitle;
    private TextMeshProUGUI tierUnlocks;
    private Coroutine tierRoutine;

    public void Initialise(RectTransform hudRoot)
    {
        Instance = this;
        root = hudRoot;
        BuildTierBanner();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        DirectiveManager.OnDirectiveCompleted += HandleDirectiveCompleted;
        DirectiveManager.OnLegacyCatchUp += HandleLegacyCatchUp;
        StationEventManager.OnEventResolved += HandleEventResolved;
        StationStatsTracker.OnRushHourHeld += HandleRushHourHeld;
        StationStatsTracker.OnDayReportReady += HandleDayReport;
        ProgressionManager.OnTierReached += HandleTierReached;
    }

    private void OnDisable()
    {
        DirectiveManager.OnDirectiveCompleted -= HandleDirectiveCompleted;
        DirectiveManager.OnLegacyCatchUp -= HandleLegacyCatchUp;
        StationEventManager.OnEventResolved -= HandleEventResolved;
        StationStatsTracker.OnRushHourHeld -= HandleRushHourHeld;
        StationStatsTracker.OnDayReportReady -= HandleDayReport;
        ProgressionManager.OnTierReached -= HandleTierReached;
    }

    private void Update()
    {
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            Toast toast = toasts[i];
            toast.age += Time.unscaledDeltaTime;
            if (toast.age >= ToastLifetime)
            {
                StartCoroutine(Dismiss(toast));
                toasts.RemoveAt(i);
                Reflow();
            }
        }
    }

    public void Show(string title, string body, Color color)
    {
        if (root == null)
        {
            return;
        }

        if (toasts.Count >= MaxToasts)
        {
            Toast oldest = toasts[toasts.Count - 1];
            toasts.RemoveAt(toasts.Count - 1);
            StartCoroutine(Dismiss(oldest));
        }

        Image background = UiKit.CreatePanel("Toast", root, FirstToastPosition + new Vector2(-40f, 0f), new Vector2(ToastWidth, ToastHeight), UiKit.PanelColor);
        UiKit.CreateAccentBar(background.transform, color, 4f);

        TextMeshProUGUI titleText = UiKit.CreateText("Title", background.transform, title, 19f, color, TextAlignmentOptions.TopLeft,
            new Vector2(8f, 14f), new Vector2(ToastWidth - 36f, 26f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI bodyText = UiKit.CreateText("Body", background.transform, body, 15f, UiKit.TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(8f, -12f), new Vector2(ToastWidth - 36f, 30f));
        bodyText.overflowMode = TextOverflowModes.Ellipsis;

        Toast toast = new Toast
        {
            rect = background.rectTransform,
            group = UiKit.EnsureCanvasGroup(background.gameObject)
        };

        toast.group.alpha = 0f;
        toast.group.blocksRaycasts = false;
        toasts.Insert(0, toast);
        StartCoroutine(UiTween.Fade(toast.group, 0f, 1f, 0.25f));
        Reflow();
    }

    private void Reflow()
    {
        for (int i = 0; i < toasts.Count; i++)
        {
            Toast toast = toasts[i];
            Vector2 target = FirstToastPosition - new Vector2(0f, i * (ToastHeight + ToastSpacing));
            if (toast.move != null)
            {
                StopCoroutine(toast.move);
            }

            toast.move = StartCoroutine(UiTween.Move(toast.rect, toast.rect.anchoredPosition, target, 0.3f));
        }
    }

    private IEnumerator Dismiss(Toast toast)
    {
        if (toast.move != null)
        {
            StopCoroutine(toast.move);
        }

        yield return UiTween.Fade(toast.group, toast.group.alpha, 0f, 0.3f);
        if (toast.rect != null)
        {
            Destroy(toast.rect.gameObject);
        }
    }

    private void HandleDirectiveCompleted(DirectiveDefinition definition)
    {
        string reward = definition.rewardMoney > 0 ? "+" + UiKit.FormatMoney(definition.rewardMoney) : string.Empty;
        if (definition.rewardXp > 0)
        {
            reward += (reward.Length > 0 ? "   " : string.Empty) + "+" + definition.rewardXp + " XP";
        }

        Show("DONE: " + definition.title.ToUpperInvariant(), reward, UiKit.Green);
        ProceduralSfx.Play(HudSound.DirectiveComplete);
    }

    private void HandleLegacyCatchUp(int count)
    {
        Show("WELCOME BACK", count + " directives already done by your station.", UiKit.Cyan);
    }

    private void HandleEventResolved(StationEventDefinition definition, bool success, int moneyChange)
    {
        if (definition == null)
        {
            return;
        }

        if (definition.isInspection)
        {
            if (success)
            {
                Show("INSPECTION PASSED", "The Transit Authority pays " + UiKit.FormatMoney(moneyChange) + ".", UiKit.Green);
                ProceduralSfx.Play(HudSound.EventSuccess);
            }
            else
            {
                Show("INSPECTION FAILED", "Fined " + UiKit.FormatMoney(-moneyChange) + ". Keep the rating up next time.", UiKit.Red);
                ProceduralSfx.Play(HudSound.EventFailure);
            }

            return;
        }

        Show(definition.title.ToUpperInvariant() + " OVER", "Things are back to normal.", UiKit.MutedTextColor);
    }

    private void HandleRushHourHeld(float average)
    {
        Show("RUSH HOUR HELD", "Averaged " + average.ToString("0.0") + " stars through the rush.", UiKit.Magenta);
        ProceduralSfx.Play(HudSound.EventSuccess);
    }

    private void HandleDayReport(DayStats stats)
    {
        // A graded day gets the full report panel instead; this covers days with no service.
        if (stats.HadService)
        {
            return;
        }

        Show("DAY " + stats.day + " ENDED", "No passengers yet. Follow the directives to open up.", UiKit.MutedTextColor);
    }

    private void HandleTierReached(int tier)
    {
        if (tierBanner == null)
        {
            return;
        }

        tierTitle.text = "TIER " + tier + " REACHED";
        tierUnlocks.text = BuildUnlockList(tier);

        if (tierRoutine != null)
        {
            StopCoroutine(tierRoutine);
        }

        tierRoutine = StartCoroutine(ShowTierBanner());
    }

    private IEnumerator ShowTierBanner()
    {
        tierBanner.gameObject.SetActive(true);
        StartCoroutine(UiTween.ScaleIn(tierBanner, 0.45f));
        yield return UiTween.Fade(tierGroup, 0f, 1f, 0.25f);

        float shown = 0f;
        while (shown < 4.5f)
        {
            shown += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return UiTween.Fade(tierGroup, 1f, 0f, 0.6f);
        tierBanner.gameObject.SetActive(false);
        tierRoutine = null;
    }

    private void BuildTierBanner()
    {
        Image background = UiKit.CreatePanel("TierBanner", root, new Vector2(0f, 190f), new Vector2(760f, 150f), UiKit.PanelColor);
        tierBanner = background.rectTransform;
        tierGroup = UiKit.EnsureCanvasGroup(background.gameObject);
        tierGroup.blocksRaycasts = false;

        Image glow = UiKit.CreatePanel("Glow", tierBanner, Vector2.zero, new Vector2(772f, 162f), new Color(UiKit.Cyan.r, UiKit.Cyan.g, UiKit.Cyan.b, 0.5f));
        glow.sprite = UiKit.RingSprite;
        glow.transform.SetAsFirstSibling();

        tierTitle = UiKit.CreateText("Title", tierBanner, string.Empty, 56f, UiKit.Cyan, TextAlignmentOptions.Center,
            new Vector2(0f, 24f), new Vector2(740f, 70f));
        tierTitle.characterSpacing = 10f;
        tierTitle.textWrappingMode = TextWrappingModes.NoWrap;

        tierUnlocks = UiKit.CreateText("Unlocks", tierBanner, string.Empty, 20f, UiKit.TextColor, TextAlignmentOptions.Center,
            new Vector2(0f, -38f), new Vector2(720f, 50f));

        tierBanner.gameObject.SetActive(false);
    }

    private static string BuildUnlockList(int tier)
    {
        List<string> unlocks = new List<string>();
        foreach (ObjectBuildable buildable in Resources.LoadAll<ObjectBuildable>("BuildItems"))
        {
            if (buildable != null && buildable.requiredTier == tier) unlocks.Add(buildable.objectName);
        }

        foreach (Train train in Resources.LoadAll<Train>("Trains"))
        {
            if (train != null && train.requiredTier == tier) unlocks.Add(train.trainName);
        }

        foreach (StaffMember staff in Resources.LoadAll<StaffMember>("Staff"))
        {
            if (staff != null && staff.requiredTier == tier) unlocks.Add(staff.staffName);
        }

        foreach (Expansion expansion in Resources.LoadAll<Expansion>("Expansions"))
        {
            if (expansion != null && expansion.requiredTier == tier) unlocks.Add(expansion.name);
        }

        if (unlocks.Count == 0)
        {
            return "The network trusts you with more.";
        }

        StringBuilder builder = new StringBuilder("<color=" + UiKit.Hex(UiKit.MutedTextColor) + ">UNLOCKED</color>  ");
        for (int i = 0; i < unlocks.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("  ·  ");
            }

            builder.Append(unlocks[i]);
        }

        return builder.ToString();
    }
}
