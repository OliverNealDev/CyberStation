using UnityEngine;
using System.Collections;

public class CleansingShowerController : StationFacility, IPreviewInitializable
{
    public int usePrice = 4;
    public override float EstimatedServiceDuration => showerDuration + dropletFallDuration + (doorMoveDuration * 2f);

    [Header("Waypoints & Targeting")]
    public Transform showerHeadPoint;
    public float passengerDropYOffset = 0f;
    public float showerSpread = 0.4f;

    [Header("Setup")]
    public GameObject dropletPrefab;
    public Light facilityLight;
    public SpriteRenderer facilityIcon;

    [Header("Visuals")]
    public float idleLightIntensity = 1f;
    public float activeLightIntensity = 3f;
    public Color activeFacilityColor = Color.magenta; // The machine's native color!

    [Header("Door Hydraulics")]
    public Transform doorTransform;
    public Transform doorIdlePosition;
    public Transform doorClosedPosition;
    public float doorMoveDuration = 1.25f;

    [Header("Shower Settings")]
    public float showerDuration = 9.1f;
    public float dropletSpawnRate = 0.12f;
    public float dropletFallDuration = 0.4f;

    [Header("Droplet Shape (Cuboid)")]
    public float dropletWidth = 0.05f;
    public float dropletLength = 0.4f;

    protected override void Start()
    {
        facilityType = FacilityType.CleansingShower;
        base.Start();

        InitializePreviewVisuals();
    }

    public void InitializePreviewVisuals()
    {
        facilityIcon = ResolveNeedIcon(facilityIcon);

        if (doorTransform != null && doorIdlePosition != null)
        {
            doorTransform.localPosition = doorIdlePosition.localPosition;
        }

        if (facilityLight != null)
        {
            facilityLight.color = Color.white;
            facilityLight.intensity = idleLightIntensity;
        }

        SetNeedIconIdle(facilityIcon);
    }

    public override void ProcessInteraction(Person person)
    {
        if (state == MachineState.Idle)
        {
            base.ProcessInteraction(person);
            CancelInvoke("FinishProcessing");
            StartCoroutine(ShowerRoutine((Passenger)person));
        }
    }

    private IEnumerator ShowerRoutine(Passenger passenger)
    {
        if (facilityLight != null)
        {
            facilityLight.color = activeFacilityColor;
            facilityLight.intensity = activeLightIntensity;
        }

        SetNeedIconActive(facilityIcon, Passenger.NeedType.Hygiene);

        if (HasDoorHydraulics())
        {
            yield return StartCoroutine(MoveDoor(doorIdlePosition.localPosition, doorClosedPosition.localPosition));
        }

        float elapsed = 0f;
        float spawnTimer = 0f;

        while (elapsed < showerDuration)
        {
            if (passenger == null) break;

            elapsed += Time.deltaTime;
            spawnTimer += Time.deltaTime;

            if (spawnTimer >= dropletSpawnRate)
            {
                spawnTimer = 0f;
                SpawnDroplet(activeFacilityColor, passenger);
            }

            yield return null;
        }

        yield return new WaitForSeconds(dropletFallDuration);

        if (HasDoorHydraulics())
        {
            yield return StartCoroutine(MoveDoor(doorClosedPosition.localPosition, doorIdlePosition.localPosition));
        }

        Invoke("FinishProcessing", 0f);
    }

    private void SpawnDroplet(Color color, Passenger passenger)
    {
        if (dropletPrefab == null || showerHeadPoint == null) return;

        Vector3 randomOffset = new Vector3(
            Random.Range(-showerSpread, showerSpread),
            0f,
            Random.Range(-showerSpread, showerSpread)
        );
        
        Vector3 spawnPos = GetShowerHeadOrigin(passenger, randomOffset);

        GameObject droplet = Instantiate(dropletPrefab, spawnPos, Quaternion.identity);
        droplet.transform.SetParent(transform, true);
        
        droplet.transform.localScale = new Vector3(dropletWidth, dropletLength, dropletWidth);

        MeshRenderer rend = droplet.GetComponent<MeshRenderer>();
        if (rend != null) rend.material.color = color;

        StartCoroutine(DropletFallRoutine(droplet, passenger, randomOffset));
    }

    private IEnumerator DropletFallRoutine(GameObject droplet, Passenger passenger, Vector3 offset)
    {
        float elapsed = 0f;
        Vector3 startPos = droplet.transform.position;

        while (elapsed < dropletFallDuration)
        {
            if (droplet == null || passenger == null) break;

            Vector3 targetPos = passenger.transform.position + offset + (Vector3.up * passengerDropYOffset);

            elapsed += Time.deltaTime;
            float t = elapsed / dropletFallDuration;
            
            droplet.transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        if (droplet != null)
        {
            Destroy(droplet);
        }
    }

    private bool HasDoorHydraulics()
    {
        return doorTransform != null && doorIdlePosition != null && doorClosedPosition != null;
    }

    private Vector3 GetShowerHeadOrigin(Passenger passenger, Vector3 randomOffset)
    {
        if (showerHeadPoint != null)
        {
            return showerHeadPoint.position + randomOffset;
        }

        float fallbackHeight = transform.position.y + 3.75f;
        Vector3 basePosition = transform.position + (Vector3.up * 3.75f);

        if (passenger != null)
        {
            basePosition = new Vector3(passenger.transform.position.x, fallbackHeight, passenger.transform.position.z);
        }

        return basePosition + randomOffset;
    }

    private IEnumerator MoveDoor(Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;
        while (elapsed < doorMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / doorMoveDuration;
            float smoothT = t * t * (3f - 2f * t);

            doorTransform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);
            yield return null;
        }

        doorTransform.localPosition = endPos;
    }

    protected override void DeliverService(Passenger passenger)
    {
        if (passenger != null)
        {
            PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Hygiene, passenger);
            EconomyManager.Instance.AddMoney(usePrice);

            if (WorldSpacePromptCoordinator.Instance != null)
            {
                WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                    "+$" + usePrice, transform.position + Vector3.up * 7f, Color.green); 
            }
        }

        if (facilityLight != null)
        {
            facilityLight.color = Color.white;
            facilityLight.intensity = idleLightIntensity;
        }

        SetNeedIconIdle(facilityIcon);
    }
}
