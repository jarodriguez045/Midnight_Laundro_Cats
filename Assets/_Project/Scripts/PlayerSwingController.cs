using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerSwingController : MonoBehaviour
{
    [SerializeField] Camera aimCamera;
    [SerializeField] PlayerMovementController movementController;
    [SerializeField] PlayerWallClimbController wallClimbController;
    [SerializeField] LayerMask swingMask;
    [SerializeField] float maxAttachDistance = 80f;
    [SerializeField] float minAttachHeightAbovePlayer = 1.5f;
    [SerializeField] float spring = 7.5f;
    [SerializeField] float damper = 3.5f;
    [SerializeField] float massScale = 4.5f;
    [SerializeField] float minDistanceMultiplier = 0.25f;
    [SerializeField] float maxDistanceMultiplier = 0.82f;
    [SerializeField] float steeringForce = 28f;
    [SerializeField] float forwardPumpForce = 16f;
    [SerializeField] float reelSpeed = 12f;
    [SerializeField] float minRopeLength = 6f;
    [SerializeField] float maxRopeLength = 90f;
    [SerializeField] float releaseForwardBoost = 3f;

    Rigidbody _body;
    LineRenderer _line;
    SpringJoint _joint;
    Transform _anchor;
    Vector3 _attachPoint;
    float _ropeLength;

    public bool IsSwinging => _joint != null;
    public Vector3 AttachPoint => _attachPoint;
    public float RopeLength => _ropeLength;

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (IsSwinging)
            {
                Detach(true);
            }
            else
            {
                TryAttach();
            }
        }

        if (!IsSwinging)
        {
            return;
        }

        float reel = 0f;
        if (Input.GetKey(KeyCode.Q))
        {
            reel = 1f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            reel = -1f;
        }

        if (Mathf.Abs(reel) > 0.01f)
        {
            _ropeLength = Mathf.Clamp(_ropeLength + reel * reelSpeed * Time.deltaTime, minRopeLength, maxRopeLength);
            _joint.maxDistance = _ropeLength;
            _joint.minDistance = Mathf.Max(0.1f, _ropeLength * minDistanceMultiplier);
        }

        UpdateLine();
    }

    void FixedUpdate()
    {
        if (!IsSwinging)
        {
            return;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 steer = right * input.x + forward * input.y;
        if (steer.sqrMagnitude > 1f)
        {
            steer.Normalize();
        }

        _body.AddForce(steer * steeringForce, ForceMode.Acceleration);

        if (input.y > 0.1f)
        {
            _body.AddForce(forward * forwardPumpForce * input.y, ForceMode.Acceleration);
        }
    }

    void TryAttach()
    {
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxAttachDistance, swingMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (hit.point.y < transform.position.y + minAttachHeightAbovePlayer)
        {
            return;
        }

        if (wallClimbController != null && wallClimbController.IsClimbing)
        {
            wallClimbController.StopClimb();
        }

        _attachPoint = hit.point;
        _ropeLength = Mathf.Clamp(Vector3.Distance(transform.position, _attachPoint), minRopeLength, maxRopeLength);

        GameObject anchorObject = new GameObject("SwingAnchor");
        anchorObject.transform.position = _attachPoint;
        _anchor = anchorObject.transform;

        _joint = gameObject.AddComponent<SpringJoint>();
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = _attachPoint;
        _joint.anchor = Vector3.zero;
        _joint.spring = spring;
        _joint.damper = damper;
        _joint.massScale = massScale;
        _joint.maxDistance = _ropeLength * maxDistanceMultiplier;
        _joint.minDistance = Mathf.Max(0.1f, _ropeLength * minDistanceMultiplier);

        if (movementController != null)
        {
            movementController.SetControlLocked(true);
        }

        _line.enabled = true;
        UpdateLine();
    }

    public void Detach(bool applyBoost)
    {
        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
        }

        if (_anchor != null)
        {
            Destroy(_anchor.gameObject);
            _anchor = null;
        }

        _line.enabled = false;

        if (movementController != null)
        {
            movementController.SetControlLocked(false);
            if (applyBoost)
            {
                Camera cam = aimCamera != null ? aimCamera : Camera.main;
                Vector3 boostDir = cam != null ? cam.transform.forward : transform.forward;
                movementController.AddVelocity(boostDir.normalized * releaseForwardBoost);
            }
        }
    }

    void UpdateLine()
    {
        if (_line == null || !_line.enabled)
        {
            return;
        }

        _line.SetPosition(0, transform.position + Vector3.up * 1.1f);
        _line.SetPosition(1, _attachPoint);
    }

    void OnDisable()
    {
        Detach(false);
    }
}
