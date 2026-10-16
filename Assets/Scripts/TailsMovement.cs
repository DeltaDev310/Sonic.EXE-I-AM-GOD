using UnityEngine;

public class TailsMovement : MonoBehaviour
{
    [Header("Running")]
    public float maxSpeed = 8f;          // top running speed
    public float acceleration = 30f;     // how fast he speeds up
    public float deceleration = 40f;     // how fast he slows down when you let go

    [Header("Jumping")]
    public float jumpForce = 12f;        // how strong the jump is
    public float jumpCutMultiplier = 0.5f; // let go of jump early = shorter jump

    [Header("Ground Check")]
    public Transform groundCheck;        // empty object placed at Tails' feet
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;        // set this to your "Ground" layer

    // --- private stuff ---
    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;
    private float airTime;
    private float inputX;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Read the player's input
        inputX = Input.GetAxisRaw("Horizontal"); // A/D or arrow keys

        // 2. Check if we are touching the ground
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 3. Count how long we've been in the air
        if (isGrounded) airTime = 0;
        else airTime += Time.deltaTime;

        // 4. Jump (only when on the ground)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // 5. Letting go of jump early makes the jump shorter
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // 6. Flip Tails to face the direction he's moving
        Vector3 scale = transform.localScale;
        if (inputX > 0) scale.x = Mathf.Abs(scale.x);   // face right
        if (inputX < 0) scale.x = -Mathf.Abs(scale.x);  // face left
        transform.localScale = scale;

        // 7. Tell the Animator how fast we're moving
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

        // 8. Tell the Animator if we're on the ground
        // Only play the jump animation after 0.1 seconds in the air
        anim.SetBool("IsGrounded", airTime < 0.1f);
    }

    void FixedUpdate()
    {
        Run();
    }

    // ---------- Our actions ----------

    void Run()
    {
        float targetSpeed = inputX * maxSpeed;

        // If pressing a direction, speed up. If not, slow down.
        float rate = (Mathf.Abs(inputX) > 0.01f) ? acceleration : deceleration;

        float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // Draws the ground check circle in the Scene view so you can see it
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}