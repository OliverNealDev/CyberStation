using System.Collections;
using UnityEngine;

public class TrainDoorController : QueuableObject
{
    public enum MachineState { Exiting, Entering }
    public MachineState state = MachineState.Exiting;
    
    private bool isProcessing = false;

    [Header("Boarding Crowd")]
    [Min(1)] public int crowdSlotsPerRing = 5;
    [Min(0.1f)] public float crowdInnerRadius = 1.1f;
    [Min(0.1f)] public float crowdRingSpacing = 0.55f;
    [Min(0f)] public float crowdStopDistanceBase = 0.35f;
    [Min(0f)] public float crowdStopDistanceStep = 0.12f;
    
    public override bool IsAvailable => state == MachineState.Entering && !isProcessing;

    public override Vector3 GetQueuePositionFor(Person person)
    {
        int index = PeopleOnWay.IndexOf(person);
        if (index < 0)
        {
            return transform.position + (transform.forward * crowdInnerRadius);
        }

        int slotsPerRing = Mathf.Max(1, crowdSlotsPerRing);
        int ring = index / slotsPerRing;
        int slot = index % slotsPerRing;

        float radius = crowdInnerRadius + (ring * crowdRingSpacing);
        float angle = GetCrowdAngle(slot);
        Vector3 crowdDirection = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
        return transform.position + (crowdDirection * radius);
    }

    public float GetCrowdStoppingDistanceFor(Person person)
    {
        int index = PeopleOnWay.IndexOf(person);
        if (index < 0)
        {
            return crowdStopDistanceBase;
        }

        int ring = index / Mathf.Max(1, crowdSlotsPerRing);
        return crowdStopDistanceBase + (ring * crowdStopDistanceStep);
    }

    public void StartBoardingProcess(TrainService arrivingService)
    {
        state = MachineState.Exiting;
        int exitingCount = Random.Range(0, Mathf.FloorToInt(arrivingService.trainData.capacityPerCarriage / 8f) + 1); 
        StartCoroutine(SpawnExitingPassengers(exitingCount, arrivingService));
    }

    private IEnumerator SpawnExitingPassengers(int count, TrainService arrivingService)
    {
        for(int i = 0; i < count; i++)
        {
            if (PassengerManager.Instance != null)
            {
                Vector3 startPosition = transform.position - (transform.forward * 0.75f);
                startPosition.y = 0.07061052f;
                
                Vector3 targetPosition = transform.position + (transform.forward * 1.5f);
                targetPosition.y = 0.07061052f;
                
                Quaternion outwardRotation = Quaternion.LookRotation(transform.forward);
                Passenger exitingPassenger = PassengerManager.Instance.SpawnExitingPassenger(startPosition, outwardRotation, arrivingService);
                
                if (exitingPassenger != null)
                {
                    StartCoroutine(AnimateExiting(exitingPassenger, startPosition, targetPosition));
                }
            }
            yield return new WaitForSeconds(0.6f); 
        }
        
        yield return new WaitForSeconds(1f); 
        
        state = MachineState.Entering;
        
        if (PassengerManager.Instance != null)
        {
            TrainController controller = GetComponentInParent<TrainController>();
            if (controller != null)
            {
                PassengerManager.Instance.NotifyDoorsReady(controller.trainService);
            }
        }
    }

    private IEnumerator AnimateExiting(Passenger passenger, Vector3 startPos, Vector3 targetPos)
    {
        targetPos.y = startPos.y;

        float duration = 0.4f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (passenger == null) yield break; 

            passenger.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            
            yield return null; 
        }

        if (passenger != null)
        {
            PassengerManager.Instance.FinaliseExitingPassenger(passenger);
        }
    }
    
    public void CloseDoors()
    {
        for (int i = PeopleOnWay.Count - 1; i >= 0; i--)
        {
            if (PeopleOnWay[i] is Passenger passenger)
            {
                passenger.currentTarget = null;
            }
        }
        
        PeopleOnWay.Clear();
    }

    public override void ProcessInteraction(Person person)
    {
        if (state == MachineState.Entering && !isProcessing)
        {
            isProcessing = true;
            Passenger passenger = (Passenger)person;
            
            StartCoroutine(AnimateBoarding(passenger));
        }
    }

    private IEnumerator AnimateBoarding(Passenger passenger)
    {
        if (passenger.navAgent != null)
        {
            passenger.navAgent.enabled = false;
        }

        Vector3 startPosition = passenger.transform.position;
        
        Vector3 endPosition = transform.position - (transform.forward * 0.75f);
        
        endPosition.y = startPosition.y; 

        float duration = 0.4f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (passenger == null) yield break; 

            passenger.transform.position = Vector3.Lerp(startPosition, endPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            
            yield return null; 
        }

        if (passenger != null)
        {
            PassengerManager.Instance.BoardTrain(passenger);
        }

        isProcessing = false;
    }

    private float GetCrowdAngle(int slot)
    {
        if (slot == 0)
        {
            return 0f;
        }

        int step = ((slot - 1) / 2) + 1;
        float sign = slot % 2 == 1 ? -1f : 1f;
        return sign * Mathf.Min(78f, step * 18f);
    }
}
