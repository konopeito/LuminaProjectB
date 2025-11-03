using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerController : MonoBehaviour
{
    #region Variables / Settings
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float dashForce = 15f;
    public float rollSpeed = 7f;
    public float longRollSpeed = 10f;
    public int maxJumps = 2;
    public float climbSpeed = 3f;

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
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Respawn Settings")]
    public Vector3 respawnPosition;

    [Header("Climb Settings")]
    public LayerMask ladderLayer;
    public Transform ladderCheck;
    public Transform ladderTopCheck;
    public float ladderCheckRadius = 0.2f;

    [Header("Inventory & Hotbar")]
    public PlayerInventory inventory;
    public HotbarUI hotbarUI;

    [Header("Zone Transition UI")]
    public CanvasGroup zoneTransitionUI;
    public TMP_Text zoneTypeText;
    public TMP_Text zoneNameText;
    public float zoneFadeTime = 1f;
    public float zoneDisplayTime = 1.5f;

    [Header("Item Buffs")]
    public bool shieldActive = false;
    public bool magnetActive = false;

    [Header("Zones")]
    public List<ZoneBounds> zones;

    [Header("Spawn Points")]
    public Transform spawnPointZone1;
    public Transform spawnPointZone2;
    public Transform spawnPointZone3;

    // --- State Tracking ---
    private int jumpCount = 0;
    private bool canDoubleJump = true;
    private bool isTouchingWall = false;
    private bool isLedgeDetected = false;
    private bool isFacingRight = true;
    private bool isDead = false;
    private bool isRolling = false;
    private bool isClimbing = false;
    private bool canMove = true;
    private bool isTransitioning = false;
    private float climbInput = 0f;
    private Vector2 moveInput;
    private Transform currentSpawnPoint;

    [System.Serializable]
    public struct ZoneBounds
    {
        public string zoneName;
        public Vector2 minBounds;
        public Vector2 maxBounds;
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (respawnPosition == Vector3.zero)
            respawnPosition = transform.position;

        transform.position = respawnPosition;

        rb.velocity = Vector2.zero;
        rb.gravityScale = 1f;
        isDead = false;
        isRolling = false;
        isClimbing = false;

        anim?.Rebind();
        anim?.Update(0f);

        GetComponent<PlayerLight>()?.ResetLight();
        if (GetComponent<PlayerLight>()?.healthUI != null)
            GetComponent<PlayerLight>().healthUI.ResetHearts();

        //useBounds = false;
        //StartCoroutine(ReenableBoundsNextFrame());
    }

    private void Update()
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
        //ApplyBounds();
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        // Climbing overrides everything
        if (isClimbing)
        {
            rb.velocity = new Vector2(0, climbInput * climbSpeed);

            if (ladderTopCheck != null && transform.position.y >= ladderTopCheck.position.y)
            {
                isClimbing = false;
                anim.SetBool("IsClimbing", false);
                rb.gravityScale = 1f;
                rb.velocity = Vector2.zero;

                canMove = false;
                StartCoroutine(HandleZoneTransition());
            }
            return;
        }

        // Rolling
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

        // Normal movement
        rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);

        if (moveInput.x > 0.1f) isFacingRight = true;
        else if (moveInput.x < -0.1f) isFacingRight = false;

        sprite.flipX = !isFacingRight;
    }

    private void ApplyBounds()
    {
        if (!useBounds || boundsTemporarilyDisabled) return;

        float x = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float y = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);
        transform.position = new Vector3(x, y, transform.position.z);
    }


    private IEnumerator ReenableBoundsNextFrame()
    {
        yield return null;
        useBounds = true;
    }

    private IEnumerator HandleZoneTransition()
    {
        isTransitioning = true;
        yield return TransitionToNextZone("Zone 2", "Zone2", "The Wastelands");
        canMove = true;
        isTransitioning = false;
    }
    #endregion

    #region Respawn / Death
    public void SetCheckpoint(Vector3 newCheckpoint) => respawnPosition = newCheckpoint;

    public void Respawn()
    {
        isDead = false;
        TeleportToSpawn(currentSpawnPoint);

        jumpCount = 0;
        canDoubleJump = true;
        rb.gravityScale = 1f;
        rb.velocity = Vector2.zero;

        anim?.Rebind();
        anim?.Update(0f);

        GetComponent<PlayerLight>()?.ResetLight();
        if (GetComponent<PlayerLight>()?.healthUI != null)
            GetComponent<PlayerLight>().healthUI.ResetHearts();

        canMove = true;
    }

    public void OnDie()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        anim?.SetTrigger("Death");
    }
    #endregion

    #region Input Callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        if (isDead || !canMove) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isDead || isClimbing) return;
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
        if (isDead || isClimbing) return;
        if (!context.performed) return;

        int rollType = Keyboard.current.leftShiftKey.isPressed ? 2 : 1;
        anim.SetInteger("RollType", rollType);
        anim.SetTrigger("Roll");
        isRolling = true;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (isDead || isClimbing) return;

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
        if (isDead) return;

        climbInput = context.ReadValue<float>();

        if (IsNearLadder() && climbInput > 0.1f)
        {
            if (!isClimbing)
            {
                isClimbing = true;
                anim.SetBool("IsClimbing", true);

                Collider2D ladder = Physics2D.OverlapCircle(ladderCheck.position, ladderCheckRadius, ladderLayer);
                if (ladder != null)
                    rb.position = new Vector3(ladder.transform.position.x, transform.position.y, transform.position.z);

                rb.gravityScale = 0f;
            }
        }
        else if (isClimbing)
        {
            isClimbing = false;
            anim.SetBool("IsClimbing", false);
            rb.gravityScale = 1f;
        }
    }

    // Attacks / Push / Pull
    public void OnAttack(InputAction.CallbackContext context) { if (isDead || !context.performed) return; anim.SetTrigger("Attack"); }
    public void OnAttack2(InputAction.CallbackContext context) { if (isDead || !context.performed) return; anim.SetTrigger("Attack2"); }
    public void OnRangedAttack(InputAction.CallbackContext context) { if (isDead || !context.performed) return; anim.SetTrigger("RangedAttack"); }
    public void OnJumpAttack(InputAction.CallbackContext context) { if (isDead || !context.performed) return; anim.SetTrigger("JumpAttack"); }
    public void OnSpecialAttack(InputAction.CallbackContext context) { if (isDead || !context.performed) return; anim.SetTrigger("SpecialAttack"); }
    public void OnPush(InputAction.CallbackContext context) { if (isDead || !context.performed) return; anim.SetBool("IsPushing", true); }
    public void OnPull(InputAction.CallbackContext context) { if (isDead || !context.performed) return; anim.SetBool("IsPulling", true); }
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

    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    #endregion

    #region Animations
    private void UpdateAnimations()
    {
        if (anim == null) return;

        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        anim.SetFloat("VerticalVelocity", rb.velocity.y);
        anim.SetBool("IsGrounded", IsGrounded());
        anim.SetBool("IsWallSliding", !IsGrounded() && isTouchingWall && rb.velocity.y < 0);
        anim.SetBool("IsLedgeGrabbing", isLedgeDetected);
    }
    #endregion

    #region Item Buffs
    public IEnumerator ApplySpeedBoost(float duration, float multiplier)
    {
        float originalSpeed = moveSpeed;
        moveSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
    }

    public IEnumerator ApplyDoubleJump(float duration)
    {
        int originalJumps = maxJumps;
        maxJumps = 999;
        yield return new WaitForSeconds(duration);
        maxJumps = originalJumps;
    }

    public IEnumerator ApplyShield(float duration)
    {
        shieldActive = true;
        yield return new WaitForSeconds(duration);
        shieldActive = false;
    }

    public IEnumerator ApplyOrbMagnet(float duration)
    {
        magnetActive = true;
        yield return new WaitForSeconds(duration);
        magnetActive = false;
    }

    public void OnPickup(InputAction.CallbackContext context)
    {
        if (!context.performed || isDead || isClimbing) return;
        TryPickupNearbyItem();
    }
    private void TryPickupNearbyItem()
    {
        if (inventory == null)
        {
            Debug.LogWarning("No PlayerInventory assigned!");
            return;
        }

        ItemPickup closestItem = FindClosestPickup();
        if (closestItem != null)
        {
            bool pickedUp = closestItem.TryPickup(inventory);
            if (pickedUp)
            {
                Debug.Log($"✅ Picked up {closestItem.item.itemName}");
                inventory.NotifyInventoryChanged();
            }
            else
            {
                Debug.Log("❌ Could not pick up item — maybe inventory full or too far.");
            }
        }
        else
        {
            Debug.Log("No item in pickup range.");
        }
    }

    private ItemPickup FindClosestPickup()
    {
        ItemPickup[] pickups = FindObjectsOfType<ItemPickup>();
        ItemPickup closest = null;
        float closestDist = Mathf.Infinity;

        foreach (var item in pickups)
        {
            float dist = Vector2.Distance(transform.position, item.transform.position);
            if (dist < closestDist && dist <= 2f) // pickup range
            {
                closest = item;
                closestDist = dist;
            }
        }

        return closest;
    }

    #endregion
    #region Zone Transition
    public IEnumerator TransitionToNextZone(string zoneTypeDisplay, string zoneKey, string zoneDisplayName)
    {
        DisableBoundsTemporarily();

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No main camera found for zone transition!");
            yield break;
        }

        Color originalColor = cam.backgroundColor;
        Color black = Color.black;
        float fadeDuration = 0.25f; // black fade in/out duration

        // 1️⃣ Fade to black
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cam.backgroundColor = Color.Lerp(originalColor, black, t / fadeDuration);
            yield return null;
        }
        cam.backgroundColor = black;

        // Tiny delay to ensure black frame renders
        yield return new WaitForSecondsRealtime(0.1f);

        // 2️⃣ Show zone UI if assigned
        if (zoneTransitionUI != null)
        {
            if (zoneTypeText != null) zoneTypeText.text = zoneTypeDisplay;
            if (zoneNameText != null) zoneNameText.text = zoneDisplayName;

            t = 0f;
            while (t < zoneFadeTime)
            {
                t += Time.deltaTime;
                zoneTransitionUI.alpha = Mathf.Lerp(0f, 1f, t / zoneFadeTime);
                yield return null;
            }

            zoneTransitionUI.alpha = 1f;
            yield return new WaitForSeconds(zoneDisplayTime);
        }

        // 3️⃣ Stop climbing / reset physics before moving
        StopClimbing();

        // 4️⃣ Move player to the new zone
        SetSpawnForZone(zoneKey);

        // 5️⃣ Fade out zone UI if assigned
        if (zoneTransitionUI != null)
        {
            t = 0f;
            while (t < zoneFadeTime)
            {
                t += Time.deltaTime;
                zoneTransitionUI.alpha = Mathf.Lerp(1f, 0f, t / zoneFadeTime);
                yield return null;
            }
            zoneTransitionUI.alpha = 0f;
        }

        // 6️⃣ Small delay for new zone to render
        yield return null;

        // 7️⃣ Fade back from black to original camera color
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cam.backgroundColor = Color.Lerp(black, originalColor, t / fadeDuration);
            yield return null;
        }
        cam.backgroundColor = originalColor;

        EnableBounds();
    }

    // === BOUNDS CONTROL ===
    private bool boundsTemporarilyDisabled = false;

    private void DisableBoundsTemporarily()
    {
        boundsTemporarilyDisabled = true;
    }

    private void EnableBounds()
    {
        boundsTemporarilyDisabled = false;
    }

    // --- Spawn Handling ---
    public void SetSpawnForZone(string zoneName)
    {
        string key = zoneName.Replace(" ", "");

        switch (key)
        {
            case "Zone1": currentSpawnPoint = spawnPointZone1; break;
            case "Zone2": currentSpawnPoint = spawnPointZone2; break;
            case "Zone3": currentSpawnPoint = spawnPointZone3; break;
            default: currentSpawnPoint = spawnPointZone1; break;
        }

        TeleportToSpawn(currentSpawnPoint);

        ZoneBounds zone = zones.Find(z => z.zoneName.Replace(" ", "") == key);
        if (!string.IsNullOrEmpty(zone.zoneName))
        {
            minBounds = zone.minBounds;
            maxBounds = zone.maxBounds;
        }

        respawnPosition = transform.position;
    }

    // --- Climbing / Teleport Helpers ---
    public bool IsClimbing() => isClimbing;

    public void StopClimbing()
    {
        isClimbing = false;
        anim?.SetBool("IsClimbing", false);
        rb.gravityScale = 1f;
        rb.velocity = Vector2.zero;
    }

    private void TeleportToSpawn(Transform spawn)
    {
        Vector3 targetPos = spawn != null ? spawn.position : respawnPosition;
        transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        rb.velocity = Vector2.zero;
        rb.gravityScale = 1f;
        isClimbing = false;
        isRolling = false;
        canMove = true;
    }

    public void SetBounds(Vector2 newMin, Vector2 newMax)
    {
        minBounds = newMin;
        maxBounds = newMax;
    }


    // ==========================
    // NPC Shop Interaction
    // ==========================
    [Header("NPC Shop Interaction")]
    private NPCShop currentShop; // Reference to the shop player is near

    // Called by Input System "Interact" action
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (currentShop != null)
            currentShop.Interact();
    }

    // Called by Input System "Shop Up" action
    public void OnShopUp(InputAction.CallbackContext context)
    {
        if (!context.performed || currentShop == null) return;
        currentShop.NavigateUp();
    }

    // Called by Input System "Shop Down" action
    public void OnShopDown(InputAction.CallbackContext context)
    {
        if (!context.performed || currentShop == null) return;
        currentShop.NavigateDown();
    }

    // Called by Input System "Shop Buy" action
    public void OnShopBuy(InputAction.CallbackContext context)
    {
        if (!context.performed || currentShop == null) return;
        currentShop.AttemptPurchase();
    }

    // Called by Input System "Shop Close" action
    public void OnShopClose(InputAction.CallbackContext context)
    {
        if (!context.performed || currentShop == null) return;
        currentShop.CloseShop();
    }

    // Detect when player enters/exits shop trigger
    private void OnTriggerEnter2D(Collider2D collision)
    {
        NPCShop shop = collision.GetComponent<NPCShop>();
        if (shop != null)
            currentShop = shop;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        NPCShop shop = collision.GetComponent<NPCShop>();
        if (shop != null && currentShop == shop)
            currentShop = null;
    }


}
#endregion


