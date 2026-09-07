using System.Collections;
using UnityEngine;

public enum PlayerStance { Stand, Climb, Crouch, Glide }

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputManager input;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
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
    [SerializeField] private float glideSpeed;
    [SerializeField] private float airDrag;
    [SerializeField] private Vector3 glideRotationSpeed;
    [SerializeField] private float minGlideRotationX;
    [SerializeField] private float maxGlideRotationX;
    [SerializeField] private float resetComboInterval;
    [SerializeField] private Transform hitDetector;
    [SerializeField] private float hitDetectorRadius;
    [SerializeField] private LayerMask hitLayer;

    private Rigidbody rb;
    private float speed;
    private float rotationSmoothVelocity;
    private bool isGrounded;
    private PlayerStance playerStance;
    private Transform cameraTransform;
    private Animator anim;
    private CapsuleCollider playerCollider;
    private bool isPunching;
    private int combo;
    private Coroutine resetComboCo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();
        cameraTransform = Camera.main.transform;

        speed = walkSpeed;
        playerStance = PlayerStance.Stand;

        HideAndLockCursor();
    }

    private void Start()
    {
        input.OnMoveInput += Move;
        input.OnSprintInput += Sprint;
        input.OnJumpInput += Jump;
        input.OnClimbInput += StartClimb;
        input.OnCancelClimb += CancelClimb;
        cameraManager.OnChangePerspective += ChangePerspective;
        input.OnCrouchInput += Crouch;
        input.OnGlideInput += StartGlide;
        input.OnCancelGlide += CancelGlide;
        input.OnPunchInput += Punch;
    }

    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
        Glide();
    }

    private void OnDestroy()
    {
        input.OnMoveInput -= Move;
        input.OnSprintInput -= Sprint;
        input.OnJumpInput -= Jump;
        input.OnClimbInput -= StartClimb;
        input.OnCancelClimb -= CancelClimb;
        cameraManager.OnChangePerspective -= ChangePerspective;
        input.OnCrouchInput -= Crouch;
        input.OnGlideInput -= StartGlide;
        input.OnCancelGlide -= CancelGlide;
        input.OnPunchInput -= Punch;
    }

    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Move(Vector2 axisDir)
    {
        Vector3 moveDir = Vector3.zero;
        if ((playerStance == PlayerStance.Stand || playerStance == PlayerStance.Crouch) && !isPunching)
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
            Vector3 velocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            anim.SetFloat("velocity", velocity.magnitude * axisDir.magnitude);
            anim.SetFloat("velocityX", velocity.magnitude * axisDir.x);
            anim.SetFloat("velocityZ", velocity.magnitude * axisDir.y);
        }
        if (playerStance == PlayerStance.Climb)
        {
            Vector3 horizontal = axisDir.x * transform.right;
            Vector3 vertical = axisDir.y * transform.up;
            moveDir = horizontal + vertical;
            rb.AddForce(moveDir * speed * Time.deltaTime);
            rb.AddForce(moveDir * Time.deltaTime * climbSpeed);

            Vector3 velocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0);
            anim.SetFloat("climbVelocityX", velocity.magnitude * axisDir.x);
            anim.SetFloat("climbVelocityY", velocity.magnitude * axisDir.y);
        }
        if (playerStance == PlayerStance.Glide)
        {
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += glideRotationSpeed.x * axisDir.x * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, minGlideRotationX, maxGlideRotationX);
            rotationDegree.z += glideRotationSpeed.z * axisDir.x * Time.deltaTime;
            rotationDegree.y += glideRotationSpeed.y * axisDir.x * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationDegree);
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
        {
            rb.AddForce(jumpDir * jumpForce * Time.deltaTime);
            anim.SetTrigger("jump");
        }
    }

    private void CheckIsGrounded()
    {
        isGrounded = Physics.CheckSphere(groundDetector.position, detectorRadius, groundLayer);
        anim.SetBool("isGrounded", isGrounded);
        if (isGrounded)
            CancelGlide();
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
            playerCollider.center = Vector3.up * 1.3f;
            Vector3 offset = (transform.forward * climbOffset.z) + (Vector3.up * climbOffset.y);
            transform.position = hit.point - offset;
            playerStance = PlayerStance.Climb;
            rb.useGravity = false;
            speed = climbSpeed;
            cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            cameraManager.SetTPSFieldOfView(70f);
            anim.SetBool("isClimbing", true);
        }
    }

    private void CancelClimb()
    {
        if (playerStance == PlayerStance.Climb)
        {
            playerStance = PlayerStance.Stand;
            playerCollider.center = Vector3.up * .9f;
            rb.useGravity = true;
            transform.position -= transform.forward;
            speed = walkSpeed;
            cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            cameraManager.SetTPSFieldOfView(40f);
            anim.SetBool("isClimbing", false);
        }
    }

    private void ChangePerspective() => anim.SetTrigger("changePerspective");

    private void Crouch()
    {
        if (playerStance == PlayerStance.Stand)
        {
            playerStance = PlayerStance.Crouch;
            anim.SetBool("isCrouch", true);
            speed = crouchSpeed;
            playerCollider.height = 1.3f;
            playerCollider.center = Vector3.up * .66f;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            playerStance = PlayerStance.Stand;
            anim.SetBool("isCrouch", false);
            speed = walkSpeed;
            playerCollider.height = 1.8f;
            playerCollider.center = Vector3.up * .9f;
        }
    }

    private void Glide()
    {
        if (playerStance == PlayerStance.Glide)
        {
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 upForce = transform.up * (lift + airDrag);
            Vector3 forwardForce = transform.forward * glideSpeed;
            Vector3 totalForce = upForce + forwardForce;
            rb.AddForce(totalForce * Time.deltaTime);
        }
    }

    private void StartGlide()
    {
        if (playerStance != PlayerStance.Glide && !isGrounded)
        {
            playerStance = PlayerStance.Glide;
            anim.SetBool("isGliding", true);
            cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
        }
    }

    private void CancelGlide()
    {
        if (playerStance == PlayerStance.Glide)
        {
            playerStance = PlayerStance.Stand;
            anim.SetBool("isGliding", false);
            cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
        }
    }

    private void Punch()
    {
        if (!isPunching && playerStance == PlayerStance.Stand)
        {
            isPunching = true;
            if (combo < 3)
                combo += 1;
            else combo = 1;

            anim.SetInteger("combo", combo);
            anim.SetTrigger("punch");
        }
    }

    private void EndPunch() //for trigger animation
    {
        isPunching = false;
        if (resetComboCo != null)
            StopCoroutine(resetComboCo);
        resetComboCo = StartCoroutine(ResetCombo());
    }

    private IEnumerator ResetCombo()
    {
        yield return new WaitForSeconds(resetComboInterval);
        combo = 0;
    }

    private void Hit()
    {
        Collider[] hitObjects = Physics.OverlapSphere(hitDetector.position, hitDetectorRadius, hitLayer);
        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i].gameObject != null)
                Destroy(hitObjects[i].gameObject);
        }
    }
}
