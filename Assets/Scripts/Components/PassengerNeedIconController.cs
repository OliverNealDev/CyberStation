using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PassengerNeedIconController : MonoBehaviour
{
    [SerializeField] private float blinkSpeed = 7f;
    [SerializeField] private float minBlinkAlpha = 0.2f;
    [SerializeField] private float popDuration = 0.32f;
    [SerializeField] private float popOvershootScale = 1.14f;
    [SerializeField] private float fadeOutDuration = 0.22f;
    [SerializeField] private float alertTransitionDuration = 0.35f;

    private Passenger targetPassenger;
    private Image iconImage;
    private Vector3 worldOffset = new Vector3(0f, 5.5f, 0f);
    private Color normalColor = Color.white;
    private Color alertColor = Color.red;
    private bool isBlinking;
    private Vector3 baseLocalScale = Vector3.one;
    private float alphaMultiplier = 1f;
    private float alertTransition01;
    private Coroutine popCoroutine;
    private Coroutine fadeCoroutine;
    private Coroutine alertCoroutine;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
        baseLocalScale = transform.localScale;
    }

    public void Initialize(Passenger passenger, Sprite sprite, Color color, Vector3 offset)
    {
        targetPassenger = passenger;
        normalColor = color;
        worldOffset = offset;

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        if (iconImage != null)
        {
            iconImage.sprite = sprite;
        }

        alphaMultiplier = 1f;
        UpdatePosition();
        PlayPopInAnimation();
        ApplyVisuals();
    }

    public void SetSprite(Sprite sprite)
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        if (iconImage != null)
        {
            iconImage.sprite = sprite;
        }
    }

    public void SetNormalColor(Color color)
    {
        normalColor = color;
        ApplyVisuals();
    }

    public void SetAlertState(bool shouldAlert, bool shouldBlink)
    {
        if (alertCoroutine != null)
        {
            StopCoroutine(alertCoroutine);
        }

        alertCoroutine = StartCoroutine(AlertTransitionRoutine(shouldAlert, shouldBlink));
    }

    public void FadeOutAndDestroy()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    private void LateUpdate()
    {
        if (targetPassenger == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePosition();
        ApplyVisuals();
    }

    private void UpdatePosition()
    {
        transform.position = targetPassenger.transform.position + worldOffset;
    }

    private void ApplyVisuals()
    {
        if (iconImage == null)
        {
            return;
        }

        float alpha = 1f;
        if (isBlinking)
        {
            alpha = Mathf.Lerp(minBlinkAlpha, 1f, 0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * blinkSpeed)));
        }

        Color activeColor = Color.Lerp(normalColor, alertColor, alertTransition01);
        iconImage.color = new Color(activeColor.r, activeColor.g, activeColor.b, alpha * alphaMultiplier);
    }

    private void PlayPopInAnimation()
    {
        if (popCoroutine != null)
        {
            StopCoroutine(popCoroutine);
        }

        popCoroutine = StartCoroutine(PopInRoutine());
    }

    private System.Collections.IEnumerator PopInRoutine()
    {
        float elapsed = 0f;
        transform.localScale = Vector3.zero;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, popDuration));
            float scaleFactor;

            if (t < 0.7f)
            {
                scaleFactor = Mathf.Lerp(0f, popOvershootScale, t / 0.7f);
            }
            else
            {
                scaleFactor = Mathf.Lerp(popOvershootScale, 1f, (t - 0.7f) / 0.3f);
            }

            transform.localScale = baseLocalScale * scaleFactor;
            yield return null;
        }

        transform.localScale = baseLocalScale;
        popCoroutine = null;
    }

    private System.Collections.IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;
        float startingAlpha = alphaMultiplier;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeOutDuration));
            alphaMultiplier = Mathf.Lerp(startingAlpha, 0f, t);
            ApplyVisuals();
            yield return null;
        }

        Destroy(gameObject);
    }

    private System.Collections.IEnumerator AlertTransitionRoutine(bool shouldAlert, bool shouldBlink)
    {
        float start = alertTransition01;
        float target = shouldAlert ? 1f : 0f;

        if (!shouldAlert)
        {
            isBlinking = false;
        }

        float elapsed = 0f;
        while (elapsed < alertTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, alertTransitionDuration));
            alertTransition01 = Mathf.Lerp(start, target, t);
            ApplyVisuals();
            yield return null;
        }

        alertTransition01 = target;
        isBlinking = shouldAlert && shouldBlink;
        ApplyVisuals();
        alertCoroutine = null;
    }
}
