using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerWallClimbController : MonoBehaviour
{
    [SerializeField] Camera inputCamera;
    [SerializeField] PlayerMovementController movementController;
    [SerializeField] PlayerSwingController swingController;
    [SerializeField] LayerMask climbableMask;
    [SerializeField] float wallCheckDistance = 1.25f;
    [SerializeField] float wallCheckRadius = 0.35f;
    [SerializeField] float wallHoldDistance = 0.65f;
    [SerializeField] KeyCode climbKey = KeyCode.F;
    [SerializeField] float climbSpeed = 5.5f;
    [SerializeField] float sidewaysSpeed = 4.5f;
    [SerializeField] float wallStickForce = 6f;
    [SerializeField] float jumpOffUpVelocity = 7f;
    [SerializeField] float jumpOffAwayVelocity = 7f;

    Rigidbody _body;
    bool _isClimbing;
    Vector3 _wallNormal;

    public bool IsClimbing => _isClimbing;
    public Vector3 WallNormal => _wallNormal;

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(climbKey))
        {
            if (_isClimbing)
            {
                StopClimb();
            }
            else
            {
                TryStartClimb();
            }
        }

        if (_isClimbing && Input.GetButtonDown("Jump"))
        {
            JumpOffWall();
        }
    }

    void FixedUpdate()
    {
        if (!_isClimbing)
        {
            return;
        }

        if (!TryFindWall(out RaycastHit hit))
        {
            StopClimb();
            return;
        }

        _wallNormal = hit.normal;
        Vector3 holdPoint = hit.point + hit.normal * wallHoldDistance;
        Vector3 toHold = holdPoint - transform.position;
        toHold = Vector3.ProjectOnPlane(toHold, Vector3.up) + Vector3.up * toHold.y;

        Camera cam = inputCamera != null ? inputCamera : Camera.main;
        Vector3 up = Vector3.up;
        Vector3 right = cam != null
            ? Vector3.Cross(hit.normal, up).normalized
            : Vector3.Cross(hit.normal, transform.up).normalized;
        if (right.sqrMagnitude < 0.01f)
        {
            right = transform.right;
        }

        Vector3 climbUp = Vector3.Cross(right, hit.normal).normalized;
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        Vector3 climbVelocity = climbUp * vertical * climbSpeed + right * horizontal * sidewaysSpeed;
        Vector3 stick = -hit.normal * wallStickForce;
        Vector3 correction = toHold / Time.fixedDeltaTime;
        correction = Vector3.ClampMagnitude(correction, climbSpeed * 2f);

        _body.useGravity = false;
        _body.linearVelocity = climbVelocity + stick * Time.fixedDeltaTime + correction * 0.35f;

        Vector3 face = -hit.normal;
        face.y = 0f;
        if (face.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(face.normalized, Vector3.up),
                12f * Time.fixedDeltaTime);
        }
    }

    void TryStartClimb()
    {
        if (!TryFindWall(out RaycastHit hit))
        {
            return;
        }

        if (swingController != null && swingController.IsSwinging)
        {
            swingController.Detach(false);
        }

        _isClimbing = true;
        _wallNormal = hit.normal;
        _body.useGravity = false;

        if (movementController != null)
        {
            movementController.SetControlLocked(true);
        }
    }

    public void StopClimb()
    {
        if (!_isClimbing)
        {
            return;
        }

        _isClimbing = false;
        _body.useGravity = true;

        if (movementController != null)
        {
            movementController.SetControlLocked(false);
        }
    }

    void JumpOffWall()
    {
        Vector3 away = _wallNormal.sqrMagnitude > 0.01f ? _wallNormal : -transform.forward;
        StopClimb();
        _body.linearVelocity = away.normalized * jumpOffAwayVelocity + Vector3.up * jumpOffUpVelocity;
    }

    bool TryFindWall(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = transform.forward;
        if (Physics.SphereCast(origin, wallCheckRadius, direction, out hit, wallCheckDistance, climbableMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        Camera cam = inputCamera != null ? inputCamera : Camera.main;
        if (cam != null)
        {
            Vector3 camFlat = cam.transform.forward;
            camFlat.y = 0f;
            if (camFlat.sqrMagnitude > 0.01f &&
                Physics.SphereCast(origin, wallCheckRadius, camFlat.normalized, out hit, wallCheckDistance, climbableMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }

        hit = default;
        return false;
    }

    void OnDisable()
    {
        if (_isClimbing)
        {
            _isClimbing = false;
            if (_body != null)
            {
                _body.useGravity = true;
            }
        }
    }
}
