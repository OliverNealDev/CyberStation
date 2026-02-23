using System.Collections;
using UnityEngine;

public class TrainDoorController : QueuableObject
{
    public enum MachineState { Exiting, Entering }
    public MachineState state = MachineState.Exiting;
    
    private bool isProcessing = false;
    
    public override bool IsAvailable => state == MachineState.Entering && !isProcessing;

    public void StartBoardingProcess(TrainService arrivingService)
    {
        state = MachineState.Exiting;
        int exitingCount = Random.Range(0, Mathf.FloorToInt(arrivingService.trainData.capacityPerCarriage / 8f) + 1); // 2 doors per carriage, so max half capacity can exit from one door
        StartCoroutine(SpawnExitingPassengers(exitingCount));
    }

    private IEnumerator SpawnExitingPassengers(int count)
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
                Passenger exitingPassenger = PassengerManager.Instance.SpawnExitingPassenger(startPosition, outwardRotation);
                
                if (exitingPassenger != null)
                {
                    StartCoroutine(AnimateExiting(exitingPassenger, startPosition, targetPosition));
                }
            }
            yield return new WaitForSeconds(0.6f); 
        }
        
        yield return new WaitForSeconds(1f); 
        
        state = MachineState.Entering;
        
        // NEW: Tell the manager to wake up anyone waiting for THIS specific train
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
}