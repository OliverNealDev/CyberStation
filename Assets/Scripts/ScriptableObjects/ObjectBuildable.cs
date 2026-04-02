using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

[CreateAssetMenu(fileName = "ObjectBuildable", menuName = "Scriptable Objects/ObjectBuildable")]
public class ObjectBuildable : ScriptableObject
{
    public string objectName = "New Buildable Object";
    public int requiredTier = 1;
    [TextArea]
    public string description = "Description of the buildable object.";
    public Sprite icon;
    public GameObject prefab;
    public int cost;
    
    // Changed to Vector2Int so it plays perfectly with our GridManager!
    public Vector2Int size = Vector2Int.one; 
    [Min(0f)] public float decorationStrength = 0f;

    [System.NonSerialized] private Sprite runtimeIcon;

    public Sprite GetIcon()
    {
        if (runtimeIcon == null)
        {
            runtimeIcon = PrefabIconRenderer.GetIcon(prefab, icon, PrefabIconView.BuildablesAndStaff);
        }

        return runtimeIcon;
    }
}

public enum PrefabIconView
{
    BuildablesAndStaff,
    TrainFront
}

public interface IPreviewInitializable
{
    void InitializePreviewVisuals();
}

public static class PrefabIconRenderer
{
    private const int PreviewLayer = 31;
    private const int IconSize = 256;

    private static readonly System.Collections.Generic.Dictionary<string, Sprite> CachedIcons =
        new System.Collections.Generic.Dictionary<string, Sprite>();

    private static Transform previewRoot;
    private static Camera previewCamera;
    private static Light previewLight;
    private static RenderTexture previewTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPreviewState()
    {
        ClearCachedIcons();
        CleanupPreviewRig();
        DestroyLeakedPreviewObjects();
    }

    public static Sprite GetIcon(GameObject prefab, Sprite fallback = null)
    {
        return GetIcon(prefab, fallback, PrefabIconView.BuildablesAndStaff);
    }

    public static Sprite GetIcon(GameObject prefab, Sprite fallback, PrefabIconView view)
    {
        if (prefab == null)
        {
            return fallback;
        }

        string cacheKey = prefab.GetInstanceID() + ":" + (int)view;
        Sprite cachedSprite;
        if (CachedIcons.TryGetValue(cacheKey, out cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        EnsurePreviewRig();

        PrefabIconRenderSettings settings = GetSettings(view);
        Sprite renderedSprite = RenderPrefabToSprite(prefab, settings);
        Sprite finalSprite = renderedSprite != null ? renderedSprite : fallback;

        if (finalSprite != null)
        {
            CachedIcons[cacheKey] = finalSprite;
        }

        return finalSprite;
    }

    private static void EnsurePreviewRig()
    {
        if (previewCamera != null && previewLight != null && previewTexture != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("PrefabIconRenderer");
        rootObject.hideFlags = HideFlags.HideInHierarchy;

        previewRoot = rootObject.transform;
        previewRoot.position = new Vector3(10000f, 10000f, 10000f);

        GameObject cameraObject = new GameObject("PrefabIconCamera");
        cameraObject.hideFlags = HideFlags.HideInHierarchy;
        cameraObject.transform.SetParent(previewRoot, false);

        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.orthographic = true;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = true;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 100f;

        previewTexture = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
        previewTexture.hideFlags = HideFlags.HideAndDontSave;
        previewTexture.name = "PrefabIconRendererTexture";
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;

        GameObject lightObject = new GameObject("PrefabIconLight");
        lightObject.hideFlags = HideFlags.HideInHierarchy;
        lightObject.transform.SetParent(previewRoot, false);

        previewLight = lightObject.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.35f;
        previewLight.color = Color.white;
        previewLight.shadows = LightShadows.None;
        previewLight.cullingMask = 1 << PreviewLayer;
        previewLight.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        previewLight.enabled = false;
    }

    private static Sprite RenderPrefabToSprite(GameObject prefab, PrefabIconRenderSettings settings)
    {
        GameObject previewInstance = Object.Instantiate(prefab);
        previewInstance.hideFlags = HideFlags.HideInHierarchy;
        previewInstance.name = prefab.name + "_Preview";
        previewInstance.transform.SetParent(previewRoot, false);
        previewInstance.transform.localPosition = Vector3.zero;
        previewInstance.transform.localRotation = Quaternion.identity;

        SetLayerRecursively(previewInstance.transform, PreviewLayer);
        PrepareInstanceForPreview(previewInstance);

        Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>(true);
        Bounds bounds;
        if (HasInvalidPreviewMaterials(renderers) || !TryGetRenderableBounds(renderers, out bounds))
        {
            previewInstance.SetActive(false);
            Object.Destroy(previewInstance);
            return null;
        }

        ConfigureCamera(bounds, settings);
        previewLight.enabled = true;
        previewCamera.Render();
        previewLight.enabled = false;

        Texture2D iconTexture = new Texture2D(IconSize, IconSize, TextureFormat.ARGB32, false);
        iconTexture.hideFlags = HideFlags.HideAndDontSave;
        iconTexture.name = prefab.name + "_RuntimeIconTexture";

        RenderTexture previousTarget = RenderTexture.active;
        RenderTexture.active = previewTexture;
        iconTexture.ReadPixels(new Rect(0f, 0f, IconSize, IconSize), 0, 0);
        iconTexture.Apply();
        RenderTexture.active = previousTarget;

        Sprite iconSprite = Sprite.Create(
            iconTexture,
            new Rect(0f, 0f, IconSize, IconSize),
            new Vector2(0.5f, 0.5f),
            100f);

        iconSprite.hideFlags = HideFlags.HideAndDontSave;
        iconSprite.name = prefab.name + "_RuntimeIcon";

        previewInstance.SetActive(false);
        Object.Destroy(previewInstance);

        return iconSprite;
    }

    private static void ClearCachedIcons()
    {
        foreach (var kvp in CachedIcons)
        {
            Sprite cachedSprite = kvp.Value;
            if (cachedSprite == null)
            {
                continue;
            }

            Texture2D texture = cachedSprite.texture;
            DestroyObject(cachedSprite);

            if (texture != null)
            {
                DestroyObject(texture);
            }
        }

        CachedIcons.Clear();
    }

    private static void CleanupPreviewRig()
    {
        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
        }

        if (previewTexture != null)
        {
            DestroyObject(previewTexture);
        }

        if (previewRoot != null)
        {
            DestroyObject(previewRoot.gameObject);
        }

        previewRoot = null;
        previewCamera = null;
        previewLight = null;
        previewTexture = null;
    }

    private static void DestroyLeakedPreviewObjects()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];
            if (obj == null)
            {
                continue;
            }

            if (obj.name == "PrefabIconRenderer" || obj.name == "PrefabIconCamera" || obj.name == "PrefabIconLight")
            {
                DestroyObject(obj);
            }
        }
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

    private static void PrepareInstanceForPreview(GameObject previewInstance)
    {
        Behaviour[] behaviours = previewInstance.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPreviewInitializable previewInitializable)
            {
                previewInitializable.InitializePreviewVisuals();
            }
        }

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null || ShouldKeepBehaviourEnabled(behaviours[i]))
            {
                continue;
            }

            behaviours[i].enabled = false;
        }

        Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        Rigidbody[] rigidbodies = previewInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if (rigidbodies[i] == null)
            {
                continue;
            }

            rigidbodies[i].linearVelocity = Vector3.zero;
            rigidbodies[i].angularVelocity = Vector3.zero;
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].detectCollisions = false;
        }

        ParticleSystem[] particleSystems = previewInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private static bool ShouldKeepBehaviourEnabled(Behaviour behaviour)
    {
        if (behaviour is Canvas)
        {
            return true;
        }

        if (behaviour is CanvasScaler)
        {
            return true;
        }

        if (behaviour is TMP_Text)
        {
            return true;
        }

        if (behaviour is Graphic)
        {
            return true;
        }

        return false;
    }

    private static bool TryGetRenderableBounds(Renderer[] renderers, out Bounds bounds)
    {
        bool foundBounds = false;
        bounds = new Bounds(Vector3.zero, Vector3.one);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!foundBounds)
            {
                bounds = renderer.bounds;
                foundBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return foundBounds;
    }

    private static bool HasInvalidPreviewMaterials(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null)
                {
                    continue;
                }

                Shader shader = material.shader;
                if (shader == null || !shader.isSupported || shader.name == "Hidden/InternalErrorShader")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ConfigureCamera(Bounds bounds, PrefabIconRenderSettings settings)
    {
        previewCamera.aspect = 1f;
        previewCamera.transform.rotation = Quaternion.Euler(settings.pitch, settings.yaw, 0f);

        float distance = Mathf.Max(bounds.size.magnitude * settings.distanceMultiplier, 3f);
        previewCamera.transform.position = bounds.center - (previewCamera.transform.forward * distance);

        Vector3[] corners = GetBoundsCorners(bounds);
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 localCorner = previewCamera.transform.InverseTransformPoint(corners[i]);
            minX = Mathf.Min(minX, localCorner.x);
            maxX = Mathf.Max(maxX, localCorner.x);
            minY = Mathf.Min(minY, localCorner.y);
            maxY = Mathf.Max(maxY, localCorner.y);
            minZ = Mathf.Min(minZ, localCorner.z);
            maxZ = Mathf.Max(maxZ, localCorner.z);
        }

        float halfWidth = Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX));
        float halfHeight = Mathf.Max(Mathf.Abs(minY), Mathf.Abs(maxY));
        previewCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / previewCamera.aspect) * settings.padding;

        float clipPadding = Mathf.Max(bounds.size.magnitude, 1f);
        previewCamera.nearClipPlane = Mathf.Max(0.01f, minZ - clipPadding);
        previewCamera.farClipPlane = maxZ + clipPadding;
    }

    private static PrefabIconRenderSettings GetSettings(PrefabIconView view)
    {
        switch (view)
        {
            case PrefabIconView.TrainFront:
                return new PrefabIconRenderSettings(18f, 135f, 1.9f, 1.15f);
            default:
                return new PrefabIconRenderSettings(20f, 145f, 1.75f, 1.2f);
        }
    }

    private static Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        return new[]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };
    }

    private static void SetLayerRecursively(Transform current, int layer)
    {
        current.gameObject.layer = layer;

        for (int i = 0; i < current.childCount; i++)
        {
            SetLayerRecursively(current.GetChild(i), layer);
        }
    }

    private struct PrefabIconRenderSettings
    {
        public readonly float pitch;
        public readonly float yaw;
        public readonly float distanceMultiplier;
        public readonly float padding;

        public PrefabIconRenderSettings(float pitch, float yaw, float distanceMultiplier, float padding)
        {
            this.pitch = pitch;
            this.yaw = yaw;
            this.distanceMultiplier = distanceMultiplier;
            this.padding = padding;
        }
    }
}
