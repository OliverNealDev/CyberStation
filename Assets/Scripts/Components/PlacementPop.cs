using UnityEngine;

// Squash-and-settle when something is placed, so a click reads as a thing arriving
// rather than a thing appearing. Runs on unscaled time so it still plays while paused.
public class PlacementPop : MonoBehaviour
{
    private const float Duration = 0.32f;
    private const float StartScale = 0.6f;
    private const float Overshoot = 1.12f;

    private Vector3 targetScale;
    private float elapsed;

    public static void Play(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        PlacementPop pop = target.GetComponent<PlacementPop>();
        if (pop == null)
        {
            pop = target.AddComponent<PlacementPop>();
        }

        pop.Begin();
    }

    private void Begin()
    {
        if (elapsed <= 0f || elapsed >= Duration)
        {
            targetScale = transform.localScale;
        }

        elapsed = 0f;
        enabled = true;
        transform.localScale = targetScale * StartScale;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / Duration);

        // Rise past full size in the first 60%, then settle back onto it.
        float scale = t < 0.6f
            ? Mathf.Lerp(StartScale, Overshoot, EaseOut(t / 0.6f))
            : Mathf.Lerp(Overshoot, 1f, EaseInOut((t - 0.6f) / 0.4f));

        transform.localScale = targetScale * scale;

        if (t >= 1f)
        {
            transform.localScale = targetScale;
            enabled = false;
        }
    }

    private void OnDisable()
    {
        if (targetScale != Vector3.zero)
        {
            transform.localScale = targetScale;
        }
    }

    private static float EaseOut(float t)
    {
        return 1f - ((1f - t) * (1f - t));
    }

    private static float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
