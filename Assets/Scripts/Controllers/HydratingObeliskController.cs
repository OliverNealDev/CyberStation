using UnityEngine;
using System.Collections;

public class HydratingObeliskController : StationFacility, IPreviewInitializable
{
    public int usePrice = 2;
    public override float EstimatedServiceDuration => (dropletCount * timeBetweenShots) + flightDuration;

    [Header("Visuals")]
    public SpriteRenderer facilityIcon;
    public Color activeFacilityColor = Color.cyan;

    [Header("Setup & Waypoints")]
    public GameObject dropletPrefab;
    public Transform nozzlePoint;          
    public float passengerHeadYOffset = 3.172f; 
    public float arcHeight = 0.4f;

    [Header("Timing & Firing")]
    public int dropletCount = 28;
    public float timeBetweenShots = 0.12f; 
    public float flightDuration = 0.65f;

    [Header("Litter")]
    public GameObject puddleLitterPrefab;
    [Range(0f, 1f)] public float puddleLitterChance = 0.05f;
    [Min(0f)] public float puddleSpawnDistance = 1f;

    protected override void Start()
    {
        facilityType = FacilityType.HydratingObelisk;
        base.Start();

        InitializePreviewVisuals();
    }

    public void InitializePreviewVisuals()
    {
        facilityIcon = ResolveNeedIcon(facilityIcon);
        SetNeedIconIdle(facilityIcon);
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
        SetNeedIconActive(facilityIcon, Passenger.NeedType.Thirst);

        for (int i = 0; i < dropletCount; i++)
        {
            if (passenger == null) break;

            if (dropletPrefab != null && nozzlePoint != null)
            {
                GameObject droplet = Instantiate(dropletPrefab, nozzlePoint.position, Quaternion.identity);
                droplet.transform.SetParent(transform, true);
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

    protected override void DeliverService(Passenger passenger)
    {
        if (passenger != null)
        {
            PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Thirst, passenger);
            EconomyManager.Instance.AddMoney(usePrice);

            if (WorldSpacePromptCoordinator.Instance != null)
            {
                WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                    "+$" + usePrice, transform.position + Vector3.up * 7f, Color.green);
            }
        }

        SetNeedIconIdle(facilityIcon);
    }

    protected override void HandleCompletedServiceLitter(Passenger passenger)
    {
        if (PassengerManager.Instance == null || puddleLitterPrefab == null || Random.value > puddleLitterChance)
        {
            return;
        }

        Vector3 puddleOrigin = transform.position + (transform.forward * puddleSpawnDistance);
        PassengerManager.Instance.TrySpawnPlacedLitter(puddleLitterPrefab, puddleOrigin);
    }

}
