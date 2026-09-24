using System;
using System.Collections.Generic;
using UnityEngine;

// Runs one announced event at a time: a warning so the player can prepare, the event
// itself, then a result. Events only bend numbers in StationModifiers, so every system
// they touch was already built to handle the situation they create.
public class StationEventManager : MonoBehaviour
{
    public static StationEventManager Instance { get; private set; }

    public enum EventStage
    {
        Idle,
        Warning,
        Active
    }

    public static event Action OnEventStageChanged;
    public static event Action<StationEventDefinition, bool, int> OnEventResolved;

    private const float FirstEventDelaySeconds = 150f;
    private const int InspectionBaseReward = 300;
    private const int InspectionBaseXp = 60;
    private const int InspectionFine = 150;
    private const int MinimumPassengersForEvents = 6;

    private EventStage stage = EventStage.Idle;
    private StationEventDefinition currentEvent;
    private StationEventType? lastEventType;
    private float stageTimer;
    private float stageDuration;
    private float nextEventCountdown = FirstEventDelaySeconds;
    private int heldPlatformNumber = -1;
    private string heldPlatformName;
    private float inspectionThreshold;
    private float inspectionRatingTotal;
    private int inspectionSampleCount;
    private float inspectionSampleTimer;

    public EventStage Stage => stage;
    public StationEventDefinition CurrentEvent => currentEvent;
    public float StageRemaining => Mathf.Max(0f, stageDuration - stageTimer);
    public float StageProgress01 => stageDuration > 0f ? Mathf.Clamp01(stageTimer / stageDuration) : 1f;
    public float InspectionThreshold => inspectionThreshold;
    public float InspectionAverage => inspectionSampleCount > 0 ? inspectionRatingTotal / inspectionSampleCount : 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            StationModifiers.ClearEventModifiers();
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        switch (stage)
        {
            case EventStage.Idle:
                UpdateIdle(deltaTime);
                break;
            case EventStage.Warning:
                stageTimer += deltaTime;
                if (stageTimer >= stageDuration)
                {
                    BeginActive();
                }
                break;
            case EventStage.Active:
                stageTimer += deltaTime;
                if (currentEvent != null && currentEvent.isInspection)
                {
                    SampleInspection(deltaTime);
                }

                if (stageTimer >= stageDuration)
                {
                    Resolve();
                }
                break;
        }
    }

    public string GetActiveDescription()
    {
        if (currentEvent == null)
        {
            return string.Empty;
        }

        if (currentEvent.holdsPlatform)
        {
            return string.Format(currentEvent.activeText, heldPlatformName);
        }

        if (currentEvent.isInspection)
        {
            return string.Format(currentEvent.activeText, inspectionThreshold.ToString("0.0"));
        }

        return currentEvent.activeText;
    }

    private void UpdateIdle(float deltaTime)
    {
        // No surprises during the tutorial: a player still learning what a Materializer
        // is should not also be handling a signal fault.
        if (DirectiveManager.Instance == null || !DirectiveManager.Instance.IsOnboardingComplete)
        {
            return;
        }

        if (PassengerManager.Instance == null || PassengerManager.Instance.activePassengers.Count < MinimumPassengersForEvents)
        {
            return;
        }

        nextEventCountdown -= deltaTime;
        if (nextEventCountdown > 0f)
        {
            return;
        }

        nextEventCountdown = StationEventScheduler.GetNextEventDelay(UnityEngine.Random.value);
        TryStartWarning();
    }

    private void TryStartWarning()
    {
        int tier = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 1;
        int lineCount = TrainManager.Instance != null ? TrainManager.Instance.activeTrainServices.Count : 0;
        List<StationEventDefinition> eligible = StationEventScheduler.GetEligible(tier, lineCount, lastEventType);

        if (!TryFindPlatformToHold(out PlatformController platformToHold))
        {
            eligible.RemoveAll(definition => definition.holdsPlatform);
        }

        StationEventDefinition picked = StationEventScheduler.PickWeighted(eligible, UnityEngine.Random.value);
        if (picked == null)
        {
            return;
        }

        currentEvent = picked;
        lastEventType = picked.type;
        heldPlatformNumber = picked.holdsPlatform ? platformToHold.platformNumber : -1;
        heldPlatformName = picked.holdsPlatform ? platformToHold.platformName : null;
        inspectionThreshold = picked.isInspection ? StationEventCatalogue.GetInspectionThreshold(tier) : 0f;
        SetStage(EventStage.Warning, picked.warningSeconds);
    }

    private void BeginActive()
    {
        StationModifiers.ApplyEvent(currentEvent);
        if (currentEvent.holdsPlatform && heldPlatformNumber >= 0)
        {
            StationModifiers.HoldPlatform(heldPlatformNumber);
        }

        inspectionRatingTotal = 0f;
        inspectionSampleCount = 0;
        inspectionSampleTimer = 0f;
        SetStage(EventStage.Active, currentEvent.durationSeconds);
    }

    private void SampleInspection(float deltaTime)
    {
        inspectionSampleTimer += deltaTime;
        if (inspectionSampleTimer < 1f || RatingManager.Instance == null)
        {
            return;
        }

        inspectionSampleTimer -= 1f;
        inspectionRatingTotal += RatingManager.Instance.stationRating;
        inspectionSampleCount++;
    }

    private void Resolve()
    {
        StationEventDefinition finished = currentEvent;
        StationModifiers.ClearEventModifiers();

        bool success = true;
        int moneyChange = 0;

        if (finished != null && finished.isInspection)
        {
            success = InspectionAverage >= inspectionThreshold;
            int tier = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 1;

            if (success)
            {
                moneyChange = Mathf.RoundToInt(InspectionBaseReward * (1f + (0.25f * (tier - 1))));
                if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(moneyChange, false);
                if (ProgressionManager.Instance != null) ProgressionManager.Instance.AddXp(InspectionBaseXp);
                if (StationStatsTracker.Instance != null) StationStatsTracker.Instance.RecordInspectionPassed();
            }
            else
            {
                moneyChange = -InspectionFine;
                if (EconomyManager.Instance != null) EconomyManager.Instance.SpendMoney(InspectionFine, null, true);
            }
        }

        currentEvent = null;
        heldPlatformNumber = -1;
        heldPlatformName = null;
        SetStage(EventStage.Idle, 0f);
        OnEventResolved?.Invoke(finished, success, moneyChange);
    }

    private void SetStage(EventStage newStage, float duration)
    {
        stage = newStage;
        stageTimer = 0f;
        stageDuration = duration;
        OnEventStageChanged?.Invoke();
    }

    private static bool TryFindPlatformToHold(out PlatformController platformToHold)
    {
        platformToHold = null;
        if (TrainManager.Instance == null)
        {
            return false;
        }

        List<PlatformController> candidates = new List<PlatformController>();
        for (int i = 0; i < TrainManager.Instance.activePlatforms.Count; i++)
        {
            PlatformController platform = TrainManager.Instance.activePlatforms[i];
            if (platform != null && (platform.trainInSlot1 != null || platform.trainInSlot2 != null))
            {
                candidates.Add(platform);
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        platformToHold = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }
}
