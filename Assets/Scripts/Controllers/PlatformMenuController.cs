using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlatformMenuController : MonoBehaviour
{
    public GameObject platformButtonPrefab;
    public Transform contentContainer;

    void OnEnable()
    {
        LoadPlatforms();
        TrainManager.OnTrainAssignmentsChanged += LoadPlatforms;
    }

    void OnDisable()
    {
        TrainManager.OnTrainAssignmentsChanged -= LoadPlatforms;
    }

    public void LoadPlatforms()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        if (TrainManager.Instance == null) return;

        foreach (var platform in TrainManager.Instance.activePlatforms)
        {
            CreatePlatformButton(platform);
        }
    }

    private void CreatePlatformButton(PlatformController platform)
    {
        GameObject newButton = Instantiate(platformButtonPrefab, contentContainer);
        
        TextMeshProUGUI nameText = newButton.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        if (nameText) nameText.text = platform.platformName;
        
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
            slotImage.sprite = train.icon;
            slotImage.color = Color.white;
        }
        else
        {
            slotImage.sprite = null;
            slotImage.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    private void OnSlotClicked(PlatformController platform, int slotIndex)
    {
        TrainManager.Instance.pendingPlatform = platform;
        TrainManager.Instance.pendingSlot = slotIndex;
        
        UIController.Instance.OpenTrainSelectionPopup();
    }
}