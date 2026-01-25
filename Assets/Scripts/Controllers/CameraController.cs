using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float rotationSpeed = 100f;
    public float mouseDragSensitivity = 0.5f;
    public float zoomSpeed = 2f;
    public float minHeight = 5f;
    public float maxHeight = 50f;

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        HandleMovement();
        HandleRotation();
        HandleZoom();
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
        float rotateDir = 0f;

        if (Keyboard.current.qKey.isPressed) rotateDir = -1f;
        if (Keyboard.current.eKey.isPressed) rotateDir = 1f;

        if (Mouse.current.rightButton.isPressed)
        {
            rotateDir += Mouse.current.delta.x.ReadValue() * mouseDragSensitivity;
        }

        transform.Rotate(Vector3.up, rotateDir * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.y.ReadValue();

        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 moveDir = transform.forward * (scroll > 0 ? 1 : -1);
            Vector3 targetPos = transform.position + (moveDir * zoomSpeed);

            if (targetPos.y >= minHeight && targetPos.y <= maxHeight)
            {
                transform.position = targetPos;
            }
        }
    }
}