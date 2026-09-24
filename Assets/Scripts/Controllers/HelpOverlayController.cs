using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Controls and the rules of the station on one card. Every control in the game used to be
// discoverable only by pressing keys to see what happened.
public class HelpOverlayController : MonoBehaviour
{
    public static HelpOverlayController Instance { get; private set; }

    private const string Controls =
        "<color=#19E6FF>WASD / right-drag</color><pos=48%>Move the camera\n" +
        "<color=#19E6FF>Scroll</color><pos=48%>Zoom\n" +
        "<color=#19E6FF>Q / E</color><pos=48%>Rotate the camera\n" +
        "<color=#19E6FF>R / right-click</color><pos=48%>Rotate a placement\n" +
        "<color=#19E6FF>Space</color><pos=48%>Pause / resume\n" +
        "<color=#19E6FF>1  2  3</color><pos=48%>Normal, fast, fastest\n" +
        "<color=#19E6FF>Y</color><pos=48%>Hide the interface\n" +
        "<color=#19E6FF>Esc</color><pos=48%>Close menus\n" +
        "<color=#19E6FF>F1</color><pos=48%>This help";

    private const string Rules =
        "<color=#FFD340>Passengers</color> arrive through a <b>Materializer</b>, buy a ticket, meet their needs, then walk to their platform.\n\n" +
        "<color=#FFD340>Six ratings</color> make up your stars. More stars means more passengers, which puts more pressure on your layout.\n\n" +
        "<color=#FFD340>Rush hours</color> in the morning and evening bring crowds. Nights are quiet: use them to rebuild.\n\n" +
        "<color=#FFD340>Events</color> are announced before they hit. Watch the card on the left.\n\n" +
        "<color=#FFD340>Each day</color> ends at midnight with a graded report. Good grades earn a subsidy.";

    private Canvas canvas;
    private RectTransform card;
    private bool isOpen;

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

        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Toggle();
        }
        else if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
        }
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (isOpen == open)
        {
            return;
        }

        isOpen = open;
        canvas.gameObject.SetActive(open);
        if (open)
        {
            StartCoroutine(UiTween.ScaleIn(card, 0.3f));
        }
    }

    private void Build()
    {
        // Above the title screen, which opens this card from its own menu.
        canvas = UiKit.CreateOverlayCanvas("HelpOverlay", 250);
        canvas.transform.SetParent(transform, false);
        RectTransform root = UiKit.CreateStretchRect("Root", canvas.transform);

        Image dim = UiKit.AddImage(UiKit.CreateStretchRect("Dim", root), new Color(0f, 0f, 0f, 0.6f), false);
        dim.raycastTarget = true;
        dim.gameObject.AddComponent<Button>().onClick.AddListener(() => SetOpen(false));

        Image cardImage = UiKit.CreatePanel("Card", root, Vector2.zero, new Vector2(1000f, 600f), UiKit.PanelColor);
        cardImage.raycastTarget = true;
        card = cardImage.rectTransform;
        Image border = UiKit.CreatePanel("Border", card, Vector2.zero, new Vector2(1000f, 600f), new Color(UiKit.Cyan.r, UiKit.Cyan.g, UiKit.Cyan.b, 0.35f));
        border.sprite = UiKit.RingSprite;

        TextMeshProUGUI title = UiKit.CreateText("Title", card, "HOW TO RUN A STATION", 40f, UiKit.TextColor, TextAlignmentOptions.Left,
            new Vector2(-120f, 250f), new Vector2(700f, 56f));
        title.characterSpacing = 8f;

        TextMeshProUGUI controlsHeader = UiKit.CreateText("ControlsHeader", card, "CONTROLS", 15f, UiKit.Cyan, TextAlignmentOptions.Left,
            new Vector2(-250f, 188f), new Vector2(440f, 22f));
        controlsHeader.characterSpacing = 8f;
        TextMeshProUGUI controls = UiKit.CreateText("Controls", card, Controls, 20f, UiKit.TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(-250f, -20f), new Vector2(440f, 390f));
        controls.lineSpacing = 18f;

        TextMeshProUGUI rulesHeader = UiKit.CreateText("RulesHeader", card, "THE BASICS", 15f, UiKit.Cyan, TextAlignmentOptions.Left,
            new Vector2(240f, 188f), new Vector2(440f, 22f));
        rulesHeader.characterSpacing = 8f;
        UiKit.CreateText("Rules", card, Rules, 18f, UiKit.TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(240f, -20f), new Vector2(440f, 390f));

        UiKit.CreateButton("Close", card, "GOT IT", new Vector2(390f, -250f), new Vector2(150f, 48f), () => SetOpen(false), 22f, UiKit.Cyan,
            new Color(0.02f, 0.03f, 0.05f, 1f));

        canvas.gameObject.SetActive(false);
    }
}
