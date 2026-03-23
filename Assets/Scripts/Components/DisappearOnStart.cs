using UnityEngine;
using UnityEngine.Serialization;

public class DisappearOnStart : MonoBehaviour
{
    [FormerlySerializedAs("dissapearOnStart")]
    [SerializeField] private bool disappearOnStart = true;

    private void Start()
    {
        if (!disappearOnStart)
        {
            return;
        }

        if (TryGetComponent<Renderer>(out var targetRenderer))
        {
            targetRenderer.enabled = false;
        }
    }
}
