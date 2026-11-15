using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] float walkSpeed = 7f;
    [SerializeField] float runSpeed = 11f;
    [SerializeField] float groundAcceleration = 45f;
    [SerializeField] float airAcceleration = 14f;
    [SerializeField] float rotationSpeed = 16f;
    [SerializeField] float jumpVelocity = 8f;
    [SerializeField] float fallGravityMultiplier = 2.2f;
    [SerializeField] float groundedStickForce = 2f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundCheckRadius = 0.32f;
    [SerializeField] float groundCheckOffset = 0.08f;

    Rigidbody _body;
    CapsuleCollider _capsule;
    bool _controlLocked;
    bool _jumpRequested;
    bool _isGrounded;
    bool _useDebugMoveInput;
    Vector2 _debugMoveInput;

    public bool IsGrounded => _isGrounded;
    public bool IsRunning { get; private set; }
    public bool IsControlLocked => _controlLocked;
    public Vector3 PlanarVelocity
    {
        get
        {
            Vector3 v = _body != null ? _body.linearVelocity : Vector3.zero;
            v.y = 0f;
            return v;
        }
    }

    public float CurrentSpeed => PlanarVelocity.magnitude;

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _body.freezeRotation = true;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (_controlLocked)
        {
            return;
        }

        if (_isGrounded && Input.GetButtonDown("Jump"))
        {
            _jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        UpdateGrounded();

        if (_controlLocked)
        {
            return;
        }

        Vector2 input = ReadMoveInput();
        IsRunning = Input.GetKey(KeyCode.LeftShift) && input.sqrMagnitude > 0.01f;
        float targetSpeed = IsRunning ? runSpeed : walkSpeed;

        Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 camRight = cameraTransform != null ? cameraTransform.right : transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 desired = (camForward * input.y + camRight * input.x);
        if (desired.sqrMagnitude > 1f)
        {
            desired.Normalize();
        }

        desired *= targetSpeed;

        Vector3 velocity = _body.linearVelocity;
        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        float accel = _isGrounded ? groundAcceleration : airAcceleration;
        planar = Vector3.MoveTowards(planar, desired, accel * Time.fixedDeltaTime);

        velocity.x = planar.x;
        velocity.z = planar.z;

        if (_jumpRequested && _isGrounded)
        {
            velocity.y = jumpVelocity;
            _isGrounded = false;
        }

        _jumpRequested = false;

        if (!_isGrounded && velocity.y < 0f)
        {
            velocity.y += Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (_isGrounded && velocity.y <= 0f)
        {
            velocity.y = -groundedStickForce;
        }

        _body.linearVelocity = velocity;

        Vector3 look = planar.sqrMagnitude > 0.05f ? planar : desired;
        look.y = 0f;
        if (look.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(look.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    public void SetControlLocked(bool locked)
    {
        _controlLocked = locked;
        _jumpRequested = false;
    }

    public void SetPlanarVelocity(Vector3 planarVelocity)
    {
        Vector3 v = _body.linearVelocity;
        v.x = planarVelocity.x;
        v.z = planarVelocity.z;
        _body.linearVelocity = v;
    }

    public void AddVelocity(Vector3 impulse)
    {
        _body.linearVelocity += impulse;
    }

    public void SetVerticalVelocity(float y)
    {
        Vector3 v = _body.linearVelocity;
        v.y = y;
        _body.linearVelocity = v;
    }

    /// <summary>Test/CLI hook so Play Mode verifiers can drive move without OS input.</summary>
    public void SetDebugMoveInput(Vector2 input)
    {
        _useDebugMoveInput = true;
        _debugMoveInput = input;
    }

    public void ClearDebugMoveInput()
    {
        _useDebugMoveInput = false;
        _debugMoveInput = Vector2.zero;
    }

    void UpdateGrounded()
    {
        // Player capsule: height 2, center 0 → transform is mid-body; feet are ~1u below.
        // Never probe from transform.position upward — that sits above the capsule center
        // and misses the floor. Cast downward from the capsule center past the feet.
        Vector3 localCenter = _capsule != null ? _capsule.center : Vector3.zero;
        float capsuleRadius = _capsule != null ? _capsule.radius : 0.5f;
        float halfHeight = _capsule != null ? Mathf.Max(_capsule.height * 0.5f, capsuleRadius) : 1f;
        float probeRadius = Mathf.Min(groundCheckRadius, Mathf.Max(0.05f, capsuleRadius - 0.05f));

        Vector3 castOrigin = transform.TransformPoint(localCenter);
        // Distance from center to feet, minus probe radius, plus skin so we overlap the floor.
        float castDistance = halfHeight - probeRadius + groundCheckOffset;
        if (castDistance < groundCheckOffset)
        {
            castDistance = groundCheckOffset;
        }

        Vector3 down = -transform.up;
        if (Physics.SphereCast(
                castOrigin,
                probeRadius,
                down,
                out _,
                castDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            _isGrounded = true;
            return;
        }

        // Fallback: CheckSphere centered just above the feet (overlaps floor by offset).
        Vector3 feet = transform.TransformPoint(localCenter + Vector3.down * halfHeight);
        Vector3 feetProbe = feet + transform.up * probeRadius - transform.up * groundCheckOffset;
        _isGrounded = Physics.CheckSphere(feetProbe, probeRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    Vector2 ReadMoveInput()
    {
        if (_useDebugMoveInput)
        {
            return _debugMoveInput;
        }

        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
}
