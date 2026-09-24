using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The end-of-day card. It pauses the station while it is open, because it is the one
// moment the game asks the player to stop and read, and it always ends on something to
// do next rather than just a score.
public class DayReportController : MonoBehaviour
{
    public static DayReportController Instance { get; private set; }

    private Canvas canvas;
    private RectTransform card;
    private CanvasGroup group;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI gradeText;
    private Image gradeBadge;
    private TextMeshProUGUI gradeCaption;
    private TextMeshProUGUI operationsText;
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI complaintsText;
    private TextMeshProUGUI focusText;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (isOpen && GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.RemovePauseLock(this);
        }

        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    private void OnEnable()
    {
        StationStatsTracker.OnDayReportReady += HandleDayReport;
    }

    private void OnDisable()
    {
        StationStatsTracker.OnDayReportReady -= HandleDayReport;
    }

    private void Update()
    {
        if (isOpen && Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            Close();
        }
    }

    private void HandleDayReport(DayStats stats)
    {
        if (stats != null && stats.HadService)
        {
            Open(stats);
        }
    }

    public void Open(DayStats stats)
    {
        Populate(stats);

        if (!isOpen)
        {
            isOpen = true;
            if (UIController.IsUiHidden)
            {
                UIController.Instance.SetUiHidden(false);
            }

            canvas.gameObject.SetActive(true);
            if (GameSpeedManager.Instance != null)
            {
                GameSpeedManager.Instance.AddPauseLock(this);
            }

            StartCoroutine(UiTween.Fade(group, 0f, 1f, 0.25f));
            StartCoroutine(UiTween.ScaleIn(card, 0.4f));
        }

        ProceduralSfx.Play(HudSound.DayReport);
    }

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        canvas.gameObject.SetActive(false);
        if (GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.RemovePauseLock(this);
        }
    }

    private void Populate(DayStats stats)
    {
        titleText.text = "DAY " + stats.day + " REPORT";

        gradeText.text = DayGrader.GetLabel(stats.grade);
        Color gradeColor = UiKit.GradeColor(stats.grade);
        gradeText.color = gradeColor;
        gradeBadge.color = gradeColor;
        gradeCaption.text = stats.subsidy > 0
            ? "Transit Authority subsidy <color=" + UiKit.Hex(UiKit.Green) + ">+" + UiKit.FormatMoney(stats.subsidy) + "</color>"
            : "No subsidy today. A grade of C or better earns one.";

        int servicePercent = Mathf.RoundToInt(stats.ServiceRatio * 100f);
        StringBuilder operations = new StringBuilder();
        AppendRow(operations, "Passengers served", stats.passengersBoarded + " of " + stats.passengersArrived);
        AppendRow(operations, "Tickets sold", stats.ticketsSold.ToString());
        AppendRow(operations, "Service", servicePercent + "%  <color=" + UiKit.Hex(UiKit.MutedTextColor) + ">(" + stats.needsFailed + " gave up)</color>");
        AppendRow(operations, "Trains departed", stats.trainsDeparted.ToString());
        AppendRow(operations, "Evaders caught", stats.evadersCaught.ToString());
        AppendRow(operations, "Busiest moment", stats.peakPassengers + " passengers");
        operationsText.text = operations.ToString();

        StringBuilder money = new StringBuilder();
        AppendRow(money, "Average rating", ColorRating(stats.AverageRating));
        AppendRow(money, "Rush hour rating", ColorRating(stats.RushAverageRating));
        AppendRow(money, "Fares & sales", "<color=" + UiKit.Hex(UiKit.Green) + ">" + UiKit.FormatMoney(stats.income) + "</color>");
        AppendRow(money, "Running costs", "<color=" + UiKit.Hex(UiKit.Red) + ">" + UiKit.FormatMoney(-stats.expenses) + "</color>");
        AppendRow(money, "Operating profit", "<color=" + UiKit.Hex(stats.Net >= 0 ? UiKit.Green : UiKit.Red) + ">" + UiKit.FormatMoney(stats.Net) + "</color>");
        AppendRow(money, "Building & hiring", UiKit.FormatMoney(-stats.investment));
        moneyText.text = money.ToString();

        List<KeyValuePair<ComplaintType, int>> complaints = stats.GetTopComplaints(3);
        if (complaints.Count == 0)
        {
            complaintsText.text = "<color=" + UiKit.Hex(UiKit.Green) + ">Nobody gave up on anything today.</color>";
        }
        else
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < complaints.Count; i++)
            {
                builder.Append("<color=" + UiKit.Hex(UiKit.Yellow) + ">" + complaints[i].Value + "x</color>  ");
                builder.Append(DayGrader.GetComplaintText(complaints[i].Key));
                builder.Append("\n<size=80%><color=" + UiKit.Hex(UiKit.MutedTextColor) + ">");
                builder.Append(DayGrader.GetComplaintHint(complaints[i].Key));
                builder.Append("</color></size>\n");
            }

            complaintsText.text = builder.ToString().TrimEnd('\n');
        }

        focusText.text = string.IsNullOrEmpty(stats.weakestCategory)
            ? string.Empty
            : "<color=" + UiKit.Hex(UiKit.Cyan) + ">FOCUS TOMORROW:</color>  " + stats.weakestCategory +
              " is your weakest rating (" + stats.weakestCategoryValue.ToString("0.0") + "). " +
              StationStatsTracker.GetCategoryHint(stats.weakestCategory);
    }

    private static string ColorRating(float rating)
    {
        return "<color=" + UiKit.Hex(UiKit.RatingColor(rating)) + ">" + rating.ToString("0.0") + " stars</color>";
    }

    private static void AppendRow(StringBuilder builder, string label, string value)
    {
        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append("<color=" + UiKit.Hex(UiKit.MutedTextColor) + ">" + label + "</color><pos=58%>" + value);
    }

    private void Build()
    {
        canvas = UiKit.CreateOverlayCanvas("DayReport", 150);
        canvas.transform.SetParent(transform, false);
        RectTransform root = UiKit.CreateStretchRect("Root", canvas.transform);
        group = UiKit.EnsureCanvasGroup(root.gameObject);

        Image dim = UiKit.AddImage(UiKit.CreateStretchRect("Dim", root), new Color(0f, 0f, 0f, 0.55f), false);
        dim.raycastTarget = true;

        Image cardImage = UiKit.CreatePanel("Card", root, Vector2.zero, new Vector2(900f, 660f), UiKit.PanelColor);
        cardImage.raycastTarget = true;
        card = cardImage.rectTransform;
        Image border = UiKit.CreatePanel("Border", card, Vector2.zero, new Vector2(900f, 660f), new Color(UiKit.Cyan.r, UiKit.Cyan.g, UiKit.Cyan.b, 0.35f));
        border.sprite = UiKit.RingSprite;

        titleText = UiKit.CreateText("Title", card, "DAY REPORT", 44f, UiKit.TextColor, TextAlignmentOptions.Left,
            new Vector2(-110f, 272f), new Vector2(620f, 60f));
        titleText.characterSpacing = 8f;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        gradeBadge = UiKit.CreatePanel("GradeBadge", card, new Vector2(362f, 262f), new Vector2(104f, 104f), UiKit.Cyan);
        gradeBadge.sprite = UiKit.RingSprite;
        gradeText = UiKit.CreateText("Grade", gradeBadge.transform, "A", 72f, UiKit.Cyan, TextAlignmentOptions.Center);
        UiKit.Stretch(gradeText.rectTransform);
        UiKit.CreateText("GradeLabel", card, "GRADE", 14f, UiKit.MutedTextColor, TextAlignmentOptions.Center,
            new Vector2(362f, 198f), new Vector2(120f, 20f)).characterSpacing = 8f;

        gradeCaption = UiKit.CreateText("Subsidy", card, string.Empty, 20f, UiKit.TextColor, TextAlignmentOptions.Left,
            new Vector2(-110f, 222f), new Vector2(620f, 30f));

        CreateSectionHeader("OPERATIONS", new Vector2(-218f, 172f));
        operationsText = UiKit.CreateText("Operations", card, string.Empty, 20f, UiKit.TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(-218f, 52f), new Vector2(400f, 190f));
        operationsText.lineSpacing = 14f;

        CreateSectionHeader("RATINGS & MONEY", new Vector2(218f, 172f));
        moneyText = UiKit.CreateText("Money", card, string.Empty, 20f, UiKit.TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(218f, 52f), new Vector2(400f, 190f));
        moneyText.lineSpacing = 14f;

        CreateSectionHeader("WHAT PASSENGERS SAID", new Vector2(0f, -66f), 836f);
        complaintsText = UiKit.CreateText("Complaints", card, string.Empty, 19f, UiKit.TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(0f, -154f), new Vector2(836f, 136f));

        focusText = UiKit.CreateText("Focus", card, string.Empty, 18f, UiKit.TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(-90f, -262f), new Vector2(656f, 56f));

        UiKit.CreateButton("Continue", card, "CONTINUE", new Vector2(350f, -270f), new Vector2(160f, 50f), Close, 22f, UiKit.Cyan,
            new Color(0.02f, 0.03f, 0.05f, 1f));

        canvas.gameObject.SetActive(false);
    }

    private void CreateSectionHeader(string text, Vector2 center, float width = 400f)
    {
        TextMeshProUGUI header = UiKit.CreateText(text, card, text, 15f, UiKit.Cyan, TextAlignmentOptions.Left, center, new Vector2(width, 22f));
        header.characterSpacing = 8f;
        UiKit.CreatePanel("Rule", card, center + new Vector2(0f, -15f), new Vector2(width, 1f), new Color(1f, 1f, 1f, 0.12f), false);
    }
}
