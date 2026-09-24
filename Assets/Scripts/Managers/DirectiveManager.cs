using System;
using System.Collections.Generic;
using UnityEngine;

// Hands the player a short list of goals, pays out when they are met, and keeps the next
// ones coming. The first few double as the tutorial: they are the three things a station
// needs before anything moves at all, which the game never used to say out loud.
public class DirectiveManager : MonoBehaviour, ISaveParticipant
{
    public static DirectiveManager Instance { get; private set; }

    public static event Action OnDirectivesChanged;
    public static event Action<DirectiveDefinition> OnDirectiveCompleted;
    public static event Action<int> OnLegacyCatchUp;

    private const float EvaluationInterval = 0.5f;
    private const int OnboardingCapacity = 1;
    private const int NormalCapacity = 3;
    private const int ContractCapacity = 2;
    private const int MinimumPassengersForRatingGoal = 8;

    public class ActiveDirective
    {
        public DirectiveDefinition definition;
        public float baseline;
        public int contractSerial = -1;
        public int contractTier = 1;
        public float progress01;
        public string progressText;
    }

    [Serializable]
    private class ActiveDirectiveSaveData
    {
        public string id;
        public float baseline;
        public int contractSerial = -1;
        public int contractTier = 1;
    }

    [Serializable]
    private class DirectiveSaveData
    {
        public List<string> completed = new List<string>();
        public List<ActiveDirectiveSaveData> active = new List<ActiveDirectiveSaveData>();
        public int nextContractSerial;
    }

    private readonly HashSet<string> completedIds = new HashSet<string>();
    private readonly List<ActiveDirective> active = new List<ActiveDirective>();
    private int nextContractSerial;
    private bool isReady;
    private int pendingCatchUpAnnouncement;
    private float evaluationTimer;

    public IReadOnlyList<ActiveDirective> Active => active;

    public bool IsOnboardingComplete
    {
        get
        {
            for (int i = 0; i < DirectiveCatalogue.OnboardingDirectiveCount; i++)
            {
                if (!completedIds.Contains(DirectiveCatalogue.Chain[i].id))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public string SaveKey => "directives";

    private void Awake()
    {
        Instance = this;
        SaveManager.RegisterParticipant(this);
    }

    private void OnDestroy()
    {
        SaveManager.UnregisterParticipant(this);
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        ProgressionManager.OnTierReached += HandleTierReached;
    }

    private void OnDisable()
    {
        ProgressionManager.OnTierReached -= HandleTierReached;
    }

    private void Update()
    {
        // Nothing pays out behind the title card or a day report, where the toast
        // announcing it would go unseen.
        if (!isReady || (GameSpeedManager.Instance != null && GameSpeedManager.Instance.IsLocked))
        {
            return;
        }

        // Unscaled, so a goal like "build a Ticket Machine" still ticks off while paused.
        evaluationTimer += Time.unscaledDeltaTime;
        if (evaluationTimer < EvaluationInterval)
        {
            return;
        }

        evaluationTimer = 0f;

        if (pendingCatchUpAnnouncement > 0)
        {
            OnLegacyCatchUp?.Invoke(pendingCatchUpAnnouncement);
            pendingCatchUpAnnouncement = 0;
        }

        Evaluate();
    }

    private void HandleTierReached(int tier)
    {
        if (isReady)
        {
            Refill();
        }
    }

    private void Evaluate()
    {
        bool changed = false;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            ActiveDirective directive = active[i];
            float value = GetMetricValue(directive.definition.metric);
            float progress = DirectiveRules.GetProgress01(directive.definition, value, directive.baseline);
            string progressText = DirectiveRules.FormatProgress(directive.definition, value, directive.baseline);

            if (!Mathf.Approximately(progress, directive.progress01) || progressText != directive.progressText)
            {
                directive.progress01 = progress;
                directive.progressText = progressText;
                changed = true;
            }

            if (DirectiveRules.IsComplete(directive.definition, value, directive.baseline))
            {
                Complete(directive);
                changed = true;
            }
        }

        if (changed)
        {
            OnDirectivesChanged?.Invoke();
        }
    }

    private void Complete(ActiveDirective directive)
    {
        active.Remove(directive);

        if (directive.contractSerial < 0)
        {
            completedIds.Add(directive.definition.id);
        }

        if (directive.definition.rewardMoney > 0 && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(directive.definition.rewardMoney, false);
        }

        if (directive.definition.rewardXp > 0 && ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.AddXp(directive.definition.rewardXp);
        }

        OnDirectiveCompleted?.Invoke(directive.definition);
        Refill();
    }

    private void Refill()
    {
        int tier = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 1;
        bool onboarding = !IsOnboardingComplete;
        int capacity = onboarding ? OnboardingCapacity : NormalCapacity;
        bool added = false;

        for (int i = 0; i < DirectiveCatalogue.Chain.Count && active.Count < capacity; i++)
        {
            DirectiveDefinition definition = DirectiveCatalogue.Chain[i];
            if (completedIds.Contains(definition.id) || IsActive(definition.id) || definition.minTier > tier)
            {
                continue;
            }

            Activate(definition, -1, tier);
            added = true;

            // The tutorial is strictly one step at a time, in order.
            if (onboarding)
            {
                break;
            }
        }

        // Contracts only fill slots the chain cannot: they never jump ahead of an authored goal.
        if (!onboarding && !HasWaitingChainDirective(tier))
        {
            while (CountContracts() < ContractCapacity && active.Count < capacity)
            {
                int serial = nextContractSerial++;
                Activate(DirectiveCatalogue.CreateContract(serial, tier), serial, tier);
                added = true;
            }
        }

        if (added)
        {
            OnDirectivesChanged?.Invoke();
        }
    }

    private void Activate(DirectiveDefinition definition, int contractSerial, int tier)
    {
        float baseline = definition.IsCounter ? GetMetricValue(definition.metric) : 0f;
        active.Add(new ActiveDirective
        {
            definition = definition,
            baseline = baseline,
            contractSerial = contractSerial,
            contractTier = tier,
            progress01 = 0f,
            progressText = DirectiveRules.FormatProgress(definition, definition.IsCounter ? baseline : GetMetricValue(definition.metric), baseline)
        });
    }

    private bool HasWaitingChainDirective(int tier)
    {
        for (int i = 0; i < DirectiveCatalogue.Chain.Count; i++)
        {
            DirectiveDefinition definition = DirectiveCatalogue.Chain[i];
            if (!completedIds.Contains(definition.id) && !IsActive(definition.id) && definition.minTier <= tier)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsActive(string id)
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].definition.id == id)
            {
                return true;
            }
        }

        return false;
    }

    private int CountContracts()
    {
        int count = 0;
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].contractSerial >= 0)
            {
                count++;
            }
        }

        return count;
    }

    // A station saved before directives existed has already done most of the tutorial.
    // Rather than pay out a pile of rewards for work done in an older version, anything
    // already true is marked done quietly and the player picks up from where they are.
    private void CatchUpLegacyStation()
    {
        int caughtUp = 0;
        for (int i = 0; i < DirectiveCatalogue.Chain.Count; i++)
        {
            DirectiveDefinition definition = DirectiveCatalogue.Chain[i];
            if (definition.IsCounter || completedIds.Contains(definition.id))
            {
                continue;
            }

            if (DirectiveRules.IsComplete(definition, GetMetricValue(definition.metric), 0f))
            {
                completedIds.Add(definition.id);
                caughtUp++;
            }
        }

        bool stationRuns = completedIds.Contains(DirectiveCatalogue.Chain[0].id) &&
                           completedIds.Contains(DirectiveCatalogue.Chain[1].id) &&
                           completedIds.Contains(DirectiveCatalogue.Chain[2].id);

        if (stationRuns)
        {
            for (int i = 0; i < DirectiveCatalogue.OnboardingDirectiveCount; i++)
            {
                if (completedIds.Add(DirectiveCatalogue.Chain[i].id))
                {
                    caughtUp++;
                }
            }
        }

        active.Clear();
        Refill();
        OnDirectivesChanged?.Invoke();

        // Announced on the first unlocked evaluation, so the player is past the title card to see it.
        pendingCatchUpAnnouncement = caughtUp;
    }

    public float GetMetricValue(DirectiveMetric metric)
    {
        switch (metric)
        {
            case DirectiveMetric.Materializers:
                return PassengerManager.Instance != null ? PassengerManager.Instance.materializers.Count : 0f;
            case DirectiveMetric.TicketMachines:
                return CountFacilities(FacilityType.TicketMachine);
            case DirectiveMetric.ThirstFacilities:
                return CountFacilities(FacilityType.HydratingObelisk) + CountFacilities(FacilityType.BottleDispenser);
            case DirectiveMetric.HungerFacilities:
                return CountFacilities(FacilityType.NutrientExtruder) + CountFacilities(FacilityType.SnackPrinter);
            case DirectiveMetric.FoodVariety:
                return (CountFacilities(FacilityType.NutrientExtruder) > 0 ? 1f : 0f) + (CountFacilities(FacilityType.SnackPrinter) > 0 ? 1f : 0f);
            case DirectiveMetric.EnergyFacilities:
                return CountFacilities(FacilityType.EnergyBottleDispenser) + CountFacilities(FacilityType.RestPad);
            case DirectiveMetric.HygieneFacilities:
                return CountFacilities(FacilityType.CleansingShower) + CountFacilities(FacilityType.PrivateLavatory) + CountFacilities(FacilityType.MolecularScrubber);
            case DirectiveMetric.Decorations:
                return CountDecorations();
            case DirectiveMetric.ActiveLines:
                return TrainManager.Instance != null ? TrainManager.Instance.activeTrainServices.Count : 0f;
            case DirectiveMetric.StationRating:
                return GetOperatingStationRating();
            case DirectiveMetric.Tier:
                return ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 1f;
            case DirectiveMetric.Janitors:
                return CountStaff<Janitor>();
            case DirectiveMetric.SecurityDrones:
                return CountStaff<SecurityDrone>();
            case DirectiveMetric.Expansions:
                return ExpansionManager.Instance != null ? ExpansionManager.Instance.builtExpansions.Count : 0f;
            case DirectiveMetric.TodayIncome:
                return StationStatsTracker.Instance != null ? StationStatsTracker.Instance.Today.income : 0f;
        }

        StationStatsTracker.LifetimeStats lifetime = StationStatsTracker.Instance != null ? StationStatsTracker.Instance.Lifetime : null;
        if (lifetime == null)
        {
            return 0f;
        }

        switch (metric)
        {
            case DirectiveMetric.TicketsSold: return lifetime.ticketsSold;
            case DirectiveMetric.PassengersBoarded: return lifetime.passengersBoarded;
            case DirectiveMetric.NeedsMet: return lifetime.needsMet;
            case DirectiveMetric.EvadersCaught: return lifetime.evadersCaught;
            case DirectiveMetric.DaysGradedB: return lifetime.daysGradedB;
            case DirectiveMetric.DaysGradedA: return lifetime.daysGradedA;
            case DirectiveMetric.RushHoursHeld: return lifetime.rushHoursHeld;
            case DirectiveMetric.InspectionsPassed: return lifetime.inspectionsPassed;
            default: return 0f;
        }
    }

    // An empty concourse scores close to five stars because nothing is going wrong in it.
    // A rating goal only counts once there are enough people in the station to judge it.
    private static float GetOperatingStationRating()
    {
        if (RatingManager.Instance == null || PassengerManager.Instance == null ||
            PassengerManager.Instance.activePassengers.Count < MinimumPassengersForRatingGoal)
        {
            return 0f;
        }

        return RatingManager.Instance.stationRating;
    }

    private static float CountFacilities(FacilityType type)
    {
        if (FacilityManager.Instance == null)
        {
            return 0f;
        }

        List<StationFacility> facilities = FacilityManager.Instance.GetFacilities(type);
        return facilities != null ? facilities.Count : 0f;
    }

    private static float CountDecorations()
    {
        PlacedBuildable[] placed = FindObjectsByType<PlacedBuildable>(FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < placed.Length; i++)
        {
            if (placed[i] != null && placed[i].HasDecoration)
            {
                count++;
            }
        }

        return count;
    }

    private static float CountStaff<T>() where T : Staff
    {
        if (StaffManager.Instance == null)
        {
            return 0f;
        }

        int count = 0;
        for (int i = 0; i < StaffManager.Instance.hiredStaff.Count; i++)
        {
            if (StaffManager.Instance.hiredStaff[i] is T)
            {
                count++;
            }
        }

        return count;
    }

    public string CaptureState()
    {
        DirectiveSaveData data = new DirectiveSaveData
        {
            completed = new List<string>(completedIds),
            nextContractSerial = nextContractSerial
        };

        for (int i = 0; i < active.Count; i++)
        {
            data.active.Add(new ActiveDirectiveSaveData
            {
                id = active[i].definition.id,
                baseline = active[i].baseline,
                contractSerial = active[i].contractSerial,
                contractTier = active[i].contractTier
            });
        }

        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        DirectiveSaveData data = JsonUtility.FromJson<DirectiveSaveData>(json);
        completedIds.Clear();
        active.Clear();

        if (data != null)
        {
            if (data.completed != null)
            {
                completedIds.UnionWith(data.completed);
            }

            nextContractSerial = data.nextContractSerial;

            if (data.active != null)
            {
                for (int i = 0; i < data.active.Count; i++)
                {
                    ActiveDirectiveSaveData saved = data.active[i];
                    DirectiveDefinition definition = saved.contractSerial >= 0
                        ? DirectiveCatalogue.CreateContract(saved.contractSerial, saved.contractTier)
                        : DirectiveCatalogue.FindChainDirective(saved.id);

                    // A directive removed in a later build is simply dropped.
                    if (definition == null)
                    {
                        continue;
                    }

                    active.Add(new ActiveDirective
                    {
                        definition = definition,
                        baseline = saved.baseline,
                        contractSerial = saved.contractSerial,
                        contractTier = saved.contractTier
                    });
                }
            }
        }

        isReady = true;
        Refill();
        OnDirectivesChanged?.Invoke();
    }

    public void OnNoSavedState(int loadedSaveVersion)
    {
        completedIds.Clear();
        active.Clear();
        nextContractSerial = 0;
        isReady = true;
        Refill();

        // Version 0 is a brand-new station. Anything else is a station from before
        // directives existed. SaveManager only calls this once the world is restored, so
        // the catch-up runs now: waiting would let a save written behind the title card
        // record an empty directive list and lose the migration for good.
        if (loadedSaveVersion > 0)
        {
            CatchUpLegacyStation();
        }

        OnDirectivesChanged?.Invoke();
    }
}
