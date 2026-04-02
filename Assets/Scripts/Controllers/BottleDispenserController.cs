using System.Collections;
using UnityEngine;

public class BottleDispenserController : StationFacility, IPreviewInitializable
{
    public int usePrice = 8;
    public override float EstimatedServiceDuration => waitDelay + endPositionDelay + dispenseDuration + magnetizeDuration;

    [Header("Setup & Waypoints")]
    public GameObject bottlePrefab;
    public Transform startPos;
    public Transform endPos;
    public Light machineLight;
    public SpriteRenderer facilityIcon;
    public float passengerHeadYOffset = 3.172f;

    [Header("Timing")]
    public float waitDelay = 4.5f;
    public float dispenseDuration = 0.5f;
    public float endPositionDelay = 2.5f;
    public float magnetizeDuration = 0.5f;

    [Header("Light")]
    public float activeLightIntensity = 4f;
    public Color activeLightColor = new Color(0.25f, 0.6f, 1f);

    private GameObject activeBottleInstance;
    private Vector3 activeBottleFullScale = Vector3.one;

    protected override void Start()
    {
        facilityType = FacilityType.BottleDispenser;
        base.Start();

        InitializePreviewVisuals();
    }

    public void InitializePreviewVisuals()
    {
        facilityIcon = ResolveNeedIcon(facilityIcon);
        SetNeedIconIdle(facilityIcon);
        SetLightState(false);
    }

    protected override void OnDestroy()
    {
        CleanupBottleInstance();
        base.OnDestroy();
    }

    public override void ProcessInteraction(Person person)
    {
        if (state != MachineState.Idle)
        {
            return;
        }

        Passenger passenger = person as Passenger;
        if (passenger == null)
        {
            return;
        }

        if (bottlePrefab == null)
        {
            Debug.LogError("BottleDispenserController has no bottle prefab assigned.", this);
            return;
        }

        if (startPos == null || endPos == null)
        {
            Debug.LogError("BottleDispenserController is missing StartPos or EndPos.", this);
            return;
        }

        base.ProcessInteraction(passenger);
        CancelInvoke("FinishProcessing");
        StartCoroutine(DispenseRoutine(passenger));
    }

    private IEnumerator DispenseRoutine(Passenger passenger)
    {
        SetNeedIconActive(facilityIcon, Passenger.NeedType.Thirst);
        SetLightState(true);
        SpawnBottleAtStart();

        if (activeBottleInstance == null)
        {
            AbortDispense();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < waitDelay)
        {
            if (passenger == null || activeBottleInstance == null)
            {
                AbortDispense();
                yield break;
            }

            activeBottleInstance.transform.SetPositionAndRotation(startPos.position, startPos.rotation);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        Quaternion startRotation = startPos.rotation;
        Quaternion endRotation = endPos.rotation;

        while (elapsed < dispenseDuration)
        {
            if (passenger == null || activeBottleInstance == null)
            {
                AbortDispense();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dispenseDuration);
            float smoothT = t * t * (3f - 2f * t);

            activeBottleInstance.transform.position = Vector3.Lerp(startPos.position, endPos.position, smoothT);
            activeBottleInstance.transform.rotation = Quaternion.Slerp(startRotation, endRotation, smoothT);

            yield return null;
        }

        activeBottleInstance.transform.SetPositionAndRotation(endPos.position, endPos.rotation);
        SetLightState(false);

        elapsed = 0f;
        while (elapsed < endPositionDelay)
        {
            if (passenger == null || activeBottleInstance == null)
            {
                AbortDispense();
                yield break;
            }

            activeBottleInstance.transform.SetPositionAndRotation(endPos.position, endPos.rotation);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        Vector3 magnetizeStartPosition = activeBottleInstance.transform.position;
        Vector3 magnetizeStartScale = activeBottleInstance.transform.localScale;

        while (elapsed < magnetizeDuration)
        {
            if (passenger == null || activeBottleInstance == null)
            {
                AbortDispense();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / magnetizeDuration);
            float smoothT = t * t * (3f - 2f * t);
            Vector3 headPosition = passenger.transform.position + (Vector3.up * passengerHeadYOffset);

            activeBottleInstance.transform.position = Vector3.Lerp(magnetizeStartPosition, headPosition, smoothT);
            activeBottleInstance.transform.localScale = Vector3.Lerp(magnetizeStartScale, Vector3.zero, smoothT);

            yield return null;
        }

        CleanupBottleInstance();
        Invoke("FinishProcessing", 0f);
    }

    protected override void DeliverService(Passenger passenger)
    {
        if (passenger != null)
        {
            PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Thirst, passenger);
            EconomyManager.Instance.AddMoney(usePrice);

            if (WorldSpacePromptCoordinator.Instance != null)
            {
                WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                    "+$" + usePrice,
                    transform.position + Vector3.up * 7f,
                    Color.green);
            }
        }

        SetLightState(false);
        SetNeedIconIdle(facilityIcon);
    }

    private void SpawnBottleAtStart()
    {
        CleanupBottleInstance();

        activeBottleInstance = Instantiate(bottlePrefab, startPos.position, startPos.rotation);
        activeBottleInstance.transform.SetParent(transform, true);
        activeBottleFullScale = activeBottleInstance.transform.localScale;
        activeBottleInstance.transform.localScale = activeBottleFullScale;
    }

    private void SetLightState(bool isActive)
    {
        if (machineLight == null)
        {
            return;
        }

        machineLight.color = activeLightColor;
        machineLight.intensity = isActive ? activeLightIntensity : 0f;
        machineLight.enabled = isActive;
    }

    private void AbortDispense()
    {
        SetLightState(false);
        SetNeedIconIdle(facilityIcon);
        CleanupBottleInstance();
        currentPerson = null;
        state = MachineState.Idle;
    }

    private void CleanupBottleInstance()
    {
        if (activeBottleInstance == null)
        {
            return;
        }

        Destroy(activeBottleInstance);
        activeBottleInstance = null;
        activeBottleFullScale = Vector3.one;
    }
}
