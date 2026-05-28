using UnityEngine;

namespace MidnightLaundroCats
{
    public sealed class CatPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private bool cameraRelativeMovement = true;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Rigidbody body;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [SerializeField] private bool updateAnimatorSpeed;
        [SerializeField] private string animatorSpeedParameter = "Speed";

        private Vector3 moveDirection;
        private float verticalVelocity;
        private int animatorSpeedHash;

        private const float Gravity = -9.81f;
        private const float GroundedDownVelocity = -0.5f;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (cameraRelativeMovement && cameraTransform == null)
            {
                Debug.LogWarning($"{nameof(CatPlayerController)}: cameraRelativeMovement is enabled but no cameraTransform is assigned. Falling back to world-space movement.", this);
            }

            animatorSpeedHash = Animator.StringToHash(animatorSpeedParameter);
        }

        private void Update()
        {
            ReadMovementInput();

            if (body == null)
            {
                MoveKinematic(Time.deltaTime);
            }

            if (updateAnimatorSpeed && animator != null)
            {
                animator.SetFloat(animatorSpeedHash, moveDirection.sqrMagnitude);
            }
        }

        private void FixedUpdate()
        {
            if (body != null)
            {
                MoveRigidbody(Time.fixedDeltaTime);
            }
        }

        private void ReadMovementInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(horizontal, 0f, vertical);

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            moveDirection = ToWorldMoveDirection(input);
        }

        private Vector3 ToWorldMoveDirection(Vector3 input)
        {
            if (!cameraRelativeMovement || cameraTransform == null)
            {
                return input;
            }

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            return forward * input.z + right * input.x;
        }

        // Called every Update when no Rigidbody is present.
        // Handles CharacterController (with gravity) or plain transform movement.
        private void MoveKinematic(float deltaTime)
        {
            if (characterController != null)
            {
                MoveCharacterController(deltaTime);
                return;
            }

            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            RotateTowardMoveDirection(deltaTime);
            transform.position += moveDirection * (moveSpeed * deltaTime);
        }

        // Gravity must be applied every frame regardless of horizontal input,
        // or the CharacterController will not stay grounded on uneven surfaces.
        private void MoveCharacterController(float deltaTime)
        {
            if (characterController.isGrounded)
            {
                verticalVelocity = GroundedDownVelocity;
            }
            else
            {
                verticalVelocity += Gravity * deltaTime;
            }

            Vector3 horizontalMovement = Vector3.zero;
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                RotateTowardMoveDirection(deltaTime);
                horizontalMovement = moveDirection * (moveSpeed * deltaTime);
            }

            horizontalMovement.y = verticalVelocity * deltaTime;
            characterController.Move(horizontalMovement);
        }

        // Called every FixedUpdate when a Rigidbody is present.
        private void MoveRigidbody(float deltaTime)
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            RotateTowardMoveDirection(deltaTime);
            body.MovePosition(body.position + moveDirection * (moveSpeed * deltaTime));
        }

        private void RotateTowardMoveDirection(float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);

            if (body != null)
            {
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, rotationSpeed * deltaTime));
                return;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (characterController != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, characterController.radius);
            }
        }
    }
}
