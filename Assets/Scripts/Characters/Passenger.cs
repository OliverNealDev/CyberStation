using System.Collections.Generic;
using UnityEngine;

public class Passenger : Person
{
    protected override bool UseRandomizedNavAgentSpeed => false;
    private const float SharedMoveSpeed = 3.5f;

    public TrainService assignedTrainService;
    public float TimeToGoToPlatform;

    [Header("Movement")]
    [Tooltip("NavMeshAgent acceleration controls both speeding up and how firmly passengers brake.")]
    [SerializeField] [Min(0.1f)] private float movementAcceleration = 18f;
    [SerializeField] [Min(0f)] private float interactionTurnSpeedDegreesPerSecond = 360f;
    
    [Header("Visuals")]
    [Tooltip("Assign the specific MeshRenderer (e.g. Visor, ID Badge) that should glow with the Hue color.")]
    public MeshRenderer visorRenderer;
    
    public bool hasTicket = false;
    public bool isTicketEvader = false;
    public bool hasBypassedBarrier = false;
    public bool isBeingEscorted = false;
    public bool hasBeenInspected = false;
    
    public bool hasFailedNeed = false;
    public bool hasGivenUpNeed = false;
    [HideInInspector] public NeedType blockedNeed = NeedType.None;
    [HideInInspector] public float blockedNeedStartTime;
    [HideInInspector] public float nextBlockedNeedCheckTime;
    [HideInInspector] public int blockedNeedFailureStage;
    [HideInInspector] public PassengerNeedIconController blockedNeedIcon;
    [HideInInspector] public Vector3 blockedNeedWanderCenter;
    [HideInInspector] public float nextBlockedNeedWanderTime;
    
    public QueuableObject currentTarget;
    public Vector3 trainWaitPosition;
    
    public bool needsHunger;
    public bool needsThirst;
    public bool needsEnergy;
    public bool needsHygiene;
    public List<GameObject> potentialLitterables = new List<GameObject>();
    [HideInInspector] public bool shouldUseFacilitiesBeforeExit = false;
    
    public passengerMasterStates currentMasterState = passengerMasterStates.InStation;
    public enum passengerMasterStates { InStation, OnPlatform, OnTrain }
    
    public passengerSubStates currentSubState = passengerSubStates.Idle;
    public enum passengerSubStates { Idle, MovingToTarget, InteractingWithSomething }
    
    public passengerSpecialTargets currentSpecialTarget = passengerSpecialTargets.None;
    public enum passengerSpecialTargets { None, Platform, TrainDoor, Exit, BlockedNeedWander }
    
    public enum NeedType { None, Ticket, Hunger, Thirst, Energy, Hygiene }

    public float InteractionTurnSpeedDegreesPerSecond => interactionTurnSpeedDegreesPerSecond;

    protected override void OnTick(float tickLength) { }

    protected override void Awake()
    {
        base.Awake();
        potentialLitterables.Clear();

        if (navAgent != null)
        {
            navAgent.speed = SharedMoveSpeed;
            navAgent.acceleration = movementAcceleration;
            navAgent.autoBraking = true;
        }
    }

    public void RollNeeds(bool hungerUnlocked, bool thirstUnlocked, bool energyUnlocked, bool hygieneUnlocked, bool requireAtLeastOne = false)
    {
        PassengerManager manager = PassengerManager.Instance;
        float hungerChance = manager != null ? manager.hungerNeedChance : 0.4f;
        float thirstChance = manager != null ? manager.thirstNeedChance : 0.4f;
        float energyChance = manager != null ? manager.energyNeedChance : 0.15f;
        float hygieneChance = manager != null ? manager.hygieneNeedChance : 0.2f;

        needsHunger = hungerUnlocked && Random.value < hungerChance;
        needsThirst = thirstUnlocked && Random.value < thirstChance;
        needsEnergy = energyUnlocked && Random.value < energyChance;
        needsHygiene = hygieneUnlocked && Random.value < hygieneChance;

        if (requireAtLeastOne && !HasAnyNeed())
        {
            EnsureAtLeastOneNeed(hungerUnlocked, thirstUnlocked, energyUnlocked, hygieneUnlocked);
        }
    }
    
    public NeedType GetNextNeed()
    {
        if (needsHunger) return NeedType.Hunger;
        if (needsThirst) return NeedType.Thirst;
        if (needsEnergy) return NeedType.Energy;
        if (needsHygiene) return NeedType.Hygiene;
        
        return NeedType.None;
    }

    public void ClearNeed(NeedType need)
    {
        if (need == NeedType.Hunger) needsHunger = false;
        if (need == NeedType.Thirst) needsThirst = false;
        if (need == NeedType.Energy) needsEnergy = false;
        if (need == NeedType.Hygiene) needsHygiene = false;
    }

    public bool HasAnyNeed()
    {
        return needsHunger || needsThirst || needsEnergy || needsHygiene;
    }

    public bool HasPotentialLitter()
    {
        return potentialLitterables != null && potentialLitterables.Count > 0;
    }

    private void EnsureAtLeastOneNeed(bool hungerUnlocked, bool thirstUnlocked, bool energyUnlocked, bool hygieneUnlocked)
    {
        if (HasAnyNeed())
        {
            return;
        }

        NeedType[] availableNeeds = new NeedType[4];
        int availableNeedCount = 0;

        if (hungerUnlocked)
        {
            availableNeeds[availableNeedCount++] = NeedType.Hunger;
        }

        if (thirstUnlocked)
        {
            availableNeeds[availableNeedCount++] = NeedType.Thirst;
        }

        if (energyUnlocked)
        {
            availableNeeds[availableNeedCount++] = NeedType.Energy;
        }

        if (hygieneUnlocked)
        {
            availableNeeds[availableNeedCount++] = NeedType.Hygiene;
        }

        if (availableNeedCount == 0)
        {
            return;
        }

        NeedType guaranteedNeed = availableNeeds[Random.Range(0, availableNeedCount)];
        if (guaranteedNeed == NeedType.Hunger) needsHunger = true;
        if (guaranteedNeed == NeedType.Thirst) needsThirst = true;
        if (guaranteedNeed == NeedType.Energy) needsEnergy = true;
        if (guaranteedNeed == NeedType.Hygiene) needsHygiene = true;
    }
}
