using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public Camera mainCamera;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        FaceCameraMethod();
    }

    private void LateUpdate()
    {
        FaceCameraMethod();
    }

    private void FaceCameraMethod()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        // Screen-aligned billboard: match the camera's orientation exactly.
        // This avoids edge-of-screen skew in orthographic mode and keeps
        // world-space UI from appearing mirrored.
        transform.rotation = mainCamera.transform.rotation;
    }
}
