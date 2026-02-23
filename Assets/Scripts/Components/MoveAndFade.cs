using TMPro;
using UnityEngine;

public class MoveAndFade : MonoBehaviour
{
    public float verticalSpeed = 1f;
    public float fadeDuration = 1f;
    void Update()
    {
        transform.position += Vector3.up * verticalSpeed * Time.deltaTime;

        Color currentColor = GetComponent<TextMeshProUGUI>().color;
        currentColor.a -= Time.deltaTime / fadeDuration;
        GetComponent<TextMeshProUGUI>().color = currentColor;

        if (currentColor.a <= 0)
        {
            Destroy(gameObject);
        }
    }
}
