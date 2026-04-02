using UnityEngine;
using System.Collections;

public class NutrientDispenserController : StationFacility, IPreviewInitializable
{
    public int usePrice = 3;
    public override float EstimatedServiceDuration => extrudeDuration + arcDuration;
    
    [Header("Visuals")]
    public Transform nutrientCube;
    public SpriteRenderer facilityIcon;
    public Color activeFacilityColor = new Color(1f, 0.64f, 0f); 

    [Header("Setup & Waypoints")]
    public Transform cubeRestPoint;      
    public Transform cubeExtrudePoint;   
    public float passengerHeadYOffset = 3.172f; 
    public float arcHeight = 1.25f;

    [Header("Timing")]
    public float extrudeDuration = 7.5f; 
    public float arcDuration = 0.75f;     
    
    private MeshRenderer cubeRenderer;

    protected override void Start()
    {
        facilityType = FacilityType.NutrientExtruder;
        base.Start();
        InitializePreviewVisuals();
    }

    public void InitializePreviewVisuals()
    {
        facilityIcon = ResolveNeedIcon(facilityIcon);

        if (nutrientCube != null)
        {
            cubeRenderer = nutrientCube.GetComponent<MeshRenderer>();

            if (cubeRenderer != null)
            {
                cubeRenderer.enabled = false;
            }

            if (cubeRestPoint != null)
            {
                nutrientCube.position = cubeRestPoint.position;
            }
        }

        SetNeedIconIdle(facilityIcon);
    }

    public override void ProcessInteraction(Person person)
    {
        if (state == MachineState.Idle)
        {
            base.ProcessInteraction(person); 
            CancelInvoke("FinishProcessing");
            StartCoroutine(ExtrudeRoutine((Passenger)person));
        }
    }

    private IEnumerator ExtrudeRoutine(Passenger passenger)
    {
        if (cubeRenderer != null)
        {
            cubeRenderer.material.color = activeFacilityColor;
            cubeRenderer.enabled = true;
        }

        SetNeedIconActive(facilityIcon, Passenger.NeedType.Hunger);

        if (nutrientCube != null && cubeRestPoint != null && cubeExtrudePoint != null)
        {
            nutrientCube.position = cubeRestPoint.position;

            float elapsed = 0f;

            while (elapsed < extrudeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / extrudeDuration;
                float smoothT = t * t * (3f - 2f * t); 
                
                nutrientCube.position = Vector3.Lerp(cubeRestPoint.position, cubeExtrudePoint.position, smoothT);
                yield return null;
            }
            
            nutrientCube.position = cubeExtrudePoint.position;

            elapsed = 0f;
            Vector3 startWorldPos = nutrientCube.position;

            while (elapsed < arcDuration)
            {
                if (passenger == null) break; 
                
                Vector3 headPosition = passenger.transform.position + (Vector3.up * passengerHeadYOffset);

                elapsed += Time.deltaTime;
                float t = elapsed / arcDuration;
                float smoothT = t * t * (3f - 2f * t);
                
                Vector3 linearPos = Vector3.Lerp(startWorldPos, headPosition, smoothT);
                float heightOffset = Mathf.Sin(smoothT * Mathf.PI) * arcHeight;
                
                nutrientCube.position = linearPos + (Vector3.up * heightOffset);
                yield return null;
            }
            
            if (cubeRenderer != null)
            {
                cubeRenderer.enabled = false;
            }
        }
        
        Invoke("FinishProcessing", 0f);
    }

    protected override void DeliverService(Passenger passenger)
    {
        if (passenger != null)
        {
            PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Hunger, passenger);
            EconomyManager.Instance.AddMoney(usePrice);
            
            if (WorldSpacePromptCoordinator.Instance != null)
            {
                WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                    "+$" + usePrice, transform.position + Vector3.up * 7f, Color.green);
            }
        }

        if (nutrientCube != null)
        {
            if (cubeRenderer != null) cubeRenderer.enabled = false;
            if (cubeRestPoint != null) nutrientCube.position = cubeRestPoint.position; 
        }

        SetNeedIconIdle(facilityIcon);
    }
}
