using UnityEngine;
using System.Collections;

public class TicketMachineController : StationFacility, IPreviewInitializable
{
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    private const string EmissionKeyword = "_EMISSION";

    [Header("Visuals")]
    public MeshRenderer[] accentRenderers;
    public MeshRenderer screenRenderer;
    public SpriteRenderer ticketSprite;

    [Header("Colors")]
    public Color screenIdleColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    public Color screenPulseColor = Color.white;

    [Header("Timing")]
    public float processingTime = 4.0f;
    public float holdDuration = 2.0f;
    public float transitionDuration = 0.1f;
    public float pulseFrequency = 0.5f;

    private Material screenMaterialInstance;
    private Color currentScreenColor;
    private Color currentTicketColor = Color.white;

    private float BaseServiceDuration => processingTime + holdDuration + (transitionDuration * 2f);
    public override float EstimatedServiceDuration => ScaleServiceDuration(BaseServiceDuration);

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
        SetTicketColor(Color.white);

        float elapsed = 0f;
        float safePulseFrequency = Mathf.Max(0.01f, pulseFrequency);

        while (elapsed < processingTime)
        {
            elapsed += Time.deltaTime;

            float pulse = Mathf.Sin((elapsed * safePulseFrequency * Mathf.PI * 2f) - (Mathf.PI * 0.5f));
            float pulseT = (pulse + 1f) * 0.5f;

            SetScreenColor(Color.Lerp(screenIdleColor, screenPulseColor, pulseT));
            yield return null;
        }

        Color passengerColor = Color.white;
        if (passenger.assignedTrainService != null && passenger.assignedTrainService.trainData != null)
        {
            passengerColor = passenger.assignedTrainService.trainData.trainColor;
        }

        yield return TransitionDisplay(currentScreenColor, passengerColor, currentTicketColor, passengerColor);

        DeliverService(passenger);

        passenger = null;
        currentPerson = null; 

        yield return new WaitForSeconds(holdDuration);

        yield return TransitionDisplay(currentScreenColor, screenIdleColor, currentTicketColor, Color.white);

        yield return new WaitForSeconds(GetAdditionalServiceDelay(BaseServiceDuration));

        SetIdleVisuals();

        state = MachineState.Idle;
    }

    private void SetIdleVisuals()
    {
        SetScreenColor(screenIdleColor);
        SetTicketColor(Color.white);
    }

    private IEnumerator TransitionDisplay(Color fromScreenColor, Color toScreenColor, Color fromTicketColor, Color toTicketColor)
    {
        if (transitionDuration <= 0f)
        {
            SetScreenColor(toScreenColor);
            SetTicketColor(toTicketColor);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            SetScreenColor(Color.Lerp(fromScreenColor, toScreenColor, t));
            SetTicketColor(Color.Lerp(fromTicketColor, toTicketColor, t));

            yield return null;
        }
    }

    private void SetScreenColor(Color color)
    {
        currentScreenColor = color;

        Material screenMaterial = GetScreenMaterial();
        if (screenMaterial == null)
        {
            return;
        }

        screenMaterial.color = color;

        if (screenMaterial.HasProperty(EmissionColorProperty))
        {
            screenMaterial.EnableKeyword(EmissionKeyword);
            screenMaterial.SetColor(EmissionColorProperty, color * 0.35f);
        }
    }

    private void SetTicketColor(Color color)
    {
        currentTicketColor = color;

        if (ticketSprite != null)
        {
            ticketSprite.color = color;
        }
    }

    private Material GetScreenMaterial()
    {
        if (screenRenderer == null)
        {
            return null;
        }

        if (screenMaterialInstance == null)
        {
            screenMaterialInstance = screenRenderer.material;
        }

        return screenMaterialInstance;
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
