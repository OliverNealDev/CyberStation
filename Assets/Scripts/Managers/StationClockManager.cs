using System;
using UnityEngine;

public class StationClockManager : MonoBehaviour, ISaveParticipant
{
    public static StationClockManager Instance { get; private set; }

    public static event Action<DayPhase> OnPhaseChanged;
    public static event Action<int> OnDayEnded;

    private float elapsedGameSeconds;
    private int currentDay = 1;
    private DayPhase currentPhase;

    public float ElapsedGameSeconds => elapsedGameSeconds;
    public int CurrentDay => currentDay;
    public DayPhase CurrentPhase => currentPhase;
    public float HourOfDay => StationClockMath.GetHourOfDay(elapsedGameSeconds);
    public bool IsRushHour => StationClockMath.IsRushHour(currentPhase);

    public string SaveKey => "clock";

    [Serializable]
    private class ClockSaveData
    {
        public float elapsedGameSeconds;
    }

    private void Awake()
    {
        Instance = this;
        SetElapsed(0f, false);
        SaveManager.RegisterParticipant(this);
    }

    private void OnDestroy()
    {
        SaveManager.UnregisterParticipant(this);
        if (Instance == this)
        {
            Instance = null;
            StationModifiers.DemandMultiplier = 1f;
        }
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        elapsedGameSeconds += Time.deltaTime;

        int day = StationClockMath.GetDay(elapsedGameSeconds);
        if (day != currentDay)
        {
            int finishedDay = currentDay;
            currentDay = day;
            OnDayEnded?.Invoke(finishedDay);
        }

        DayPhase phase = StationClockMath.GetPhase(HourOfDay);
        if (phase != currentPhase)
        {
            ApplyPhase(phase, true);
        }
    }

    private void SetElapsed(float seconds, bool notify)
    {
        elapsedGameSeconds = Mathf.Max(0f, seconds);
        currentDay = StationClockMath.GetDay(elapsedGameSeconds);
        ApplyPhase(StationClockMath.GetPhase(HourOfDay), notify);
    }

    private void ApplyPhase(DayPhase phase, bool notify)
    {
        currentPhase = phase;
        StationModifiers.DemandMultiplier = StationClockMath.GetDemandMultiplier(phase);

        if (notify)
        {
            OnPhaseChanged?.Invoke(phase);
        }
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new ClockSaveData { elapsedGameSeconds = elapsedGameSeconds });
    }

    public void RestoreState(string json)
    {
        ClockSaveData data = JsonUtility.FromJson<ClockSaveData>(json);
        // Restoring is not a phase transition: announcing one here would let listeners
        // score a rush hour that ended before the save was written.
        if (data != null)
        {
            SetElapsed(data.elapsedGameSeconds, false);
        }
    }

    public void OnNoSavedState(int loadedSaveVersion)
    {
    }
}
