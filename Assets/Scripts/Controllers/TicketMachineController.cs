using UnityEngine;
using System.Collections;

public class TicketMachineController : StationFacility
{
    [Header("Visuals")]
    public MeshRenderer[] accentRenderers;
    public MeshRenderer screenRenderer;
    public SpriteRenderer ticketSprite;

    [Header("Colors")]
    public Color darkGrey = new Color(0.2f, 0.2f, 0.2f);
    public Color lightGrey = new Color(0.7f, 0.7f, 0.7f);
    public Color screenIdleColor = Color.black;

    [Header("Timing")]
    public float processingTime = 1.5f;
    public float holdDuration = 1.0f;
    public float pulseSpeed = 4f;
    public float transitionDuration = 0.2f;

    private bool isHoldingResult = false;
    public override float EstimatedServiceDuration => processingTime + holdDuration + (transitionDuration * 2f);

    protected override void Start()
    {
        base.Start();
        
        if (screenRenderer != null) 
            screenRenderer.material.color = screenIdleColor;
    }

    void Update()
    {
        if (!isHoldingResult)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            Color currentPulse = Color.Lerp(darkGrey, lightGrey, t);
            SetAccentColors(currentPulse);
            
            if (ticketSprite != null) 
                ticketSprite.color = currentPulse;
        }
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
        isHoldingResult = true;

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

            float pulseT = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            Color currentPulse = Color.Lerp(darkGrey, lightGrey, pulseT);

            SetAccentColors(Color.Lerp(passengerColor, currentPulse, t));
            
            if (ticketSprite != null) 
                ticketSprite.color = Color.Lerp(passengerColor, currentPulse, t);
                
            if (screenRenderer != null) 
                screenRenderer.material.color = Color.Lerp(passengerColor, screenIdleColor, t);

            yield return null;
        }

        if (screenRenderer != null) 
            screenRenderer.material.color = screenIdleColor;

        isHoldingResult = false;
        state = MachineState.Idle;
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
