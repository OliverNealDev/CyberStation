using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainRowUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI timeText; 
    public Image trainIcon;
    public Image statusBackground; 

    private ScheduledArrival linkedData;
    
    private Color normalColor = Color.white;
    private Color delayedColor = new Color(1f, 0.4f, 0.4f); 
    private Color dueColor = new Color(0.5f, 1f, 0.5f); 

    private int lastSecondsRemaining = -999;
    private ArrivalStatus lastStatus = ArrivalStatus.OnTime;
    private bool lastSpawnedState = false;

    public void Setup(ScheduledArrival data)
    {
        linkedData = data;
        if(trainIcon != null) trainIcon.sprite = data.service.trainData.icon;
        ForceUpdateUI();
    }

    public string GetID()
    {
        return linkedData != null ? linkedData.uiID : "";
    }

    void Update()
    {
        if (linkedData != null)
        {
            UpdateUI();
        }
    }

    void ForceUpdateUI()
    {
        lastSecondsRemaining = -999; 
        UpdateUI();
    }

    void UpdateUI()
    {
        if (timeText == null) return;

        bool isDelayed = linkedData.status == ArrivalStatus.Delayed;
        bool hasSpawned = linkedData.hasSpawned;

        if (isDelayed)
        {
            if (timeText.color != delayedColor) timeText.color = delayedColor;
            
            int currentDelay = Mathf.FloorToInt(linkedData.delayDuration);
            if (currentDelay != lastSecondsRemaining || lastStatus != ArrivalStatus.Delayed)
            {
                timeText.text = $"Delayed {currentDelay}s";
                lastSecondsRemaining = currentDelay;
                lastStatus = ArrivalStatus.Delayed;
            }
            return;
        }
        else if (hasSpawned)
        {
            if (timeText.color != dueColor) timeText.color = dueColor;
            
            if (!lastSpawnedState)
            {
                timeText.text = "Due";
                lastSpawnedState = true;
            }
            return;
        }
        else
        {
            if (timeText.color != normalColor) timeText.color = normalColor;
            
            float timeRemaining = linkedData.arrivalTime - Time.time;
            int currentSeconds = Mathf.FloorToInt(timeRemaining);

            if (currentSeconds != lastSecondsRemaining || lastStatus != ArrivalStatus.OnTime)
            {
                lastSecondsRemaining = currentSeconds;
                lastStatus = ArrivalStatus.OnTime;

                if (timeRemaining <= 0) timeText.text = "Arriving...";
                else if (timeRemaining < 60) timeText.text = $"{currentSeconds}s";
                else
                {
                    int mins = Mathf.FloorToInt(timeRemaining / 60);
                    int secs = Mathf.FloorToInt(timeRemaining % 60);
                    timeText.text = $"{mins}m {secs}s";
                }
            }
        }
    }
}