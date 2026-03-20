using UnityEngine;
using System.Collections;

public class NutrientExtruderController : StationFacility
{
    public int usePrice = 1;
    
    [Header("Timing")]
    public float extrudeDuration = 3.0f;
    public float arcDuration = 0.75f;
    
    [Header("Visuals")]
    public Transform nutrientCube;
    public Light extruderLight;
    public float idleLightIntensity = 1f;
    public float activeLightIntensity = 3f;
    
    private Vector3 initialCubeLocalPos;
    private MeshRenderer cubeRenderer;

    protected override void Start()
    {
        base.Start();
        
        if (nutrientCube != null)
        {
            initialCubeLocalPos = nutrientCube.localPosition; 
            cubeRenderer = nutrientCube.GetComponent<MeshRenderer>();
            
            if (cubeRenderer != null)
            {
                cubeRenderer.enabled = false;
            }
        }
        
        if (extruderLight != null)
        {
            extruderLight.color = Color.white;
            extruderLight.intensity = idleLightIntensity;
        }
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
        Color passengerColor = Color.white;
        
        if (passenger.assignedTrainService != null && passenger.assignedTrainService.trainData != null)
        {
            passengerColor = passenger.assignedTrainService.trainData.trainColor;
        }

        if (cubeRenderer != null)
        {
            cubeRenderer.material.color = passengerColor;
            cubeRenderer.enabled = true;
        }

        if (nutrientCube != null)
        {
            Vector3 startLocalPos = new Vector3(initialCubeLocalPos.x, 2.086f, initialCubeLocalPos.z);
            Vector3 extrudedLocalPos = new Vector3(initialCubeLocalPos.x, 2.338f, initialCubeLocalPos.z);
            
            nutrientCube.localPosition = startLocalPos;

            float elapsed = 0f;
            float lightTransitionDuration = 0.25f;

            while (elapsed < extrudeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / extrudeDuration;
                float smoothT = t * t * (3f - 2f * t); 
                
                nutrientCube.localPosition = Vector3.Lerp(startLocalPos, extrudedLocalPos, smoothT);

                if (extruderLight != null)
                {
                    float lightT = Mathf.Clamp01(elapsed / lightTransitionDuration);
                    extruderLight.color = Color.Lerp(Color.white, passengerColor, lightT);
                    extruderLight.intensity = Mathf.Lerp(idleLightIntensity, activeLightIntensity, lightT);
                }

                yield return null;
            }
            
            nutrientCube.localPosition = extrudedLocalPos;

            elapsed = 0f;
            Vector3 startWorldPos = nutrientCube.position;
            float arcHeight = 2.5f; 

            while (elapsed < arcDuration)
            {
                if (passenger == null) break; 
                
                Vector3 headPosition = passenger.transform.position + (Vector3.up * 3.172f); 

                elapsed += Time.deltaTime;
                float t = elapsed / arcDuration;
                float smoothT = t * t * (3f - 2f * t);
                
                Vector3 linearPos = Vector3.Lerp(startWorldPos, headPosition, smoothT);
                float heightOffset = Mathf.Sin(smoothT * Mathf.PI) * arcHeight;
                
                nutrientCube.position = linearPos + (Vector3.up * heightOffset);
                
                if (extruderLight != null)
                {
                    extruderLight.color = Color.Lerp(passengerColor, Color.white, smoothT);
                    extruderLight.intensity = Mathf.Lerp(activeLightIntensity, idleLightIntensity, smoothT);
                }
                
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
        if (passenger == null) return; 

        PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Hunger, passenger);
        EconomyManager.Instance.AddMoney(usePrice);
        
        if (WorldSpacePromptCoordinator.Instance != null)
        {
            WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                "+$" + usePrice, transform.position + Vector3.up * 7f, Color.green);
        }

        if (nutrientCube != null)
        {
            if (cubeRenderer != null) cubeRenderer.enabled = false;
            nutrientCube.localPosition = new Vector3(initialCubeLocalPos.x, 2.086f, initialCubeLocalPos.z); 
        }
        
        if (extruderLight != null)
        {
            extruderLight.color = Color.white;
            extruderLight.intensity = idleLightIntensity;
        }
    }
}