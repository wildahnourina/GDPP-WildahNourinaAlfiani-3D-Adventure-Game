
using UnityEngine;

public class PlayerMovement_zzz : MonoBehaviour
{
    private InputPlayerControls input;
    private Animator animator;

    [SerializeField] private float walkSpeed;
    private Rigidbody rb;
    private Vector3 movementDirection;

    public Vector2 moveInput { get; private set; }

    private void Awake()
    {
        input = new InputPlayerControls();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        ApplyMovement();
        AnimationControllers();
    }

    private void AnimationControllers()
    {
        float xVelocity = Vector3.Dot(movementDirection.normalized, transform.right);
        float zVelocity = Vector3.Dot(movementDirection.normalized, transform.forward);

        animator.SetFloat("xVelocity", xVelocity, .1f, Time.deltaTime);
        animator.SetFloat("zVelocity", zVelocity, .1f, Time.deltaTime);
    }

    private void ApplyMovement()
    {
        movementDirection = new Vector3(moveInput.x, 0, moveInput.y);

        if (movementDirection.magnitude > 0)
        {
            rb.AddForce(movementDirection *  walkSpeed);
        }
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Movement.performed += context => moveInput = context.ReadValue<Vector2>();
        input.Player.Movement.canceled += context => moveInput = Vector2.zero;

        input.Player.tes.performed += context => Debug.Log("TESSS");
    }

    private void OnDisable() => input.Disable();
}
