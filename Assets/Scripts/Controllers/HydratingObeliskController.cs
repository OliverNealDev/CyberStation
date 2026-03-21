using UnityEngine;
using System.Collections;

public class HydratingObeliskController : StationFacility
{
    public int usePrice = 1;

    [Header("Waypoints & Targeting")]
    public Transform nozzlePoint;          
    public float passengerHeadYOffset = 3.172f; 
    public float arcHeight = 1.5f;

    [Header("Setup")]
    public Transform tubeTransform;
    public GameObject dropletPrefab;
    public Light facilityLight;
    public SpriteRenderer facilityIcon;

    [Header("Visuals")]
    public float idleLightIntensity = 1f;
    public float activeLightIntensity = 3f;
    public Color idleIconColor = Color.lightGray;

    [Header("Firing Settings")]
    public int dropletCount = 3;
    public float timeBetweenShots = 0.35f; // Slowed down from 0.15f
    public float flightDuration = 0.6f;    // Slowed down from 0.4f

    [Header("Squash & Stretch")]
    public float animDuration = 0.18f;     // Slowed down slightly to match
    public Vector3 squashScaleMultiplier = new Vector3(1.3f, 0.7f, 1.3f);
    public Vector3 stretchScaleMultiplier = new Vector3(0.7f, 1.3f, 0.7f);

    private Vector3 initialTubeScale;
    private Coroutine currentSquashCoroutine;

    protected override void Start()
    {
        base.Start();

        if (tubeTransform != null)
        {
            initialTubeScale = tubeTransform.localScale;
        }

        if (facilityLight != null)
        {
            facilityLight.color = Color.white;
            facilityLight.intensity = idleLightIntensity;
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
            StartCoroutine(HydrationRoutine((Passenger)person));
        }
    }

    private IEnumerator HydrationRoutine(Passenger passenger)
    {
        Color passengerColor = Color.white;

        if (passenger.assignedTrainService != null && passenger.assignedTrainService.trainData != null)
        {
            passengerColor = passenger.assignedTrainService.trainData.trainColor;
        }

        if (facilityLight != null)
        {
            facilityLight.color = passengerColor;
            facilityLight.intensity = activeLightIntensity;
        }

        if (facilityIcon != null)
        {
            facilityIcon.color = passengerColor;
        }

        for (int i = 0; i < dropletCount; i++)
        {
            if (passenger == null) break;

            if (tubeTransform != null)
            {
                if (currentSquashCoroutine != null) StopCoroutine(currentSquashCoroutine);
                currentSquashCoroutine = StartCoroutine(SquashStretchRoutine());
            }

            if (dropletPrefab != null && nozzlePoint != null)
            {
                GameObject droplet = Instantiate(dropletPrefab, nozzlePoint.position, Quaternion.identity);
                MeshRenderer rend = droplet.GetComponent<MeshRenderer>();
                if (rend != null) rend.material.color = passengerColor;

                StartCoroutine(DropletFlightRoutine(droplet, passenger));
            }

            yield return new WaitForSeconds(timeBetweenShots);
        }

        yield return new WaitForSeconds(flightDuration);

        Invoke("FinishProcessing", 0f);
    }

    private IEnumerator DropletFlightRoutine(GameObject droplet, Passenger passenger)
    {
        float elapsed = 0f;
        Vector3 startPos = droplet.transform.position;

        while (elapsed < flightDuration)
        {
            if (passenger == null || droplet == null) break;

            Vector3 headPos = passenger.transform.position + (Vector3.up * passengerHeadYOffset);

            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration;
            float smoothT = t * t * (3f - 2f * t);

            Vector3 linearPos = Vector3.Lerp(startPos, headPos, smoothT);
            float heightOffset = Mathf.Sin(smoothT * Mathf.PI) * arcHeight;

            droplet.transform.position = linearPos + (Vector3.up * heightOffset);

            yield return null;
        }

        if (droplet != null)
        {
            Destroy(droplet);
        }
    }

    private IEnumerator SquashStretchRoutine()
    {
        float elapsed = 0f;
        float phaseDuration = animDuration / 3f;

        Vector3 squashScale = new Vector3(initialTubeScale.x * squashScaleMultiplier.x, initialTubeScale.y * squashScaleMultiplier.y, initialTubeScale.z * squashScaleMultiplier.z);
        Vector3 stretchScale = new Vector3(initialTubeScale.x * stretchScaleMultiplier.x, initialTubeScale.y * stretchScaleMultiplier.y, initialTubeScale.z * stretchScaleMultiplier.z);

        tubeTransform.localScale = initialTubeScale;

        while (elapsed < phaseDuration)
        {
            elapsed += Time.deltaTime;
            tubeTransform.localScale = Vector3.Lerp(initialTubeScale, squashScale, elapsed / phaseDuration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < phaseDuration)
        {
            elapsed += Time.deltaTime;
            tubeTransform.localScale = Vector3.Lerp(squashScale, stretchScale, elapsed / phaseDuration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < phaseDuration)
        {
            elapsed += Time.deltaTime;
            tubeTransform.localScale = Vector3.Lerp(stretchScale, initialTubeScale, elapsed / phaseDuration);
            yield return null;
        }

        tubeTransform.localScale = initialTubeScale;
    }

    protected override void DeliverService(Passenger passenger)
    {
        if (passenger == null) return;

        PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Thirst, passenger);
        EconomyManager.Instance.AddMoney(usePrice);

        if (WorldSpacePromptCoordinator.Instance != null)
        {
            WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                "+$" + usePrice, transform.position + Vector3.up * 7f, Color.green);
        }

        if (facilityLight != null)
        {
            facilityLight.color = Color.white;
            facilityLight.intensity = idleLightIntensity;
        }

        if (facilityIcon != null)
        {
            facilityIcon.color = idleIconColor;
        }
    }
}