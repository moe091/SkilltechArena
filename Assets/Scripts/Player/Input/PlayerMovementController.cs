using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float maxMoveSpeed = 8f;
    public float acceleration = 40f;
    public float deceleration = 60f;   // when target speed is 0
    public float airAcceleration = 30f; // optional: lower accel in air
    public float airDeceleration = 40f;

    [Header("Jumping")]
    public float jumpForce = 14f;
    public float gravity = -40f;          // manual gravity (negative)
    public float coyoteTime = 0.10f;      // jump allowed shortly after leaving ground
    public float jumpBufferTime = 0.10f;  // queue jump shortly before landing
    public bool variableJump = true;      // short hop if jump not held
    public float jumpCutMultiplier = 0.5f; // how strongly we cut jump when not held

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;         // place at the feet
    public float groundCheckRadius = 0.12f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;

    // Simulated state (kept explicit for prediction)
    private Vector2 velocity;
    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashTimer;
    private int facing = 1;               // -1 left, +1 right

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;            // we apply gravity manually for determinism
        rb.freezeRotation = true;
        velocity = rb.velocity;
    }

    /// <summary>
    /// Call from FixedUpdate with Time.fixedDeltaTime.
    /// </summary>
    public void ApplyMovement(PlayerInputData input, float deltaTime)
    {
        // --- Buffered inputs & timers ---
        if (input.jumpPressed)
            jumpBufferTimer = jumpBufferTime;

        // Grounded check BEFORE we change velocity
        UpdateGrounded();
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer = Mathf.Max(0f, coyoteTimer - deltaTime);

        // --- Dash (timeboxed, overrides horizontal speed while active) ---
        if (dashTimer <= 0f && input.dashPressed)
        {
            dashTimer = dashDuration;
            int dir = Mathf.Abs(input.horizontalInput) > 0.01f ? (input.horizontalInput > 0f ? 1 : -1) : facing;
            velocity.x = dir * dashSpeed;
        }

        if (dashTimer > 0f)
        {
            dashTimer -= deltaTime;
        }
        else
        {
            // --- Horizontal movement with accel/decel (deterministic) ---
            float targetSpeed = input.horizontalInput * maxMoveSpeed;
            bool hasInput = Mathf.Abs(targetSpeed) > 0.01f;

            float accel = isGrounded
                ? (hasInput ? acceleration : deceleration)
                : (hasInput ? airAcceleration : airDeceleration);

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * deltaTime);
        }

        // --- Jump: use buffer + coyote for feel, but deterministic ---
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = jumpForce;
            jumpBufferTimer = 0f; // consume
            coyoteTimer = 0f;
        }
        else
        {
            // Manual gravity
            velocity.y += gravity * deltaTime;

            // Variable jump height: cut upward velocity when jump no longer held
            if (variableJump && !input.jumpHeld && velocity.y > 0f)
                velocity.y += gravity * (1f - jumpCutMultiplier) * deltaTime; // stronger pull-down
        }

        // Prevent tiny downward accumulation while grounded
        if (isGrounded && velocity.y < 0f)
            velocity.y = 0f;

        // Apply
        rb.velocity = velocity;

        // --- Facing / sprite flip (visual only) ---
        if (Mathf.Abs(input.horizontalInput) > 0.01f)
        {
            facing = input.horizontalInput > 0f ? 1 : -1;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * facing;
            transform.localScale = s;
        }
    }

    private void UpdateGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            // Fallback: small circle at feet (object's position)
            isGrounded = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
        }
    }

    public void SetState(Vector2 position, Vector2 velocity)
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.position = position;
        rb.velocity = velocity;
        // if you cache velocity internally, update it here too
    }
}
