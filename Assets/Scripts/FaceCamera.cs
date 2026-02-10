using UnityEngine;
public class FaceCamera : MonoBehaviour
{
    public Camera mainCamera;
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Automatically assign the main camera
        }
    }
    void Update()
    {
        // Make the text face the camera
        transform.LookAt(mainCamera.transform);
        // Adjust rotation to prevent flipping
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + 180, 0);
    }
}