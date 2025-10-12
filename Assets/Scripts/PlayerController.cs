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

    [Header("Game Over")]
    public GameOverManager gameOverManager;

    [Header("Bounds")]
    public bool useBounds = true;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Respawn Settings")]
    public Vector3 respawnPosition;

    [Header("State Tracking")]
    private int jumpCount = 0;
    private bool canDoubleJump = true;
    private bool isTouchingWall = false;
    private bool isLedgeDetected = false;
    private bool isFacingRight = true;

    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        respawnPosition = transform.position;
    }

    void Update()
    {
        if (gameOverManager != null && gameOverManager.isPaused)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        HandleMovement();
        GroundCheck();
        WallCheck();
        LedgeCheck();
        UpdateAnimations();

        if (useBounds)
        {
            float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
            float clampedY = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
        }
    }

    #region Movement
    private void HandleMovement()
    {
        bool canMove = !(anim.GetCurrentAnimatorStateInfo(0).IsName("PlayerRoll") ||
                         anim.GetCurrentAnimatorStateInfo(0).IsName("PlayerLongRoll") ||
                         anim.GetBool("IsDashing"));

        if (canMove)
            rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);

        if (moveInput.x > 0.1f) isFacingRight = true;
        else if (moveInput.x < -0.1f) isFacingRight = false;

        sprite.flipX = !isFacingRight;
    }
    #endregion

    #region Respawn
    public void SetCheckpoint(Vector3 newCheckpoint)
    {
        respawnPosition = newCheckpoint;
    }

    public void Respawn()
    {
        transform.position = respawnPosition;
        rb.velocity = Vector2.zero;
        jumpCount = 0;
        canDoubleJump = true;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        PlayerLight light = GetComponent<PlayerLight>();
        if (light != null)
            light.ResetLight();
    }
    #endregion

    #region Input Callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        if (gameOverManager != null && gameOverManager.isPaused) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (gameOverManager != null && gameOverManager.isPaused) return;
        if (!context.performed) return;

        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCount++;
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
        if (gameOverManager != null && gameOverManager.isPaused) return;
        if (!context.performed) return;

        int rollType = Keyboard.current.leftShiftKey.isPressed ? 2 : 1;
        anim.SetInteger("RollType", rollType);

        float rollVel = (rollType == 1) ? rollSpeed : longRollSpeed;
        rb.velocity = new Vector2((isFacingRight ? 1 : -1) * rollVel, rb.velocity.y);
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (gameOverManager != null && gameOverManager.isPaused) return;

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
        anim.SetBool("IsClimbing", anim.GetBool("IsClimbing"));
        anim.SetBool("IsPushing", anim.GetBool("IsPushing"));
        anim.SetBool("IsPulling", anim.GetBool("IsPulling"));
    }
    #endregion

    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
        Gizmos.DrawWireCube(center, size);
    }
    #endregion
}
