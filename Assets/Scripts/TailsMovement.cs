
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TailsMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 40f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpCut = 0.5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBuffer = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator anim;

    private float moveInput;
    private float coyoteCounter;
    private float jumpCounter;

    private bool isGrounded;
    private bool jumpReleased;
    
    private bool controlEnabled = true;
    private float forcedInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }
    
    public void SetControl(bool enabled, float input = 0f)
    {
        controlEnabled = enabled;
        forcedInput = input;

        if (!enabled)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
    
    private void Update()
    {
        moveInput = controlEnabled ? Input.GetAxisRaw("Horizontal") : forcedInput;
        
        if (Input.GetButtonDown("Jump"))
            jumpCounter = jumpBuffer;
        else
            jumpCounter -= Time.deltaTime;

        if (Input.GetButtonUp("Jump"))
            jumpReleased = true;

        // Turn Tails around
        if (moveInput != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveInput);
            transform.localScale = scale;
        }

        // Update animations
        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            anim.SetBool("IsGrounded", isGrounded);
        }
    }

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );

        // Coyote time
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;

        // Movement
        float targetSpeed = moveInput * speed;
        float rate = moveInput != 0 ? acceleration : deceleration;

        float newSpeed = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);

        // Jump
        if (jumpCounter > 0 && coyoteCounter > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            jumpCounter = 0;
            coyoteCounter = 0;
        }

        // Variable jump height
        if (jumpReleased)
        {
            if (rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y * jumpCut
                );

            jumpReleased = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
