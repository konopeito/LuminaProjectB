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

    [Header("Bounds")]
    public bool useBounds = true;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Respawn Settings")]
    public Vector3 respawnPosition;

    [Header("Climb Settings")]
    public float climbSpeed = 3f;
    public LayerMask ladderLayer;
    public Transform ladderCheck;
    public Transform ladderTopCheck; // Top of ladder
    public float ladderCheckRadius = 0.2f;
    public Vector3 nextStageSpawn; // assign later

    // --- State Tracking ---
    private int jumpCount = 0;
    private bool canDoubleJump = true;
    private bool isTouchingWall = false;
    private bool isLedgeDetected = false;
    private bool isFacingRight = true;
    private bool isDead = false;
    private bool isRolling = false;
    private bool isClimbing = false;
    private float climbInput = 0f;

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
        if (isDead || (GameMenusManager.Instance != null && GameMenusManager.Instance.isPaused && !GameMenusManager.Instance.IsGameOverActive()))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        GroundCheck();
        WallCheck();
        LedgeCheck();
        HandleMovement();
        UpdateAnimations();
        ApplyBounds();
    }

    #region Movement
    private void HandleMovement()
    {
        // --- Climbing overrides everything ---
        if (isClimbing)
        {
            rb.velocity = new Vector2(0, climbInput * climbSpeed);

            // Check if player reached the top of the ladder
            if (ladderTopCheck != null && transform.position.y >= ladderTopCheck.position.y)
            {
                transform.position = nextStageSpawn;
                isClimbing = false;
                anim.SetBool("IsClimbing", false);
                rb.gravityScale = 1f;
            }

            return;
        }

        // --- Rolling ---
        if (isRolling)
        {
            float rollVel = anim.GetInteger("RollType") == 1 ? rollSpeed : longRollSpeed;
            rb.velocity = new Vector2((isFacingRight ? 1 : -1) * rollVel, rb.velocity.y);

            if (Mathf.Abs(moveInput.x) < 0.1f)
            {
                isRolling = false;
                anim.SetInteger("RollType", 0);
            }
            return;
        }

        // --- Normal Movement ---
        rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);

        if (moveInput.x > 0.1f) isFacingRight = true;
        else if (moveInput.x < -0.1f) isFacingRight = false;

        sprite.flipX = !isFacingRight;
    }

    private void ApplyBounds()
    {
        if (!useBounds) return;
        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
    #endregion

    #region Respawn
    public void SetCheckpoint(Vector3 newCheckpoint) => respawnPosition = newCheckpoint;

    public void Respawn()
    {
        isDead = false;
        transform.position = respawnPosition;
        rb.velocity = Vector2.zero;
        jumpCount = 0;
        canDoubleJump = true;

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        GetComponent<PlayerLight>()?.ResetLight();
        PlayerHealthUI healthUI = GetComponent<PlayerLight>()?.healthUI;
        if (healthUI != null) healthUI.ResetHearts();

        enabled = true;
    }
    #endregion

    #region Input Callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || isClimbing) return;
        if (!context.performed) return;

        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCount++;
        }
        else if (canDoubleJump && jumpCount < maxJumps)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCount++;
            if (jumpCount >= maxJumps) canDoubleJump = false;
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || isClimbing) return;
        if (!context.performed) return;

        int rollType = Keyboard.current.leftShiftKey.isPressed ? 2 : 1;
        anim.SetInteger("RollType", rollType);
        anim.SetTrigger("Roll");
        isRolling = true;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || isClimbing) return;

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

    public void OnClimb(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true) return;

        // Use float (0 or 1) for key press instead of Vector2
        climbInput = context.ReadValue<float>();

        if (IsNearLadder() && climbInput > 0.1f) // pressing key
        {
            if (!isClimbing)
            {
                isClimbing = true;
                anim.SetBool("IsClimbing", true);

                // Snap player X to ladder
                Collider2D ladder = Physics2D.OverlapCircle(ladderCheck.position, ladderCheckRadius, ladderLayer);
                if (ladder != null)
                    transform.position = new Vector3(ladder.transform.position.x, transform.position.y, transform.position.z);

                rb.gravityScale = 0f;
            }
        }
        else
        {
            if (isClimbing)
            {
                isClimbing = false;
                anim.SetBool("IsClimbing", false);
                rb.gravityScale = 1f;
            }
        }
    }


    // --- ATTACKS ---
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || !context.performed) return;
        anim.SetTrigger("Attack");
    }

    public void OnAttack2(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || !context.performed) return;
        anim.SetTrigger("Attack2");
    }

    public void OnRangedAttack(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || !context.performed) return;
        anim.SetTrigger("RangedAttack");
    }

    public void OnJumpAttack(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || !context.performed) return;
        anim.SetTrigger("JumpAttack");
    }

    public void OnSpecialAttack(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || !context.performed) return;
        anim.SetTrigger("SpecialAttack");
    }

    public void OnPush(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || !context.performed) return;
        anim.SetBool("IsPushing", true);
    }

    public void OnPull(InputAction.CallbackContext context)
    {
        if (isDead || GameMenusManager.Instance?.isPaused == true || !context.performed) return;
        anim.SetBool("IsPulling", true);
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
        isTouchingWall = Physics2D.Raycast(transform.position, Vector2.right * (isFacingRight ? 1 : -1), wallCheckDistance, wallLayer);
    }

    private void LedgeCheck()
    {
        RaycastHit2D hit = Physics2D.Raycast(ledgeCheck.position, Vector2.right * (isFacingRight ? 1 : -1), ledgeCheckDistance, wallLayer);
        isLedgeDetected = hit.collider != null;
    }

    private bool IsNearLadder()
    {
        return Physics2D.OverlapCircle(ladderCheck.position, ladderCheckRadius, ladderLayer);
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
    }
    #endregion

    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

    public void OnDie()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        anim.SetTrigger("Death");
    }
}
