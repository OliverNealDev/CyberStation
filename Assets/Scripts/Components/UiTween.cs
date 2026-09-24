using System;
using System.Collections;
using UnityEngine;

// Coroutine tweens for the runtime HUD. All on unscaled time: the HUD has to keep
// animating while the simulation is paused, which is exactly when players read it.
public static class UiTween
{
    public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
        {
            yield break;
        }

        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            if (group == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, SmoothStep(elapsed / duration));
            yield return null;
        }

        if (group != null)
        {
            group.alpha = to;
        }
    }

    public static IEnumerator Move(RectTransform rect, Vector2 from, Vector2 to, float duration, Func<float, float> ease = null)
    {
        if (rect == null)
        {
            yield break;
        }

        ease = ease ?? EaseOutCubic;
        float elapsed = 0f;
        rect.anchoredPosition = from;
        while (elapsed < duration)
        {
            if (rect == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        if (rect != null)
        {
            rect.anchoredPosition = to;
        }
    }

    public static IEnumerator ScaleIn(Transform target, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        float elapsed = 0f;
        target.localScale = Vector3.one * 0.6f;
        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(Mathf.Clamp01(elapsed / duration)));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        if (target != null)
        {
            target.localScale = Vector3.one;
        }
    }

    public static IEnumerator Punch(Transform target, float amount, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI) * (1f - t);
            target.localScale = Vector3.one * (1f + (amount * wave));
            yield return null;
        }

        if (target != null)
        {
            target.localScale = Vector3.one;
        }
    }

    public static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }

    public static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        t = Mathf.Clamp01(t) - 1f;
        return 1f + (t * t * (((overshoot + 1f) * t) + overshoot));
    }
}
