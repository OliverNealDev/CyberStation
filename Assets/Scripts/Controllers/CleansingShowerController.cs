using UnityEngine;
using System.Collections;

public class CleansingShowerController : StationFacility
{
    public int usePrice = 1;

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
    public Color idleIconColor = Color.lightGray;

    [Header("Shower Settings")]
    public float showerDuration = 4.0f;
    public float dropletSpawnRate = 0.05f;
    public float dropletFallDuration = 0.4f;

    [Header("Droplet Shape (Cuboid)")]
    public float dropletWidth = 0.05f;
    public float dropletLength = 0.4f;

    protected override void Start()
    {
        base.Start();

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
            StartCoroutine(ShowerRoutine((Passenger)person));
        }
    }

    private IEnumerator ShowerRoutine(Passenger passenger)
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
                SpawnDroplet(passengerColor, passenger);
            }

            yield return null;
        }

        yield return new WaitForSeconds(dropletFallDuration);

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
        
        Vector3 spawnPos = showerHeadPoint.position + randomOffset;

        GameObject droplet = Instantiate(dropletPrefab, spawnPos, Quaternion.identity);
        
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

    protected override void DeliverService(Passenger passenger)
    {
        if (passenger == null) return;

        PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Hygiene, passenger);
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