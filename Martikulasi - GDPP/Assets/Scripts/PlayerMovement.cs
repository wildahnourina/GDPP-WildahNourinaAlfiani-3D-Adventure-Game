using Unity.VisualScripting;
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

    private Rigidbody rb;
    private float speed;
    private float rotationSmoothVelocity;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        speed = walkSpeed;

        input.OnMoveInput += Move;
        input.OnSprintInput += Sprint;
        input.OnJumpInput += Jump;
    }

    private void Update()
    {
        CheckIsGrounded();
    }

    private void OnDestroy()
    {
        input.OnMoveInput -= Move;
        input.OnSprintInput -= Sprint;
        input.OnJumpInput -= Jump;
    }

    private void Move(Vector2 axisDir)
    {
        if (axisDir.magnitude >= 0.1)
        {
            float rotationAngle = Mathf.Atan2(axisDir.x, axisDir.y) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref rotationSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
            rb.AddForce(moveDir * speed * Time.deltaTime);
        }
    }

    private void Sprint (bool isSprint)
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


}
