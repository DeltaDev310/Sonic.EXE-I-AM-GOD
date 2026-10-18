using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TailsMovement : MonoBehaviour
{
    [Header("Running")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 40f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
    [Tooltip("Can still jump this long after walking off a ledge.")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("A jump pressed this long before landing still counts.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation")]
    [Tooltip("Seconds in the air before the jump/fall animation plays.")]
    [SerializeField] private float airAnimDelay = 0.1f;

    // Animator parameter hashes (faster than using strings every frame)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private Rigidbody2D rb;
    private Animator anim;

    private bool isGrounded;
    private float inputX;
    private float airTime;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool jumpCutRequested;

    // Used by cutscenes to take control away from the player
    private bool controlEnabled = true;
    private float forcedInput;

    /// <summary>
    /// Turn player control on/off. While off, Tails ignores the keyboard and
    /// moves using forcedInput instead (-1 = left, 0 = stand still, 1 = right).
    /// </summary>
    public void SetControl(bool enabled, float forcedInput = 0f)
    {
        controlEnabled = enabled;
        this.forcedInput = forcedInput;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // optional: script still works without one

        if (groundCheck == null)
            Debug.LogWarning($"{name}: Ground Check is not assigned!", this);
    }

    private void Update()
    {
        ReadInput();
        UpdateTimers();
        UpdateFacing();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        CheckGround();
        Run();
        HandleJump();
    }

    // ---------- Update helpers ----------

    private void ReadInput()
    {
        if (!controlEnabled)
        {
            inputX = forcedInput;
            jumpBufferCounter = 0f;
            return;
        }

        inputX = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;

        if (Input.GetButtonUp("Jump"))
            jumpCutRequested = true;
    }

    private void UpdateTimers()
    {
        jumpBufferCounter -= Time.deltaTime;

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            airTime = 0f;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
            airTime += Time.deltaTime;
        }
    }

    private void UpdateFacing()
    {
        if (Mathf.Abs(inputX) < 0.01f) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(inputX);
        transform.localScale = scale;
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        anim.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool(IsGroundedHash, airTime < airAnimDelay);
    }

    // ---------- Physics actions ----------

    private void CheckGround()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Run()
    {
        float targetSpeed = inputX * maxSpeed;
        float rate = Mathf.Abs(inputX) > 0.01f ? acceleration : deceleration;

        float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        // Jump if the button was pressed recently AND we were on the ground recently
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;      // prevents jumping twice off one ground contact
            jumpCutRequested = false;
        }

        // Released jump early while still rising = shorter jump
        if (jumpCutRequested)
        {
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            jumpCutRequested = false;
        }
    }

    // Shows the ground check circle in the Scene view
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}