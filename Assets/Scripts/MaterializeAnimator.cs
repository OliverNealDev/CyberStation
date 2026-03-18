using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterializeAnimator : MonoBehaviour
{
    private List<Transform> bodyParts = new List<Transform>();
    private List<Vector3> originalScales = new List<Vector3>();

    void Awake()
    {
        foreach (Transform child in transform)
        {
            bodyParts.Add(child);
            originalScales.Add(child.localScale);
        }
    }

    public void Materialize(System.Action onComplete = null)
    {
        StartCoroutine(MaterializeRoutine(onComplete));
    }

    public void Dematerialize(System.Action onComplete = null)
    {
        StartCoroutine(DematerializeRoutine(onComplete));
    }
    
    public void Pop()
    {
        StartCoroutine(PopRoutine());
    }

    private IEnumerator MaterializeRoutine(System.Action onComplete)
    {
        float spawnDuration = 1.0f;
        float spawnElapsed = 0f;

        for (int i = 0; i < bodyParts.Count; i++)
        {
            bodyParts[i].localScale = Vector3.zero; 
        }

        while (spawnElapsed < spawnDuration)
        {
            for (int i = 0; i < bodyParts.Count; i++)
            {
                float delay = i * 0.2f; 
                float partT = Mathf.Clamp01((spawnElapsed - delay) / (spawnDuration - 0.4f)); 
                partT = 1f - Mathf.Pow(1f - partT, 3f);
                
                bodyParts[i].localScale = Vector3.Lerp(Vector3.zero, originalScales[i], partT);
            }

            spawnElapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < bodyParts.Count; i++)
        {
            bodyParts[i].localScale = originalScales[i];
        }

        yield return StartCoroutine(PopRoutine());
        onComplete?.Invoke();
    }

    private IEnumerator PopRoutine()
    {
        float popUpDuration = 0.15f;
        float popDownDuration = 0.2f;
        Vector3 baseScale = Vector3.one;
        Vector3 peakScale = baseScale * 1.35f;

        float elapsed = 0f;
        while (elapsed < popUpDuration)
        {
            float t = elapsed / popUpDuration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localScale = Vector3.Lerp(baseScale, peakScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < popDownDuration)
        {
            float t = elapsed / popDownDuration;
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(peakScale, baseScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = baseScale;
    }

    private IEnumerator DematerializeRoutine(System.Action onComplete)
    {
        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * t; // Smooth ease-in
            
            for (int i = 0; i < bodyParts.Count; i++)
            {
                bodyParts[i].localScale = Vector3.Lerp(originalScales[i], Vector3.zero, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        onComplete?.Invoke();
    }
}