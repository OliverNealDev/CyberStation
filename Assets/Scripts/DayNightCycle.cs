using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public float dayLengthInSeconds = 600f;
    public float nightLengthInSeconds = 300f;

    public Light sunLight;

    public AnimationCurve intensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.15f),
        new Keyframe(0.25f, 0.6f),
        new Keyframe(0.5f, 0.15f),
        new Keyframe(0.75f, 0.05f),
        new Keyframe(1f, 0.15f)
    );

    public AnimationCurve temperatureCurve = new AnimationCurve(
        new Keyframe(0f, 3000f),
        new Keyframe(0.25f, 6500f),
        new Keyframe(0.5f, 3000f),
        new Keyframe(0.75f, 10000f),
        new Keyframe(1f, 3000f)
    );

    private float currentXRotation = 0f;
    private float initialYRotation;
    private float initialZRotation;

    void Start()
    {
        if (sunLight == null)
        {
            sunLight = GetComponent<Light>();
        }

        sunLight.useColorTemperature = true;

        initialYRotation = sunLight.transform.eulerAngles.y;
        initialZRotation = sunLight.transform.eulerAngles.z;
        currentXRotation = 0f;
    }

    void Update()
    {
        float rotationSpeed = CalculateRotationSpeed();
        currentXRotation += rotationSpeed * Time.deltaTime;

        if (currentXRotation >= 360f)
        {
            currentXRotation -= 360f;
            OnNewDayStarted();
        }

        sunLight.transform.rotation = Quaternion.Euler(currentXRotation, initialYRotation, initialZRotation);

        float cyclePercent = currentXRotation / 360f;
        sunLight.intensity = intensityCurve.Evaluate(cyclePercent);
        sunLight.colorTemperature = temperatureCurve.Evaluate(cyclePercent);
    }

    private float CalculateRotationSpeed()
    {
        if (currentXRotation < 180f)
        {
            return 180f / dayLengthInSeconds;
        }
        else
        {
            return 180f / nightLengthInSeconds;
        }
    }

    private void OnNewDayStarted()
    {
        
    }
}