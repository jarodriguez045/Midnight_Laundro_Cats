using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CalicoFollowerController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 followOffset = new Vector3(-1.4f, 0f, -1.8f);
    [SerializeField] float followSpeed = 4.2f;
    [SerializeField] float catchUpSpeed = 7f;
    [SerializeField] float stopDistance = 0.8f;
    [SerializeField] float catchUpDistance = 5f;
    [SerializeField] float rotationSpeed = 12f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundRayHeight = 2f;
    [SerializeField] float groundRayDistance = 5f;
    [SerializeField] float groundOffset = 0.05f;

    Rigidbody _body;

    public float DistanceToTarget
    {
        get
        {
            if (target == null)
            {
                return 0f;
            }

            Vector3 a = transform.position;
            Vector3 b = target.position;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.freezeRotation = true;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.TransformPoint(followOffset);
        Vector3 flatCurrent = transform.position;
        Vector3 flatDesired = desired;
        flatCurrent.y = 0f;
        flatDesired.y = 0f;

        float distance = Vector3.Distance(flatCurrent, flatDesired);
        Vector3 velocity = _body.linearVelocity;

        if (distance <= stopDistance)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, followSpeed * 2f * Time.fixedDeltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0f, followSpeed * 2f * Time.fixedDeltaTime);
        }
        else
        {
            float speed = distance >= catchUpDistance ? catchUpSpeed : followSpeed;
            Vector3 dir = (flatDesired - flatCurrent).normalized;
            Vector3 planar = dir * speed;
            velocity.x = planar.x;
            velocity.z = planar.z;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        Vector3 rayOrigin = transform.position + Vector3.up * groundRayHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            float targetY = groundHit.point.y + groundOffset;
            float yError = targetY - transform.position.y;
            velocity.y = yError / Time.fixedDeltaTime;
            velocity.y = Mathf.Clamp(velocity.y, -20f, 20f);
        }

        _body.linearVelocity = velocity;
    }
}
