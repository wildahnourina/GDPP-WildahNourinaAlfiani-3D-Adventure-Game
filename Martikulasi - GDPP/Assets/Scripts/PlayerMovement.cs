using UnityEngine;

public enum PlayerStance { Stand, Climb }

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputManager input;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float walkSprintTransition;
    [SerializeField] private float jumpForce;
    [SerializeField] private float rotationSmoothTime = .1f;
    [SerializeField] private Transform groundDetector;
    [SerializeField] private float detectorRadius;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector3 upperStepOffset;
    [SerializeField] private float stepCheckerDistance;
    [SerializeField] private float stepForce;
    [SerializeField] private Transform climbDetector;
    [SerializeField] private float climbCheckDistance;
    [SerializeField] private LayerMask climbableLayer;
    [SerializeField] private Vector3 climbOffset;
    [SerializeField] private float climbSpeed;
    [SerializeField] private CameraManager cameraManager;

    private Rigidbody rb;
    private float speed;
    private float rotationSmoothVelocity;
    private bool isGrounded;
    private PlayerStance playerStance;
    private Transform cameraTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        speed = walkSpeed;
        playerStance = PlayerStance.Stand;
        cameraTransform = Camera.main.transform;

        HideAndLockCursor();
    }

    private void Start()
    {
        input.OnMoveInput += Move;
        input.OnSprintInput += Sprint;
        input.OnJumpInput += Jump;
        input.OnClimbInput += StartClimb;
        input.OnCancelClimb += CancelClimb;
    }

    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
    }

    private void OnDestroy()
    {
        input.OnMoveInput -= Move;
        input.OnSprintInput -= Sprint;
        input.OnJumpInput -= Jump;
        input.OnClimbInput -= StartClimb;
        input.OnCancelClimb -= CancelClimb;
    }

    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Move(Vector2 axisDir)
    {
        Vector3 moveDir = Vector3.zero;
        if (playerStance == PlayerStance.Stand)
        {
            switch (cameraManager.cameraState)
            {
                case CameraState.ThirdPerson:
                    if (axisDir.magnitude >= 0.1)
                    {
                        float rotationAngle = Mathf.Atan2(axisDir.x, axisDir.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref rotationSmoothVelocity, rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        moveDir = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        rb.AddForce(moveDir * speed * Time.deltaTime);
                    }
                    break;
                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
                    Vector3 verticalDir = axisDir.y * transform.forward;
                    Vector3 horizontalDir = axisDir.x * transform.right;
                    moveDir = verticalDir + horizontalDir;
                    rb.AddForce(moveDir * speed * Time.deltaTime);
                    break;
                default:
                    break;
            }
        }
        if (playerStance == PlayerStance.Climb)
        {
            Vector3 horizontal = axisDir.x * transform.right;
            Vector3 vertical = axisDir.y * transform.up;
            moveDir = horizontal + vertical;
            rb.AddForce(moveDir * speed * Time.deltaTime);
        }
    }

    private void Sprint(bool isSprint)
    {
        if (isSprint)
        {
            if (speed < sprintSpeed)
                speed += walkSprintTransition * Time.deltaTime;
        }
        else
        {
            if (speed > walkSpeed)
                speed -= walkSprintTransition * Time.deltaTime;
        }
    }

    private void Jump()
    {
        Vector3 jumpDir = Vector3.up;
        if (isGrounded)
            rb.AddForce(jumpDir * jumpForce * Time.deltaTime);
    }

    private void CheckIsGrounded()
    {
        isGrounded = Physics.CheckSphere(groundDetector.position, detectorRadius, groundLayer);
    }

    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(groundDetector.position, transform.forward, stepCheckerDistance);
        bool isHitUpperStep = Physics.Raycast(groundDetector.position + upperStepOffset, transform.forward, stepCheckerDistance);

        if (isHitLowerStep && !isHitUpperStep)
            rb.AddForce(0, stepForce * Time.deltaTime, 0);
    }

    private void StartClimb()
    {
        bool isInFrontOfClimbingWall = Physics.Raycast(climbDetector.position, transform.forward, out RaycastHit hit, climbCheckDistance, climbableLayer);
        bool isNotClimbing = playerStance != PlayerStance.Climb;

        if (isInFrontOfClimbingWall && isGrounded && isNotClimbing)
        {
            Vector3 offset = (transform.forward * climbOffset.z) + (Vector3.up * climbOffset.y);
            transform.position = hit.point - offset;
            playerStance = PlayerStance.Climb;
            rb.useGravity = false;
            speed = climbSpeed;
        }
    }

    private void CancelClimb()
    {
        if (playerStance == PlayerStance.Climb)
        {
            playerStance = PlayerStance.Stand;
            rb.useGravity = true;
            transform.position -= transform.forward;
            speed = walkSpeed;
        }
    }
}
