using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float rotationSpeed = 100f;
    public float zoomSpeed = 2f;
    public float minHeight = 5f;
    public float maxHeight = 50f;

    private Camera controlledCamera;
    private Vector2 rightDragStartScreenPosition;
    private Vector2 lastRightDragScreenPosition;
    private float rightDragPlaneHeight;
    private bool isTrackingRightDrag;
    private bool isRightDragPanning;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        if (controlledCamera == null)
        {
            controlledCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        if (UIController.IsCameraInputBlockedByMenu)
        {
            EndRightDragTracking();
            return;
        }

        HandleMovement();
        HandleMousePan();
        if (!isRightDragPanning)
        {
            HandleRotation();
        }
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

        transform.Rotate(Vector3.up, rotateDir * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void HandleMousePan()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            BeginRightDragTracking();
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            EndRightDragTracking();
            return;
        }

        if (!isTrackingRightDrag)
        {
            return;
        }

        Vector2 currentMousePosition = Mouse.current.position.ReadValue();
        if (!isRightDragPanning &&
            !PointerDragUtility.HasExceededDragThreshold(rightDragStartScreenPosition, currentMousePosition))
        {
            return;
        }

        Camera activeCamera = controlledCamera != null ? controlledCamera : Camera.main;
        if (activeCamera == null ||
            !PointerDragUtility.TryGetGroundPlanePoint(activeCamera, lastRightDragScreenPosition, out Vector3 previousWorldPosition, rightDragPlaneHeight) ||
            !PointerDragUtility.TryGetGroundPlanePoint(activeCamera, currentMousePosition, out Vector3 currentWorldPosition, rightDragPlaneHeight))
        {
            return;
        }

        isRightDragPanning = true;
        Vector3 panOffset = previousWorldPosition - currentWorldPosition;
        transform.position += panOffset;
        lastRightDragScreenPosition = currentMousePosition;
    }

    private void BeginRightDragTracking()
    {
        if (PointerUiUtility.IsPointerOverBlockingUi())
        {
            EndRightDragTracking();
            return;
        }

        Camera activeCamera = controlledCamera != null ? controlledCamera : Camera.main;
        if (!PointerDragUtility.TryGetGroundPlanePoint(activeCamera, Mouse.current.position.ReadValue(), out Vector3 startWorldPosition))
        {
            EndRightDragTracking();
            return;
        }

        isTrackingRightDrag = true;
        isRightDragPanning = false;
        rightDragStartScreenPosition = Mouse.current.position.ReadValue();
        lastRightDragScreenPosition = rightDragStartScreenPosition;
        rightDragPlaneHeight = startWorldPosition.y;
    }

    private void EndRightDragTracking()
    {
        isTrackingRightDrag = false;
        isRightDragPanning = false;
    }

    private void HandleZoom()
    {
        if (PointerUiUtility.IsPointerOverScrollableUi())
        {
            return;
        }

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
