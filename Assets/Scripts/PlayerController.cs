using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float dashForce = 15f;
    public float rollSpeed = 7f;
    public float longRollSpeed = 10f;
    public int maxJumps = 2;

    [Header("Components")]
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sprite;

    [Header("Ground & Environment")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Wall & Ledge")]
    public Transform wallCheck;
    public Transform ledgeCheck;
    public float wallCheckDistance = 0.3f;
    public float ledgeCheckDistance = 0.5f;
    public LayerMask wallLayer;

    [Header("State Tracking")]
    private int jumpCount = 0;
    private bool canDoubleJump = true;
    private bool isTouchingWall = false;
    private bool isLedgeDetected = false;
    private bool isFacingRight = true;

    private Vector2 moveInput;

    #region Input Callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCount++;
            // First jump animation is handled by VerticalVelocity + IsGrounded
        }
        else if (canDoubleJump && jumpCount < maxJumps)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            anim.SetTrigger("DoubleJump");
            jumpCount++;
            if (jumpCount >= maxJumps) canDoubleJump = false;
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        int rollType = 1; // Regular roll
        if (Keyboard.current.leftShiftKey.isPressed) rollType = 2; // Long roll
        anim.SetInteger("RollType", rollType);

        float rollVel = (rollType == 1) ? rollSpeed : longRollSpeed;
        rb.velocity = new Vector2((isFacingRight ? 1 : -1) * rollVel, rb.velocity.y);
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            anim.SetBool("IsDashing", true);
            rb.velocity = new Vector2((isFacingRight ? 1 : -1) * dashForce, rb.velocity.y);
        }
        else if (context.canceled)
        {
            anim.SetBool("IsDashing", false);
        }
    }

    public void OnAttack(InputAction.CallbackContext context) { if (context.performed) anim.SetTrigger("Attack"); }
    public void OnAttack2(InputAction.CallbackContext context) { if (context.performed) anim.SetTrigger("Attack2"); }
    public void OnRangedAttack(InputAction.CallbackContext context) { if (context.performed) anim.SetTrigger("RangedAttack"); }
    public void OnJumpAttack(InputAction.CallbackContext context) { if (context.performed) anim.SetTrigger("JumpAttack"); }
    public void OnSpecialAttack(InputAction.CallbackContext context) { if (context.performed) anim.SetTrigger("SpecialAttack"); }
    public void OnTakeDamage(InputAction.CallbackContext context) { if (context.performed) anim.SetTrigger("TakeDamage"); }
    public void OnDeath(InputAction.CallbackContext context) { if (context.performed) anim.SetTrigger("Death"); }
    public void OnClimb(InputAction.CallbackContext context) { anim.SetBool("IsClimbing", context.performed); }
    public void OnPush(InputAction.CallbackContext context) { anim.SetBool("IsPushing", context.performed); }
    public void OnPull(InputAction.CallbackContext context) { anim.SetBool("IsPulling", context.performed); }
    public void OnLedgeMove(InputAction.CallbackContext context)
    {
        if (context.performed) anim.SetFloat("LedgeMove", context.ReadValue<float>());
    }
    #endregion

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        HandleMovement();
        GroundCheck();
        WallCheck();
        LedgeCheck();
        UpdateAnimations();
    }

    #region Movement
    private void HandleMovement()
    {
        // Check if player is allowed to move
        bool canMove = !(anim.GetCurrentAnimatorStateInfo(0).IsName("PlayerRoll") ||
                         anim.GetCurrentAnimatorStateInfo(0).IsName("PlayerLongRoll") ||
                         anim.GetBool("IsDashing"));

        if (canMove)
            rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);

        // Sprite flip
        if (moveInput.x > 0.1f) isFacingRight = true;
        else if (moveInput.x < -0.1f) isFacingRight = false;

        sprite.flipX = !isFacingRight;
    }
    #endregion

    #region Environment Checks
    private void GroundCheck()
    {
        if (Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer))
        {
            jumpCount = 0;
            canDoubleJump = true;
        }
    }

    private void WallCheck()
    {
        isTouchingWall = Physics2D.Raycast(transform.position,
            Vector2.right * (isFacingRight ? 1 : -1),
            wallCheckDistance,
            wallLayer);
    }

    private void LedgeCheck()
    {
        RaycastHit2D hit = Physics2D.Raycast(ledgeCheck.position,
            Vector2.right * (isFacingRight ? 1 : -1),
            ledgeCheckDistance,
            wallLayer);

        isLedgeDetected = hit.collider != null;
    }
    #endregion

    #region Animations
    private void UpdateAnimations()
    {
        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        anim.SetFloat("VerticalVelocity", rb.velocity.y);
        anim.SetBool("IsGrounded", IsGrounded());
        anim.SetBool("IsWallSliding", !IsGrounded() && isTouchingWall && rb.velocity.y < 0);
        anim.SetBool("IsLedgeGrabbing", isLedgeDetected);
        anim.SetBool("IsClimbing", anim.GetBool("IsClimbing")); // set via ladder collisions
        anim.SetBool("IsPushing", anim.GetBool("IsPushing"));
        anim.SetBool("IsPulling", anim.GetBool("IsPulling"));
    }
    #endregion

    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
}
