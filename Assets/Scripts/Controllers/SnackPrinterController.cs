using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnackPrinterController : StationFacility
{
    private const float NutrientDispenserDefaultDuration = 3.75f;

    public int usePrice = 8;
    public override float EstimatedServiceDuration => printDuration + consumeDuration;

    [Header("Setup & Waypoints")]
    public List<GameObject> snackPrefabs = new List<GameObject>();
    public Transform startPos;
    public Transform endPos;
    public Light printerLight;
    public float passengerHeadYOffset = 3.172f;

    [Header("Visuals")]
    public float idleLightIntensity = 0f;
    public float activeLightIntensity = 4f;
    public Color idleLightColor = Color.white;
    public Color activeLightColor = new Color(1f, 0.64f, 0f);

    [Header("Timing")]
    public float printDuration = NutrientDispenserDefaultDuration * 2f;
    public float consumeDuration = 0.75f;

    private GameObject activeSnackInstance;
    private Vector3 activeSnackFullScale;

    protected override void Start()
    {
        base.Start();
        SetLightState(false);
    }

    protected override void OnDestroy()
    {
        CleanupSnackInstance();
        base.OnDestroy();
    }

    public override void ProcessInteraction(Person person)
    {
        if (state != MachineState.Idle)
        {
            return;
        }

        if (startPos == null || endPos == null)
        {
            Debug.LogError("SnackPrinterController is missing StartPos or EndPos.", this);
            return;
        }

        if (snackPrefabs == null || snackPrefabs.Count == 0)
        {
            Debug.LogError("SnackPrinterController has no snack prefabs assigned.", this);
            return;
        }

        base.ProcessInteraction(person);
        CancelInvoke("FinishProcessing");
        StartCoroutine(PrintRoutine((Passenger)person));
    }

    private IEnumerator PrintRoutine(Passenger passenger)
    {
        SetLightState(true);
        SpawnSnackAtStart();

        if (activeSnackInstance == null)
        {
            AbortPrint();
            yield break;
        }

        float elapsed = 0f;
        Quaternion startRotation = startPos.rotation;
        Quaternion endRotation = endPos.rotation;

        while (elapsed < printDuration)
        {
            if (activeSnackInstance == null)
            {
                AbortPrint();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / printDuration);
            float smoothT = t * t * (3f - 2f * t);

            activeSnackInstance.transform.position = Vector3.Lerp(startPos.position, endPos.position, smoothT);
            activeSnackInstance.transform.rotation = Quaternion.Slerp(startRotation, endRotation, smoothT);
            activeSnackInstance.transform.localScale = Vector3.Lerp(Vector3.zero, activeSnackFullScale, smoothT);

            yield return null;
        }

        activeSnackInstance.transform.position = endPos.position;
        activeSnackInstance.transform.rotation = endPos.rotation;
        activeSnackInstance.transform.localScale = activeSnackFullScale;

        if (passenger == null)
        {
            AbortPrint();
            yield break;
        }

        elapsed = 0f;
        Vector3 consumeStartPosition = activeSnackInstance.transform.position;
        Vector3 consumeStartScale = activeSnackInstance.transform.localScale;

        while (elapsed < consumeDuration)
        {
            if (passenger == null || activeSnackInstance == null)
            {
                AbortPrint();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / consumeDuration);
            float smoothT = t * t * (3f - 2f * t);
            Vector3 headPosition = passenger.transform.position + (Vector3.up * passengerHeadYOffset);

            activeSnackInstance.transform.position = Vector3.Lerp(consumeStartPosition, headPosition, smoothT);
            activeSnackInstance.transform.localScale = Vector3.Lerp(consumeStartScale, Vector3.zero, smoothT);

            yield return null;
        }

        CleanupSnackInstance();

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
                    "+$" + usePrice,
                    transform.position + Vector3.up * 7f,
                    Color.green);
            }
        }

        SetLightState(false);
    }

    private void SpawnSnackAtStart()
    {
        CleanupSnackInstance();

        GameObject snackPrefab = GetRandomSnackPrefab();
        if (snackPrefab == null)
        {
            return;
        }

        activeSnackInstance = Instantiate(snackPrefab, startPos.position, startPos.rotation);
        activeSnackInstance.transform.SetParent(transform, true);
        activeSnackFullScale = activeSnackInstance.transform.localScale;
        activeSnackInstance.transform.localScale = Vector3.zero;
    }

    private GameObject GetRandomSnackPrefab()
    {
        if (snackPrefabs == null || snackPrefabs.Count == 0)
        {
            Debug.LogError("SnackPrinterController has no snack prefabs assigned.", this);
            return null;
        }

        GameObject snackPrefab = snackPrefabs[Random.Range(0, snackPrefabs.Count)];
        if (snackPrefab == null)
        {
            Debug.LogError("SnackPrinterController selected a null snack prefab.", this);
            return null;
        }

        return snackPrefab;
    }

    private void SetLightState(bool isPrinting)
    {
        if (printerLight == null)
        {
            return;
        }

        printerLight.color = isPrinting ? activeLightColor : idleLightColor;
        printerLight.intensity = isPrinting ? activeLightIntensity : idleLightIntensity;
    }

    private void AbortPrint()
    {
        SetLightState(false);
        CleanupSnackInstance();
        currentPerson = null;
        state = MachineState.Idle;
    }

    private void CleanupSnackInstance()
    {
        if (activeSnackInstance == null)
        {
            return;
        }

        Destroy(activeSnackInstance);
        activeSnackInstance = null;
        activeSnackFullScale = Vector3.one;
    }
}
