using UnityEngine;
using System.Collections.Generic;

public class TrainlineGlobeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform globeTransform;
    [SerializeField] private Transform globeTopPosition;
    [SerializeField] private Transform globeBottomPosition;
    [SerializeField] private Transform trainlineRingTemplate;

    [Header("Motion")]
    [SerializeField] private float floatSpeed = 0.15f;
    [SerializeField] private float ringRotationSpeed = 15f;
    [SerializeField] private Vector3 ringRotationAxis = Vector3.right;
    [SerializeField] private float ringTiltStep = 22.5f;
    [SerializeField] private float ringYawStep = 40f;
    [SerializeField] private float ringScaleStep = 0.01f;

    private const string GlobePath = "Model/Globe";
    private const string GlobeTopName = "GlobeTopPosition";
    private const string GlobeBottomName = "GlobeBottomPosition";
    private const string TrainlineRingName = "TrainlineRing";

    private readonly List<Transform> activeRings = new List<Transform>();

    private void Awake()
    {
        CacheReferences();
        SetTemplateVisible(false);
    }

    private void OnEnable()
    {
        TrainManager.OnTrainAssignmentsChanged += RefreshActiveTrainRings;
        RefreshActiveTrainRings();
    }

    private void OnDisable()
    {
        TrainManager.OnTrainAssignmentsChanged -= RefreshActiveTrainRings;
    }

    private void OnValidate()
    {
        CacheReferences();
    }

    private void Update()
    {
        AnimateGlobeFloat();
        RotateTrainlineRing();
    }

    private void CacheReferences()
    {
        globeTransform ??= transform.Find(GlobePath);
        globeTopPosition ??= transform.Find(GlobeTopName);
        globeBottomPosition ??= transform.Find(GlobeBottomName);

        if (trainlineRingTemplate == null)
        {
            trainlineRingTemplate = FindChildRecursive(transform, TrainlineRingName);
        }
    }

    private void AnimateGlobeFloat()
    {
        if (globeTransform == null || globeTopPosition == null || globeBottomPosition == null)
        {
            return;
        }

        float cycle = Mathf.PingPong(Time.time * floatSpeed, 1f);
        float easedCycle = Mathf.SmoothStep(0f, 1f, cycle);

        globeTransform.localPosition = Vector3.Lerp(
            globeBottomPosition.localPosition,
            globeTopPosition.localPosition,
            easedCycle
        );
    }

    private void RotateTrainlineRing()
    {
        if (activeRings.Count == 0)
        {
            return;
        }

        Vector3 axis = ringRotationAxis.sqrMagnitude > 0f ? ringRotationAxis.normalized : Vector3.right;
        for (int i = 0; i < activeRings.Count; i++)
        {
            Transform ring = activeRings[i];
            if (ring == null)
            {
                continue;
            }

            ring.Rotate(axis, ringRotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void RefreshActiveTrainRings()
    {
        ClearActiveRings();

        if (trainlineRingTemplate == null || TrainManager.Instance == null)
        {
            SetTemplateVisible(false);
            return;
        }

        List<TrainService> services = TrainManager.Instance.activeTrainServices;
        for (int i = 0; i < services.Count; i++)
        {
            TrainService service = services[i];
            if (service?.trainData == null)
            {
                continue;
            }

            Transform spawnedRing = Instantiate(trainlineRingTemplate, trainlineRingTemplate.parent);
            spawnedRing.name = $"{TrainlineRingName}_{service.trainData.trainName}_{i + 1}";
            spawnedRing.gameObject.SetActive(true);
            spawnedRing.localPosition = trainlineRingTemplate.localPosition;
            spawnedRing.localScale = trainlineRingTemplate.localScale + (Vector3.one * (ringScaleStep * i));
            spawnedRing.localRotation = Quaternion.Euler(
                (i + 1) * ringTiltStep,
                i * ringYawStep,
                0f
            );

            ApplyRingColor(spawnedRing, service.trainData.trainColor);
            activeRings.Add(spawnedRing);
        }

        SetTemplateVisible(false);
    }

    private void ClearActiveRings()
    {
        for (int i = 0; i < activeRings.Count; i++)
        {
            if (activeRings[i] != null)
            {
                Destroy(activeRings[i].gameObject);
            }
        }

        activeRings.Clear();
    }

    private void ApplyRingColor(Transform ring, Color ringColor)
    {
        MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", ringColor);
        propertyBlock.SetColor("_Color", ringColor);
        propertyBlock.SetColor("_EmissionColor", ringColor);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void SetTemplateVisible(bool isVisible)
    {
        if (trainlineRingTemplate != null)
        {
            trainlineRingTemplate.gameObject.SetActive(isVisible);
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindChildRecursive(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}
