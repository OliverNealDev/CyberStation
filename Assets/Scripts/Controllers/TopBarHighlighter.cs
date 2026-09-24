using UnityEngine;
using UnityEngine.UI;

// Pulses a ring around the top-bar button the current directive needs, with a small
// arrow under it. It follows the real button every frame rather than copying its
// position, because the menu groups re-lay themselves out as buttons unlock.
public class TopBarHighlighter : MonoBehaviour
{
    private RectTransform ring;
    private Image ringImage;
    private RectTransform arrow;
    private Image arrowImage;
    private RectTransform root;

    public void Initialise(RectTransform hudRoot)
    {
        root = hudRoot;

        ringImage = UiKit.CreatePanel("DirectiveHighlight", root, Vector2.zero, new Vector2(52f, 52f), UiKit.Cyan);
        ringImage.sprite = UiKit.RingSprite;
        ring = ringImage.rectTransform;

        arrowImage = UiKit.CreateIcon("DirectiveArrow", root, UiKit.TriangleSprite, Vector2.zero, new Vector2(16f, 16f), UiKit.Cyan, 90f);
        arrow = arrowImage.rectTransform;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        Button target = GetTargetButton(out GameObject targetPanel);
        bool show = target != null &&
                    target.gameObject.activeInHierarchy &&
                    (targetPanel == null || !targetPanel.activeInHierarchy) &&
                    !UIController.IsUiHidden;

        SetVisible(show);
        if (!show)
        {
            return;
        }

        RectTransform targetRect = (RectTransform)target.transform;
        Vector3 localCenter = root.InverseTransformPoint(targetRect.TransformPoint(targetRect.rect.center));

        float pulse = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
        ring.anchoredPosition = localCenter;
        ring.sizeDelta = Vector2.one * Mathf.Lerp(46f, 58f, pulse);
        ringImage.color = new Color(UiKit.Cyan.r, UiKit.Cyan.g, UiKit.Cyan.b, Mathf.Lerp(1f, 0.35f, pulse));

        arrow.anchoredPosition = (Vector2)localCenter + new Vector2(0f, -42f - (pulse * 6f));
    }

    private void SetVisible(bool visible)
    {
        if (ringImage.enabled != visible)
        {
            ringImage.enabled = visible;
            arrowImage.enabled = visible;
        }
    }

    private static Button GetTargetButton(out GameObject panel)
    {
        panel = null;
        UIController ui = UIController.Instance;
        DirectiveManager directives = DirectiveManager.Instance;
        if (ui == null || directives == null || directives.Active.Count == 0)
        {
            return null;
        }

        // Only the first goal on the list gets a nudge. Pulsing three buttons at once
        // would be noise rather than guidance.
        DirectiveHighlight highlight = directives.Active[0].definition.highlight;
        switch (highlight)
        {
            case DirectiveHighlight.Build: panel = ui.buildPanel; return ui.buildMenuButton;
            case DirectiveHighlight.Trains: panel = ui.trainPanel; return ui.trainMenuButton;
            case DirectiveHighlight.Platforms: panel = ui.platformPanel; return ui.platformMenuButton;
            case DirectiveHighlight.Staff: panel = ui.staffPanel; return ui.staffMenuButton;
            case DirectiveHighlight.Expansions: panel = ui.expansionPanel; return ui.expansionMenuButton;
            case DirectiveHighlight.Ratings: panel = ui.ratingsPanel; return ui.ratingsMenuButton;
            case DirectiveHighlight.Progression: panel = ui.progressionPanel; return ui.progressionMenuButton;
            default: return null;
        }
    }
}
