using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrthographicCameraController : MonoBehaviour
{
    private Camera cam;

    [Header("Movement")]
    public float moveSpeed = 20f;
    
    [Header("Rotation")]
    public float rotationTime = 0.15f;
    private bool isRotating = false;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f; 
    public float maxZoom = 50f;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main; 
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        HandleMovement();
        HandleZoom();

        if (!isRotating)
        {
            HandleRotation();
        }
    }

    private void HandleMovement()
    {
        float xInput = 0f;
        float zInput = 0f;

        if (Keyboard.current.wKey.isPressed) zInput = 1f;
        if (Keyboard.current.sKey.isPressed) zInput = -1f;
        if (Keyboard.current.aKey.isPressed) xInput = -1f;
        if (Keyboard.current.dKey.isPressed) xInput = 1f;

        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 moveDir = (forward * zInput + right * xInput).normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            StartCoroutine(RotateAroundCenter(45f));
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(RotateAroundCenter(-45f));
        }
    }

    private IEnumerator RotateAroundCenter(float angle)
    {
        isRotating = true;

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = new Ray(transform.position, transform.forward);
        
        Vector3 pivotPoint;
        if (groundPlane.Raycast(ray, out float distance))
        {
            pivotPoint = ray.GetPoint(distance);
        }
        else
        {
            pivotPoint = transform.position + transform.forward * 10f; 
        }

        Quaternion startRot = transform.rotation;
        Vector3 startPos = transform.position;
        
        Quaternion endRot = Quaternion.Euler(0, angle, 0) * startRot;
        Vector3 offset = startPos - pivotPoint;
        Vector3 endPos = pivotPoint + (Quaternion.Euler(0, angle, 0) * offset);

        float elapsed = 0f;
        while (elapsed < rotationTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationTime);
            
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        isRotating = false;
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.y.ReadValue();

        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (cam.orthographic)
            {
                float zoomAmount = scroll > 0 ? -zoomSpeed : zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + zoomAmount, minZoom, maxZoom);
            }
            else
            {
                Vector3 moveDir = transform.forward * (scroll > 0 ? 1 : -1);
                Vector3 targetPos = transform.position + (moveDir * zoomSpeed);

                if (targetPos.y >= minZoom && targetPos.y <= maxZoom)
                {
                    transform.position = targetPos;
                }
            }
        }
    }
}