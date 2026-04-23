using UnityEngine;
using System.Collections;

public class PrivateLavatoryController : StationFacility, IPreviewInitializable, IRenderPreviewInitializable
{
    public int usePrice = 4; 
    private float BaseServiceDuration => usageTime + (doorMoveDuration * 2f);
    public override float EstimatedServiceDuration => ScaleServiceDuration(BaseServiceDuration);

    [Header("Visuals")]
    public SpriteRenderer facilityIcon;
    public Color activeFacilityColor = Color.magenta; 

    [Header("Door Hydraulics")]
    public Transform doorTransform;
    public Transform doorIdlePosition;
    public Transform doorClosedPosition;
    public float doorMoveDuration = 0.75f; 
    
    [Header("Timing")]
    public float usageTime = 1.5f;

    protected override void Start()
    {
        facilityType = FacilityType.PrivateLavatory;
        base.Start();
        InitializePreviewVisuals();
    }

    public void InitializePreviewVisuals()
    {
        facilityIcon = ResolveNeedIcon(facilityIcon);

        if (doorTransform != null && doorIdlePosition != null)
        {
            doorTransform.localPosition = doorIdlePosition.localPosition;
        }

        SetNeedIconIdle(facilityIcon);
    }

    public void InitializeRenderPreviewVisuals()
    {
        if (doorTransform != null && doorClosedPosition != null)
        {
            doorTransform.localPosition = doorClosedPosition.localPosition;
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
        SetNeedIconActive(facilityIcon, Passenger.NeedType.Hygiene);

        if (doorTransform != null && doorIdlePosition != null && doorClosedPosition != null)
        {
            yield return StartCoroutine(MoveDoor(doorIdlePosition.localPosition, doorClosedPosition.localPosition));
        }

        yield return new WaitForSeconds(usageTime);

        if (doorTransform != null && doorIdlePosition != null && doorClosedPosition != null)
        {
            yield return StartCoroutine(MoveDoor(doorClosedPosition.localPosition, doorIdlePosition.localPosition));
        }

        ScheduleFinishAfterAdditionalDelay(BaseServiceDuration);
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
        if (passenger != null)
        {
            PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Hygiene, passenger);
            EconomyManager.Instance.AddMoney(usePrice);

            if (WorldSpacePromptCoordinator.Instance != null)
            {
                WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                    "+$" + usePrice, transform.position + Vector3.up * 7f, Color.green); 
            }
        }

        SetNeedIconIdle(facilityIcon);
    }
}
