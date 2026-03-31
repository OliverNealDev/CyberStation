using System.Collections.Generic;
using UnityEngine;

public class SquareBillboardController : MonoBehaviour
{
    [Header("Adverts")]
    public List<GameObject> advertPrefabs = new List<GameObject>();
    public Transform advertTransform;

    [Header("Motion")]
    public Transform floatingTransform;
    public float floatSpeed = 0.15f;
    public float minFloatLocalY = 3f;
    public float maxFloatLocalY = 4f;

    private GameObject currentAdvertInstance;

    private void Start()
    {
        SpawnRandomAdvert();
    }

    private void Update()
    {
        AnimateFloat();
    }

    public void SpawnRandomAdvert()
    {
        CleanupCurrentAdvert();

        if (!TryGetAdvertTransform(out Transform targetTransform))
        {
            Debug.LogError("SquareBillboardController could not find an advert transform.", this);
            return;
        }

        GameObject advertPrefab = GetRandomAdvertPrefab();
        if (advertPrefab == null)
        {
            Debug.LogError("SquareBillboardController has no valid advert prefabs assigned.", this);
            return;
        }

        currentAdvertInstance = Instantiate(advertPrefab, targetTransform.position, targetTransform.rotation, targetTransform);
        currentAdvertInstance.transform.localPosition = Vector3.zero;
        currentAdvertInstance.transform.localRotation = Quaternion.identity;

        PointerUiUtility.DisableWorldSpaceCanvasInteraction(currentAdvertInstance);
    }

    private bool TryGetAdvertTransform(out Transform targetTransform)
    {
        if (advertTransform == null)
        {
            advertTransform = transform.Find("AdvertTransform");
        }

        targetTransform = advertTransform;
        return targetTransform != null;
    }

    private GameObject GetRandomAdvertPrefab()
    {
        if (advertPrefabs == null || advertPrefabs.Count == 0)
        {
            return null;
        }

        int startIndex = Random.Range(0, advertPrefabs.Count);

        for (int offset = 0; offset < advertPrefabs.Count; offset++)
        {
            GameObject advertPrefab = advertPrefabs[(startIndex + offset) % advertPrefabs.Count];
            if (advertPrefab != null)
            {
                return advertPrefab;
            }
        }

        return null;
    }

    private void CleanupCurrentAdvert()
    {
        if (currentAdvertInstance == null)
        {
            return;
        }

        Destroy(currentAdvertInstance);
        currentAdvertInstance = null;
    }

    private void AnimateFloat()
    {
        if (floatingTransform == null)
        {
            return;
        }

        float minY = Mathf.Min(minFloatLocalY, maxFloatLocalY);
        float maxY = Mathf.Max(minFloatLocalY, maxFloatLocalY);
        float cycle = Mathf.PingPong(Time.time * floatSpeed, 1f);
        float easedCycle = Mathf.SmoothStep(0f, 1f, cycle);

        Vector3 localPosition = floatingTransform.localPosition;
        localPosition.y = Mathf.Lerp(minY, maxY, easedCycle);
        floatingTransform.localPosition = localPosition;
    }
}
