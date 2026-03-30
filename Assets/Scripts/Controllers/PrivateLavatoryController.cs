using UnityEngine;
using System.Collections;

public class PrivateLavatoryController : StationFacility
{
    public int usePrice = 15; 
    public override float EstimatedServiceDuration => ((minUsageTime + maxUsageTime) * 0.5f) + (doorMoveDuration * 2f);

    [Header("Visuals")]
    public SpriteRenderer facilityIcon;
    public Color idleIconColor = new Color(0.2f, 0.2f, 0.2f); 
    public Color activeFacilityColor = Color.magenta; 

    [Header("Door Hydraulics")]
    public Transform doorTransform;
    public Transform doorIdlePosition;
    public Transform doorClosedPosition;
    public float doorMoveDuration = 0.8f; 
    
    [Header("Timing")]
    public float minUsageTime = 8.0f;
    public float maxUsageTime = 16.0f;

    protected override void Start()
    {
        base.Start();

        if (doorTransform != null && doorIdlePosition != null)
        {
            doorTransform.localPosition = doorIdlePosition.localPosition;
        }

        if (facilityIcon != null)
        {
            facilityIcon.color = idleIconColor;
        }
    }

    public override void ProcessInteraction(Person person)
    {
        if (state == MachineState.Idle)
        {
            base.ProcessInteraction(person); 
            
            CancelInvoke("FinishProcessing"); 
            StartCoroutine(LavatoryRoutine((Passenger)person));
        }
    }

    private IEnumerator LavatoryRoutine(Passenger passenger)
    {
        if (facilityIcon != null)
        {
            facilityIcon.color = activeFacilityColor;
        }

        if (doorTransform != null && doorIdlePosition != null && doorClosedPosition != null)
        {
            yield return StartCoroutine(MoveDoor(doorIdlePosition.localPosition, doorClosedPosition.localPosition));
        }

        float waitTime = Random.Range(minUsageTime, maxUsageTime);
        yield return new WaitForSeconds(waitTime);

        if (doorTransform != null && doorIdlePosition != null && doorClosedPosition != null)
        {
            yield return StartCoroutine(MoveDoor(doorClosedPosition.localPosition, doorIdlePosition.localPosition));
        }

        Invoke("FinishProcessing", 0f);
    }

    private IEnumerator MoveDoor(Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;
        while (elapsed < doorMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / doorMoveDuration;
            
            float smoothT = t * t * (3f - 2f * t); 
            
            doorTransform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);
            yield return null;
        }
        doorTransform.localPosition = endPos;
    }

    protected override void DeliverService(Passenger passenger)
    {
        if (passenger == null) return;

        PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Hygiene, passenger);
        EconomyManager.Instance.AddMoney(usePrice);

        if (WorldSpacePromptCoordinator.Instance != null)
        {
            WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                "+$" + usePrice, transform.position + Vector3.up * 7f, Color.green); 
        }

        if (facilityIcon != null)
        {
            facilityIcon.color = idleIconColor;
        }
    }
}
