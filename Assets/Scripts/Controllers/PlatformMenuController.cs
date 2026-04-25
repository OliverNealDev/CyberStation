using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlatformMenuController : MonoBehaviour
{
    private const int PlatformListBottomPadding = 120;

    public GameObject platformButtonPrefab;
    public Transform contentContainer;
    [SerializeField] private ScrollRect platformScrollRect;

    private Coroutine pendingScrollReset;

    void OnEnable()
    {
        ConfigureContentLayout();
        LoadPlatforms();
        QueueScrollReset();
        TrainManager.OnTrainAssignmentsChanged += LoadPlatforms;
    }

    void OnDisable()
    {
        TrainManager.OnTrainAssignmentsChanged -= LoadPlatforms;
        StopQueuedScrollReset();
        ClearSelectedMenuObject();
    }

    public void LoadPlatforms()
    {
        ConfigureContentLayout();
        UIRuntimeListUtility.ClearChildren(contentContainer);

        if (TrainManager.Instance == null)
        {
            UIRuntimeListUtility.RefreshLayout(contentContainer);
            return;
        }

        foreach (var platform in TrainManager.Instance.activePlatforms)
        {
            CreatePlatformButton(platform);
        }

        UIRuntimeListUtility.RefreshLayout(contentContainer);
    }

    private void CreatePlatformButton(PlatformController platform)
    {
        GameObject newButton = Instantiate(platformButtonPrefab, contentContainer);
        
        TextMeshProUGUI nameText = newButton.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        if (nameText) nameText.text = platform.platformName;

        Image iconImage = newButton.transform.Find("Icon").GetComponent<Image>();
        if (iconImage)
        {
            iconImage.sprite = platform.GetIcon();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
        }
        
        Button slot1Btn = newButton.transform.Find("TrainSlot1").GetComponent<Button>();
        Button slot2Btn = newButton.transform.Find("TrainSlot2").GetComponent<Button>();

        UpdateSlotVisuals(slot1Btn, platform.trainInSlot1);
        UpdateSlotVisuals(slot2Btn, platform.trainInSlot2);

        slot1Btn.onClick.AddListener(() => OnSlotClicked(platform, 1));
        slot2Btn.onClick.AddListener(() => OnSlotClicked(platform, 2));
    }

    private void UpdateSlotVisuals(Button slotBtn, Train train)
    {
        Image slotImage = slotBtn.GetComponent<Image>();
        if (train != null)
        {
            slotImage.sprite = train.GetIcon();
            slotImage.color = Color.white;
        }
        else
        {
            slotImage.color = new Color(1f, 1f, 1f, 0.35f);
        }
    }

    private void OnSlotClicked(PlatformController platform, int slotIndex)
    {
        TrainManager.Instance.pendingPlatform = platform;
        TrainManager.Instance.pendingSlot = slotIndex;
        
        UIController.Instance.OpenTrainSelectionPopup();
    }

    private void ConfigureContentLayout()
    {
        if (contentContainer == null || !contentContainer.TryGetComponent(out VerticalLayoutGroup layoutGroup))
        {
            return;
        }

        layoutGroup.childForceExpandHeight = false;

        RectOffset padding = layoutGroup.padding;
        if (padding == null || padding.bottom >= PlatformListBottomPadding)
        {
            return;
        }

        layoutGroup.padding = new RectOffset(
            padding.left,
            padding.right,
            padding.top,
            PlatformListBottomPadding);
    }

    private void QueueScrollReset()
    {
        StopQueuedScrollReset();
        pendingScrollReset = StartCoroutine(ResetScrollStateNextFrame());
    }

    private void StopQueuedScrollReset()
    {
        if (pendingScrollReset == null)
        {
            return;
        }

        StopCoroutine(pendingScrollReset);
        pendingScrollReset = null;
    }

    private IEnumerator ResetScrollStateNextFrame()
    {
        yield return null;
        pendingScrollReset = null;

        UIRuntimeListUtility.RefreshLayout(contentContainer);
        ClearSelectedMenuObject();

        ScrollRect scrollRect = ResolveScrollRect();
        if (scrollRect == null)
        {
            yield break;
        }

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;

        scrollRect.enabled = false;
        scrollRect.enabled = true;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private ScrollRect ResolveScrollRect()
    {
        if (platformScrollRect == null && contentContainer != null)
        {
            platformScrollRect = contentContainer.GetComponentInParent<ScrollRect>(true);
        }

        return platformScrollRect;
    }

    private void ClearSelectedMenuObject()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null && selectedObject.transform.IsChildOf(transform))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
