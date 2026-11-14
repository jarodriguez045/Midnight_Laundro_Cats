using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] PlayerSwingController swingController;
    [SerializeField] Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] float followSmoothing = 18f;
    [SerializeField] float mouseSensitivityX = 4.5f;
    [SerializeField] float mouseSensitivityY = 3.5f;
    [SerializeField] float minPitch = -35f;
    [SerializeField] float maxPitch = 72f;
    [SerializeField] float normalDistance = 7f;
    [SerializeField] float swingingDistance = 10.5f;
    [SerializeField] float distanceSmoothing = 8f;
    [SerializeField] LayerMask collisionMask;
    [SerializeField] float collisionRadius = 0.28f;
    [SerializeField] float collisionPadding = 0.25f;

    float _yaw;
    float _pitch = 12f;
    float _currentDistance;
    bool _cursorLocked = true;

    public float Yaw => _yaw;
    public float Pitch => _pitch;
    public float CurrentDistance => _currentDistance;

    void Awake()
    {
        _currentDistance = normalDistance;
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        SetCursorLocked(true);
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(!_cursorLocked);
        }

        if (target == null)
        {
            return;
        }

        if (_cursorLocked)
        {
            _yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivityX;
            _pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivityY;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        bool swinging = swingController != null && swingController.IsSwinging;
        float desiredDistance = swinging ? swingingDistance : normalDistance;
        _currentDistance = Mathf.Lerp(_currentDistance, desiredDistance, 1f - Mathf.Exp(-distanceSmoothing * Time.deltaTime));

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 focus = target.position + targetOffset;
        Vector3 desiredPosition = focus - rotation * Vector3.forward * _currentDistance;

        float resolvedDistance = _currentDistance;
        Vector3 direction = desiredPosition - focus;
        float magnitude = direction.magnitude;
        if (magnitude > 0.001f &&
            Physics.SphereCast(focus, collisionRadius, direction.normalized, out RaycastHit hit, magnitude, collisionMask, QueryTriggerInteraction.Ignore))
        {
            resolvedDistance = Mathf.Max(0.4f, hit.distance - collisionPadding);
            desiredPosition = focus - rotation * Vector3.forward * resolvedDistance;
        }

        float t = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
    }

    void SetCursorLocked(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
