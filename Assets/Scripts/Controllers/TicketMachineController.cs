using UnityEngine;
using System.Collections;

public class TicketMachineController : StationFacility, IPreviewInitializable
{
    [Header("Visuals")]
    public MeshRenderer[] accentRenderers;
    public MeshRenderer screenRenderer;
    public SpriteRenderer ticketSprite;

    [Header("Colors")]
    public Color screenIdleColor = Color.black;

    [Header("Timing")]
    public float processingTime = 4.0f;
    public float holdDuration = 2.0f;
    public float transitionDuration = 0.1f;

    public override float EstimatedServiceDuration => processingTime + holdDuration + (transitionDuration * 2f);

    protected override void Start()
    {
        facilityType = FacilityType.TicketMachine;
        base.Start();

        InitializePreviewVisuals();
    }

    public void InitializePreviewVisuals()
    {
        SetIdleVisuals();
    }

    public override void ProcessInteraction(Person person)
    {
        if (state == MachineState.Idle)
        {
            base.ProcessInteraction(person); 
            CancelInvoke("FinishProcessing"); 
            StartCoroutine(TicketRoutine((Passenger)person));
        }
    }

    private IEnumerator TicketRoutine(Passenger passenger)
    {
        if (screenRenderer != null) 
            screenRenderer.material.color = Color.white;

        SetAccentColors(Color.white);
        
        if (ticketSprite != null) 
            ticketSprite.color = Color.white;

        yield return new WaitForSeconds(processingTime);

        Color passengerColor = Color.white;
        if (passenger.assignedTrainService != null && passenger.assignedTrainService.trainData != null)
        {
            passengerColor = passenger.assignedTrainService.trainData.trainColor;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            Color lerpedColor = Color.Lerp(Color.white, passengerColor, t);

            SetAccentColors(lerpedColor);
            
            if (screenRenderer != null) 
                screenRenderer.material.color = lerpedColor;
                
            if (ticketSprite != null) 
                ticketSprite.color = lerpedColor;

            yield return null;
        }

        SetAccentColors(passengerColor);
        
        if (screenRenderer != null) 
            screenRenderer.material.color = passengerColor;
            
        if (ticketSprite != null) 
            ticketSprite.color = passengerColor;

        DeliverService(passenger);

        passenger = null;
        currentPerson = null; 

        yield return new WaitForSeconds(holdDuration);

        elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            Color idleAccentColor = Color.white;

            SetAccentColors(Color.Lerp(passengerColor, idleAccentColor, t));
            
            if (ticketSprite != null) 
                ticketSprite.color = Color.Lerp(passengerColor, idleAccentColor, t);
                
            if (screenRenderer != null) 
                screenRenderer.material.color = Color.Lerp(passengerColor, screenIdleColor, t);

            yield return null;
        }

        SetIdleVisuals();

        state = MachineState.Idle;
    }

    private void SetIdleVisuals()
    {
        if (screenRenderer != null)
            screenRenderer.material.color = screenIdleColor;

        SetAccentColors(Color.white);

        if (ticketSprite != null)
            ticketSprite.color = Color.white;
    }

    private void SetAccentColors(Color color)
    {
        if (accentRenderers == null) return;
        
        foreach (MeshRenderer rend in accentRenderers)
        {
            if (rend != null)
            {
                rend.material.color = color;
                rend.material.SetColor("_EmissionColor", color); 
            }
        }
    }

    protected override void DeliverService(Passenger passenger)
    {
        if (passenger == null) return;

        PassengerManager.Instance.ReceiveTicket(passenger);
        int price = 0;

        if (passenger.assignedTrainService != null && passenger.assignedTrainService.trainData != null)
        {
            price = passenger.assignedTrainService.trainData.costPerRide;
            
            if (WorldSpacePromptCoordinator.Instance != null && price > 0)
            {
                WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                    "+$" + price, transform.position + Vector3.up * 7f, Color.darkGreen);
            }
        }
    }
}
