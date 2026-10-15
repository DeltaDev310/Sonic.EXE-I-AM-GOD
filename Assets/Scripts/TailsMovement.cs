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
 
    [Header("Flying")]
    public float flyTime = 8f;           // seconds Tails can fly
    public float flapForce = 4f;         // little boost up each time you press jump
    public float flyGravity = 0.5f;      // gravity while flying (lower = floatier)
    public float maxFlyUpSpeed = 5f;     // so he doesn't shoot to the sky
 
    [Header("Ground Check")]
    public Transform groundCheck;        // empty object placed at Tails' feet
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;        // set this to your "Ground" layer
 
    // --- private stuff (you don't need to touch this) ---
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isFlying;
    private bool isTired;
    private float flyTimer;
    private float normalGravity;
    private float inputX;
 
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        normalGravity = rb.gravityScale; // remember the normal gravity
    }
 
    void Update()
    {
        // 1. Read the player's input
        inputX = Input.GetAxisRaw("Horizontal"); // A/D or arrow keys
 
        // 2. Check if we are touching the ground
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
 
        // 3. Landing resets everything about flying
        if (isGrounded)
        {
            StopFlying();
            isTired = false;
            flyTimer = flyTime;
        }
 
        // 4. Jump button pressed
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                Jump();
            }
            else if (!isFlying && !isTired)
            {
                StartFlying();          // pressing jump in the air = start flying!
            }
            else if (isFlying)
            {
                Flap();                 // pressing jump while flying = go up a bit
            }
        }
 
        // 5. Letting go of jump early makes the jump shorter
        if (Input.GetButtonUp("Jump") && !isFlying && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
 
        // 6. Count down the flying time
        if (isFlying)
        {
            flyTimer -= Time.deltaTime;
            if (flyTimer <= 0)
            {
                // Out of energy! Tails gets tired and falls.
                StopFlying();
                isTired = true;
            }
        }
 
        // 7. Turn the sprite left or right
        if (inputX > 0) transform.localScale = new Vector3(1, 1, 1);
        if (inputX < 0) transform.localScale = new Vector3(-1, 1, 1);
    }
 
    void FixedUpdate()
    {
        Run();
 
        // Don't let him go up too fast while flying
        if (isFlying && rb.linearVelocity.y > maxFlyUpSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFlyUpSpeed);
        }
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
 
    void StartFlying()
    {
        isFlying = true;
        rb.gravityScale = flyGravity;   // float more
        Flap();
    }
 
    void Flap()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, flapForce);
    }
 
    void StopFlying()
    {
        isFlying = false;
        rb.gravityScale = normalGravity; // back to normal gravity
    }
 
    // Draws the ground check circle in the Scene view so you can see it
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
