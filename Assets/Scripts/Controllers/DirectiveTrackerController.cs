using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The goal list on the right of the screen. Entries are reused rather than rebuilt,
// because progress changes every half second and only the text needs to move.
public class DirectiveTrackerController : MonoBehaviour
{
    private const float PanelWidth = 330f;
    private const float PanelX = 785f;
    private const float PanelTop = 466f;

    private class EntryView
    {
        public RectTransform rect;
        public TextMeshProUGUI title;
        public TextMeshProUGUI hint;
        public TextMeshProUGUI progress;
        public TextMeshProUGUI reward;
        public RectTransform progressFill;
        public string directiveId;
    }

    private RectTransform panel;
    private RectTransform entryContainer;
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI collapseText;
    private readonly List<EntryView> entries = new List<EntryView>();
    private bool collapsed;

    public void Initialise(RectTransform root)
    {
        panel = UiKit.CreateRect("Directives", root, new Vector2(PanelX, PanelTop), new Vector2(PanelWidth, 0f));
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(PanelX, PanelTop);
        UiKit.AddImage(panel, UiKit.PanelColor);
        UiKit.AddVerticalLayout(panel.gameObject, 6f, new RectOffset(14, 12, 10, 12));
        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildHeader();

        entryContainer = UiKit.CreateRect("Entries", panel, Vector2.zero, new Vector2(PanelWidth, 0f));
        UiKit.AddVerticalLayout(entryContainer.gameObject, 8f);
        ContentSizeFitter entryFitter = entryContainer.gameObject.AddComponent<ContentSizeFitter>();
        entryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Refresh();
    }

    private void OnEnable()
    {
        DirectiveManager.OnDirectivesChanged += Refresh;
    }

    private void OnDisable()
    {
        DirectiveManager.OnDirectivesChanged -= Refresh;
    }

    private void BuildHeader()
    {
        RectTransform header = UiKit.CreateRect("Header", panel, Vector2.zero, new Vector2(PanelWidth, 26f));
        UiKit.SetLayoutSize(header.gameObject, 26f);
        Image hitArea = UiKit.AddImage(header, new Color(0f, 0f, 0f, 0f));
        hitArea.raycastTarget = true;

        Button toggle = header.gameObject.AddComponent<Button>();
        toggle.transition = Selectable.Transition.None;
        toggle.onClick.AddListener(ToggleCollapsed);

        headerText = UiKit.CreateText("Title", header, "DIRECTIVES", 17f, UiKit.Cyan, TextAlignmentOptions.Left);
        UiKit.Stretch(headerText.rectTransform);
        headerText.characterSpacing = 8f;

        collapseText = UiKit.CreateText("Collapse", header, "-", 22f, UiKit.MutedTextColor, TextAlignmentOptions.Right);
        UiKit.Stretch(collapseText.rectTransform);
    }

    private void ToggleCollapsed()
    {
        collapsed = !collapsed;
        collapseText.text = collapsed ? "+" : "-";
        entryContainer.gameObject.SetActive(!collapsed);
    }

    private void Refresh()
    {
        DirectiveManager manager = DirectiveManager.Instance;
        if (manager == null || panel == null)
        {
            return;
        }

        IReadOnlyList<DirectiveManager.ActiveDirective> active = manager.Active;
        panel.gameObject.SetActive(active.Count > 0);

        while (entries.Count < active.Count)
        {
            entries.Add(CreateEntry());
        }

        for (int i = 0; i < entries.Count; i++)
        {
            bool used = i < active.Count;
            entries[i].rect.gameObject.SetActive(used);
            if (used)
            {
                Bind(entries[i], active[i]);
            }
        }

        headerText.text = manager.IsOnboardingComplete ? "DIRECTIVES" : "GETTING STARTED";
    }

    private void Bind(EntryView entry, DirectiveManager.ActiveDirective directive)
    {
        bool isNew = entry.directiveId != directive.definition.id;
        entry.directiveId = directive.definition.id;
        entry.title.text = directive.definition.title;
        entry.hint.text = directive.definition.hint;
        entry.progress.text = directive.progressText;
        entry.reward.text = FormatReward(directive.definition);
        UiKit.SetProgress(entry.progressFill, directive.progress01);

        if (isNew && isActiveAndEnabled)
        {
            StartCoroutine(UiTween.Punch(entry.rect, 0.06f, 0.35f));
        }
    }

    private static string FormatReward(DirectiveDefinition definition)
    {
        string reward = definition.rewardMoney > 0 ? "+" + UiKit.FormatMoney(definition.rewardMoney) : string.Empty;
        if (definition.rewardXp > 0)
        {
            reward += (reward.Length > 0 ? "  " : string.Empty) + "<color=" + UiKit.Hex(UiKit.Cyan) + ">+" + definition.rewardXp + " XP</color>";
        }

        return reward;
    }

    private EntryView CreateEntry()
    {
        RectTransform rect = UiKit.CreateRect("Directive", entryContainer, Vector2.zero, new Vector2(PanelWidth, 0f));
        UiKit.AddImage(rect, new Color(1f, 1f, 1f, 0.04f));
        UiKit.AddVerticalLayout(rect.gameObject, 3f, new RectOffset(12, 10, 8, 9));
        ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        EntryView entry = new EntryView { rect = rect };

        // The accent bar ignores the layout so it can hug the left edge of the card.
        Image accent = UiKit.CreateAccentBar(rect, UiKit.Cyan, 3f);
        accent.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        entry.title = UiKit.CreateText("Title", rect, string.Empty, 22f, UiKit.TextColor, TextAlignmentOptions.TopLeft);
        entry.hint = UiKit.CreateText("Hint", rect, string.Empty, 17f, UiKit.MutedTextColor, TextAlignmentOptions.TopLeft);

        RectTransform barRow = UiKit.CreateRect("BarRow", rect, Vector2.zero, new Vector2(PanelWidth, 10f));
        UiKit.SetLayoutSize(barRow.gameObject, 10f);
        RectTransform bar = UiKit.CreateProgressBar("Bar", barRow, Vector2.zero, Vector2.zero, UiKit.Cyan, out entry.progressFill);
        bar.anchorMin = new Vector2(0f, 0.5f);
        bar.anchorMax = new Vector2(1f, 0.5f);
        bar.sizeDelta = new Vector2(0f, 5f);
        bar.anchoredPosition = Vector2.zero;

        RectTransform progressRow = UiKit.CreateRect("ProgressRow", rect, Vector2.zero, new Vector2(PanelWidth, 18f));
        UiKit.SetLayoutSize(progressRow.gameObject, 18f);

        entry.progress = UiKit.CreateText("Progress", progressRow, string.Empty, 16f, UiKit.TextColor, TextAlignmentOptions.Left);
        entry.progress.rectTransform.anchorMin = new Vector2(0f, 0f);
        entry.progress.rectTransform.anchorMax = new Vector2(0.55f, 1f);
        entry.progress.rectTransform.offsetMin = Vector2.zero;
        entry.progress.rectTransform.offsetMax = Vector2.zero;
        entry.progress.textWrappingMode = TextWrappingModes.NoWrap;

        entry.reward = UiKit.CreateText("Reward", progressRow, string.Empty, 16f, UiKit.Green, TextAlignmentOptions.Right);
        entry.reward.rectTransform.anchorMin = new Vector2(0.45f, 0f);
        entry.reward.rectTransform.anchorMax = new Vector2(1f, 1f);
        entry.reward.rectTransform.offsetMin = Vector2.zero;
        entry.reward.rectTransform.offsetMax = Vector2.zero;
        entry.reward.textWrappingMode = TextWrappingModes.NoWrap;

        return entry;
    }
}
