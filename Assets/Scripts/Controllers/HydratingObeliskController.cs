using UnityEngine;
using System.Collections;

public class HydratingObeliskController : StationFacility
{
    public int usePrice = 1;

    [Header("Visuals")]
    public SpriteRenderer facilityIcon;
    public Color idleIconColor = Color.lightGray;
    public Color activeFacilityColor = Color.cyan;

    [Header("Setup & Waypoints")]
    public Transform tubeTransform;
    public GameObject dropletPrefab;
    public Transform nozzlePoint;          
    public float passengerHeadYOffset = 3.172f; 
    public float arcHeight = 1.5f;

    [Header("Timing & Firing")]
    public int dropletCount = 3;
    public float timeBetweenShots = 0.35f; 
    public float flightDuration = 0.6f;    
    public float animDuration = 0.18f;     
    
    [Header("Squash & Stretch")]
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
        if (facilityIcon != null)
        {
            facilityIcon.color = activeFacilityColor;
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
                if (rend != null) rend.material.color = activeFacilityColor;

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

        if (facilityIcon != null)
        {
            facilityIcon.color = idleIconColor;
        }
    }
}