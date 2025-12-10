using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using TMPro;

/// <summary>
/// Generic player controller for all playable characters.
/// Supports 2D combat with light/heavy attacks, charge mechanics, and knockback physics.
/// Each player can be configured for gamepad input using Input System Action Maps.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // Only allow one projectile throw per game
    private bool hasThrownProjectile = false;
    private int ignoreCollisionFrames = 10;
    private int ignoreCollisionCounter = 0;
    private PlayerInput playerInput;
    [SerializeField] private Animator _animator;
    [SerializeField] private int playerNumber = 1; // 1, 2, 3, or 4
    [SerializeField] private InputActionAsset inputActions; // Reference to PlayerControls.inputactions
    
    private PlayerControls playerControls; // Generated class from Input System
    
    public Animator GetAnimator() => _animator;
    public int GetPlayerNumber() => playerNumber;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 16f;
    public float airSpeed = 6f;
    public float gravity = 3f;
    public float fastFallMultiplier = 2f;
    public float acceleration = 15f;
    public float runAcceleration = 8f;
    public float deceleration = 20f;
    public float airAcceleration = 10f;
    public float attackMoveSpeedMultiplier = 0.5f;
    
    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;
    
    [Header("Attack")]
    public Transform lightAttackHitbox;
    public Transform heavyAttackHitbox;
    public float lightAttackKnockback = 8f;
    public float heavyAttackKnockback = 10f;
    public float chargedHeavyAttackKnockback = 20f;
    public float lightAttackCooldown = 1f;
    public float lightAttackDelay = 0f;
    public float heavyAttackCooldown = 0.6f;
    public float lightAttackKnockbackDelay = 0f;
    public float lightAttack2KnockbackDelay = 0f;
    public float heavyAttackKnockbackDelay = 0.15f;
    public float lightAttackComboWindow = 0.6f;
    public float lightAttackMoveDistance = 0.5f;
    public float regularHeavyAttackKnockback = 10f;
    public float lightAttackAnimationDuration = 0.5f; // Time for animation to complete
    public float heavyAttackAnimationDuration = 0.7f; // Time for animation to complete
    
    [Header("Setup")]
    public float groundDistance = 0.3f;
    
    [Header("Animation Tuning")]
    [Tooltip("Playback speed multiplier while in jump state before hold.")]
    public float jumpPlaySpeed = 0.85f;
    [Tooltip("Normalized time at which to start holding last frame while airborne (0..1).")]
    [Range(0.8f, 1f)] public float jumpHoldThreshold = 0.95f;
    
    // State
    private Rigidbody2D rb;
    private bool isGrounded;
    private int jumps;
    private int maxJumps = 2;
    private bool isDashing;
    private bool facingRight = true;
    private float dashTimer;
    private float dashCooldownTimer;
    private float lightAttackTimer;
    private float heavyAttackTimer;
    private float lightKnockbackDelayTimer;
    private float heavyKnockbackDelayTimer;
    private float lightAttackComboTimer;
    private int lightAttackComboCount;
    private bool hasHitCombo1;
    private bool hasHitCombo2;
    private float combo1DelayTimer;
    private float combo2DelayTimer;
    private float currentVelocityX;
    public float knockbackLockTimer; // Public so other players can set it
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float hitFlashTimer;
    private float hitFlashDuration = 0.1f; // Duration of each flash
    
    // Input actions (mapped per player without dynamic types)
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;
    private Transform respawnPoint;

    // Platform drop integration
    [Header("Platform Drop Settings")]
    [SerializeField] private float platformDropForce = -5f;
    [SerializeField] private float dropCooldown = 0.2f;
    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private float collisionReenableDelay = 0.5f;
    [SerializeField] private GameObject bigPlatform;
    private float dropCooldownTimer;
    private bool isDropping = false;
    private Collider2D currentDropPlatform;

    // Special: Projectile firing via simultaneous Light + Heavy
    [Header("Special Projectile")]
    [Tooltip("Prefab to spawn when Light+Heavy are pressed together.")]
    public GameObject projectilePrefab;
    [Tooltip("Optional fire point transform. If null, uses an offset from player.")]
    public Transform firePoint;
    [Tooltip("Horizontal speed of the projectile.")]
    public float projectileSpeed = 18f;
    [Tooltip("Seconds before projectile auto-destroys (fallback if prefab has no script).")]
    public float projectileLifetime = 3f;
    [Tooltip("Cooldown between projectile shots (prevent spamming).")]
    public float projectileCooldown = 0.5f;
    [Tooltip("Knockback applied by rocket on hit if projectile handles no logic.")]
    public float projectileKnockback = 12f;
    private float projectileCooldownTimer = 0f;

    // Inline player number label (no extra script needed)
    [Header("UI: Player Label")] public bool showPlayerLabel = true;
    [Tooltip("Base local offset above player center (used if dynamic height disabled). ")] public Vector3 playerLabelOffset = new Vector3(0f, 0.9f, 0f);
    [Tooltip("Extra vertical padding added above sprite when dynamic height enabled.")] public float playerLabelPadding = 0.15f;
    [Tooltip("Enable dynamic height so label stays just above current sprite bounds.")] public bool dynamicLabelHeight = true;
    [Tooltip("How quickly the label moves toward target height (higher = snappier). ")] public float playerLabelSmoothing = 10f;
    private TextMeshPro playerLabel;
    private TextMeshPro playerLabelShadow;

    // Hit / health tracking (invisible): after threshold triggers skyrocket knockback on next hit
    [Header("Health / Hit Tracking")]
    public int hitsTaken = 0;               // Accumulated hits on this player
    public int hitsThreshold = 20;          // After this many hits, next hit causes skyrocket knockback
    public float skyrocketVerticalMultiplier = 3f; // Upward force multiplier for skyrocket
    public float skyrocketHorizontalMultiplier = 1.2f; // Horizontal scaling during skyrocket
    public bool justSkyrocketed;            // True for one frame after special knockback (can drive animation)
    [Tooltip("Guarantee strong horizontal launch on skyrocket: base horizontal scale.")]
    public float skyrocketHorizontalMultiplierBoost = 2.5f; // Additional boost factor applied AFTER regular multiplier
    [Tooltip("Minimum absolute horizontal velocity applied during skyrocket (override if lower).")]
    public float skyrocketMinHorizontalSpeed = 20f;

    [Header("Respawn")]
    public bool enableRespawn = true;
    public float respawnYThreshold = -12f; // If player falls below this, respawn
    public float respawnInvulnTime = 1.5f; // Seconds of invulnerability after respawn
    public float respawnDelay = 2f; // Delay before repositioning on death/kill zone
    public int maxRespawns = 3; // Maximum number of times this player can respawn (not counting initial spawn)
    public int respawnsUsed = 0; // How many respawns have been consumed
    public bool isEliminated = false; // True once out of lives
    private Vector3 spawnPosition;
    private bool isInvulnerable;
    private float invulnTimer;
    private bool isRespawning; // True during delay window
    private bool initialFacingRight; // Stores default facing established at Start
    // Jump animation hold tracking
    private bool jumpHoldApplied = false;

    // Controller gating
    [Header("Controller Activation")] public bool requireController = true; // If true, player only active with a paired gamepad (except fallback rules)
    [Tooltip("Allow Player 1 to stay active using keyboard if no controller is connected.")] public bool allowPlayer1KeyboardFallback = true;
    private bool hasGamepad; // Set true when a pad is paired
    private InputUser inputUser; // Stored for deferred pairing
    private static readonly System.Collections.Generic.List<PlayerController> allPlayers = new System.Collections.Generic.List<PlayerController>();
    public static bool globalGameplayEnabled = true; // Set false by a countdown manager until game start

    // Ensure InputUser exists before OnEnable triggers pairing logic
    void Awake()
    {
        // No manual PlayerControls or InputUser setup needed; PlayerInput handles this
    }
    
    void Start()
    {
        ignoreCollisionCounter = 0;
        playerInput = GetComponent<PlayerInput>();
        // --- Player Label Creation ---
        if (showPlayerLabel)
        {
            // Try to find existing label in children
            playerLabel = GetComponentInChildren<TextMeshPro>();
            if (playerLabel == null)
            {
                // Create label GameObject
                GameObject labelObj = new GameObject("PlayerLabel");
                labelObj.transform.SetParent(transform);
                labelObj.transform.localPosition = playerLabelOffset;
                playerLabel = labelObj.AddComponent<TextMeshPro>();
                playerLabel.alignment = TextAlignmentOptions.Center;
                playerLabel.fontSize = 4.5f;
                playerLabel.color = GetColorForPlayer(playerInput != null ? playerInput.playerIndex + 1 : playerNumber);
                playerLabel.text = playerInput != null ? $"P{playerInput.playerIndex + 1}" : $"P{playerNumber}";
                playerLabel.sortingOrder = 100;
            }
            // Create shadow for readability
            if (playerLabelShadow == null)
            {
                GameObject shadowObj = new GameObject("PlayerLabelShadow");
                shadowObj.transform.SetParent(playerLabel.transform.parent);
                shadowObj.transform.localPosition = playerLabel.transform.localPosition + new Vector3(0.03f, -0.03f, 0f);
                playerLabelShadow = shadowObj.AddComponent<TextMeshPro>();
                playerLabelShadow.alignment = TextAlignmentOptions.Center;
                playerLabelShadow.fontSize = playerLabel.fontSize;
                playerLabelShadow.color = new Color(0,0,0,0.7f);
                playerLabelShadow.text = playerLabel.text;
                playerLabelShadow.sortingOrder = 99;
                // Ensure shadow is rendered behind
                shadowObj.transform.SetSiblingIndex(0);
            }
        }
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravity;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            Debug.Log($"[Player {playerNumber}] Animator auto-assigned from GetComponent.");
        }
        else
        {
            Debug.Log($"[Player {playerNumber}] Animator already assigned in Inspector.");
        }
        // Update label text and color if label exists (after playerInput is set)
        if (showPlayerLabel && playerLabel != null && playerInput != null)
        {
            playerLabel.text = $"P{playerInput.playerIndex + 1}";
            playerLabel.color = GetColorForPlayer(playerInput.playerIndex + 1);
            if (playerLabelShadow != null)
                playerLabelShadow.text = playerLabel.text;
        }
        // Use PlayerInput's assigned actions (action map is set by PlayerInput.Instantiate)
        InputActionMap map = null;
        if (playerInput != null)
        {
            map = playerInput.currentActionMap;
            if (map == null)
            {
                // Set the correct action map based on player index
                string mapName = $"Player{playerInput.playerIndex + 1}Controls";
                map = playerInput.actions.FindActionMap(mapName, true);
                if (map != null)
                {
                    playerInput.SwitchCurrentActionMap(mapName);
                    Debug.Log($"[PlayerController] Switched to action map: {mapName}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerController] Could not find action map: {mapName}");
                }
            }
        }
        if (map != null)
        {
            moveAction = map.FindAction("Move");
            jumpAction = map.FindAction("Jump");
            dashAction = map.FindAction("Dash");
            lightAttackAction = map.FindAction("LightAttack");
            heavyAttackAction = map.FindAction("HeavyAttack");
            map.Enable();
            Debug.Log($"[PlayerController] Using action map: {map.name}");
        }
        else
        {
            Debug.LogWarning("[PlayerController] No action map found for this player!");
        }
        // Cache initial spawn position
        spawnPosition = transform.position;
        Debug.Log($"[Player {playerNumber}] Input System initialized; using action map: {(map != null ? map.name : "none")}");
        // Ignore collisions with other players' box colliders so players don't bump into each other
        BoxCollider2D myBox = GetComponent<BoxCollider2D>();
        if (myBox != null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                if (player == this.gameObject) continue;
                BoxCollider2D otherBox = player.GetComponent<BoxCollider2D>();
                if (otherBox != null)
                {
                    Physics2D.IgnoreCollision(myBox, otherBox);
                }
            }
        }
        // Default facing: Players 2 and 4 start facing left
        if (playerInput != null && (playerInput.playerIndex == 1 || playerInput.playerIndex == 3)) // 0-based index
        {
            if (facingRight) // Flip only if currently facing right
            {
                FlipDirection();
            }
        }
        // Cache the default facing after any initial flips
        initialFacingRight = facingRight;
    // End of Start()
    }
    
    void Update()
    {
        // Robustly ignore collisions between all non-trigger colliders of all players
        if (ignoreCollisionCounter < ignoreCollisionFrames)
        {
            var myColliders = GetComponents<Collider2D>();
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                if (player == this.gameObject) continue;
                var otherColliders = player.GetComponents<Collider2D>();
                foreach (var myCol in myColliders)
                {
                    if (myCol.isTrigger) continue;
                    foreach (var otherCol in otherColliders)
                    {
                        if (otherCol.isTrigger) continue;
                        Physics2D.IgnoreCollision(myCol, otherCol);
                    }
                }
            }
            ignoreCollisionCounter++;
        }

        CheckGround();
        UpdateTimers();

        // Platform drop cooldown timer
        if (dropCooldownTimer > 0)
            dropCooldownTimer -= Time.deltaTime;

        // If currently in a respawn delay, skip gameplay processing
        if (isRespawning)
        {
            return;
        }

        // Stop all processing if eliminated
        if (isEliminated)
        {
            return;
        }

        // Global gameplay gate (e.g., countdown). Allow animations (blink, hit) but block input-driven actions.
        if (!globalGameplayEnabled)
        {
            UpdateAnimations();
            return;
        }

        // Fire projectile when Light + Heavy are pressed simultaneously (one-shot with cooldown)
        if (!hasThrownProjectile && projectilePrefab != null && projectileCooldownTimer <= 0f &&
            ((lightAttackAction != null && lightAttackAction.IsPressed()) || (playerNumber == 1 && Keyboard.current != null && (Keyboard.current.jKey.isPressed || Keyboard.current.zKey.isPressed))) &&
            ((heavyAttackAction != null && heavyAttackAction.IsPressed()) || (playerNumber == 1 && Keyboard.current != null && (Keyboard.current.kKey.isPressed || Keyboard.current.xKey.isPressed))))
        {
            SpawnProjectile();
            projectileCooldownTimer = projectileCooldown;
            hasThrownProjectile = true;
        }

        // Platform drop: only Down button (keyboard S or gamepad stick down)
        if (CanDropPlatform() && IsDownPressed())
        {
            DropThroughPlatform();
        }

        // Reflect dash input only when cooldown finished and not currently dashing
        if (_animator != null)
        {
            bool dashHeld = (dashAction != null && dashAction.IsPressed());
            if (!isDashing)
            {
                bool canDashNow = dashCooldownTimer <= 0f;
                _animator.SetBool("IsDash", dashHeld && canDashNow);
            }
        }

        if (requireController && !hasGamepad && !(playerNumber == 1 && allowPlayer1KeyboardFallback))
        {
            // Should not occur now (object would have been destroyed). Guard anyway.
            return;
        }

        // Only allow dash logic while dashing
        if (isDashing)
        {
            var dashVel = new Vector2(facingRight ? dashSpeed : -dashSpeed, rb.linearVelocity.y);
            rb.linearVelocity = dashVel;
            Debug.Log($"[Player {playerNumber}] Dash velocity set to: {dashVel}, Rigidbody2D velocity: {rb.linearVelocity}");
            if (_animator != null)
            {
                _animator.SetBool("IsDash", true);
            }
            // Skip all other movement, jump, and attack logic while dashing
            return;
        }
        Move();
        Jump();
        Dash();
        Attack();

        UpdateAnimations();

        // Out-of-bounds respawn check
        if (enableRespawn && transform.position.y < respawnYThreshold)
        {
            HandleDeath();
        }
    }

    bool IsDownPressed()
    {
        // Gamepad: left stick down
        if (moveAction != null)
        {
            Vector2 moveValue = moveAction.ReadValue<Vector2>();
            if (moveValue.y < -0.7f) return true;
        }
        // Keyboard: S key
        if (playerNumber == 1 && Keyboard.current != null)
        {
            if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                return true;
        }
        return false;
    }

    bool CanDropPlatform()
    {
        if (dropCooldownTimer > 0) return false;
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;
        Vector2 checkPos = new Vector2(transform.position.x, col.bounds.min.y - 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, 1f, platformLayerMask);
        if (hit.collider != null)
        {
            if (bigPlatform != null && hit.collider.gameObject == bigPlatform)
                return false;
            return true;
        }
        return false;
    }

    void DropThroughPlatform()
    {
        Collider2D col = GetComponent<Collider2D>();
        Vector2 checkPos = new Vector2(transform.position.x, col.bounds.min.y - 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, 1f, platformLayerMask);
        if (hit.collider != null && (bigPlatform == null || hit.collider.gameObject != bigPlatform))
        {
            currentDropPlatform = hit.collider;
            Physics2D.IgnoreCollision(col, hit.collider, true);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, platformDropForce);
            Invoke(nameof(ReenableCollision), collisionReenableDelay);
            dropCooldownTimer = dropCooldown;
            isDropping = true;
        }
    }

    void ReenableCollision()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (currentDropPlatform != null && col != null)
        {
            Physics2D.IgnoreCollision(col, currentDropPlatform, false);
        }
        isDropping = false;
        currentDropPlatform = null;
    }
    bool IsDropping() => isDropping;
    
    void CheckGround()
    {
        bool wasGrounded = isGrounded;
        
        // Get the collider bounds
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Check slightly below the collider's bottom edge
            Vector2 checkPos = new Vector2(transform.position.x, col.bounds.min.y - 0.1f);
            
            // Raycast down to check for ground
            RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, groundDistance, ~LayerMask.GetMask("Player"));
            isGrounded = hit.collider != null;
        }
        else
        {
            // Fallback: check with overlap circle
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.7f, ~LayerMask.GetMask("Player"));
            isGrounded = hit.collider != null;
        }
        
        if (isGrounded && !wasGrounded)
        {
            jumps = 0;
            _animator.SetBool("IsJump", false);
        }
    }
    
    void UpdateTimers()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                if (_animator != null)
                {
                    _animator.SetBool("IsDash", false);
                }
            }
        }
        
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (invulnTimer > 0f)
        {
            invulnTimer -= Time.deltaTime;
            if (invulnTimer <= 0f)
            {
                isInvulnerable = false;
            }
        }
        if (lightAttackTimer > 0) lightAttackTimer -= Time.deltaTime;
        if (heavyAttackTimer > 0) heavyAttackTimer -= Time.deltaTime;
        if (lightKnockbackDelayTimer > 0) lightKnockbackDelayTimer -= Time.deltaTime;
        if (heavyKnockbackDelayTimer > 0) heavyKnockbackDelayTimer -= Time.deltaTime;
        if (lightAttackComboTimer > 0) lightAttackComboTimer -= Time.deltaTime;
        if (combo1DelayTimer > 0) combo1DelayTimer -= Time.deltaTime;
        if (combo2DelayTimer > 0) combo2DelayTimer -= Time.deltaTime;
        if (knockbackLockTimer > 0) knockbackLockTimer -= Time.deltaTime;
        if (projectileCooldownTimer > 0f) projectileCooldownTimer -= Time.deltaTime;
        
        // Reset combo if window expires
        if (lightAttackComboTimer <= 0)
        {
            lightAttackComboCount = 0;
        }
    }
    
    void Move()
    {
        // Prevent movement from overriding knockback - complete lockout
        if (knockbackLockTimer > 0)
        {
            // Don't apply any movement input, just let physics take over
            return;
        }
        
        // Prevent movement during light or heavy attacks (check animator bools)
        // But allow movement if doing IsLightForward (running attack)
        if (_animator != null)
        {
            bool isLightForward = _animator.GetBool("IsLightForward");
            bool isLightAttack = _animator.GetBool("IsLightAttack");
            bool isLightAttack2 = _animator.GetBool("IsLightAttack2");
            bool isHeavyAttack = _animator.GetBool("IsHeavyAttack");
            
            // Lock movement if attacking, UNLESS it's IsLightForward
            if ((isLightAttack || isLightAttack2 || isHeavyAttack) && !isLightForward)
            {
                currentVelocityX = 0;
                rb.linearVelocity = new Vector2(currentVelocityX, rb.linearVelocity.y);
                return;
            }
        }
        
        float moveInput = GetMoveInput();
        
        // Flip sprite only if not attacking
        if (_animator != null)
        {
            bool attackingNotFlip = _animator.GetBool("IsLightAttack") || _animator.GetBool("IsLightAttack2") || _animator.GetBool("IsLightForward") || _animator.GetBool("IsHeavyAttack");
            
            if (!attackingNotFlip)
            {
                if (moveInput > 0 && !facingRight) FlipDirection();
                else if (moveInput < 0 && facingRight) FlipDirection();
            }
        }
        
        // Reduce speed if attacking
        float currentMoveSpeed = moveSpeed;
        if (lightAttackTimer > 0)
        {
            currentMoveSpeed = moveSpeed * attackMoveSpeedMultiplier;
        }
        
        // Further reduce speed during IsLightForward
        if (_animator != null && _animator.GetBool("IsLightForward"))
        {
            currentMoveSpeed = moveSpeed * 0.5f; // 50% speed during forward light attack
        }
        
        // Apply movement with acceleration
        if (isGrounded)
        {
            currentVelocityX = Mathf.Lerp(currentVelocityX, moveInput * currentMoveSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentVelocityX = Mathf.Lerp(currentVelocityX, moveInput * airSpeed, airAcceleration * Time.deltaTime);
        }
        
        rb.linearVelocity = new Vector2(currentVelocityX, rb.linearVelocity.y);
    }
    
    void Jump()
    {
        if (GetJumpInput())
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumps = 1; // First jump used
                _animator.SetBool("IsJump", true);
                // Prepare jump animation playback & reset hold state
                jumpHoldApplied = false;
                _animator.speed = Mathf.Max(0.01f, jumpPlaySpeed);
            }
            else if (jumps < maxJumps)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumps++;
                // Restart the jump animation by toggling it off and back on with a delay
                StartCoroutine(RestartJumpAnimation());
                // Reset hold state for in-air jump
                jumpHoldApplied = false;
                _animator.speed = Mathf.Max(0.01f, jumpPlaySpeed);
            }
        }
    }
    
    IEnumerator RestartJumpAnimation()
    {
        _animator.SetBool("IsJump", false);
        yield return null; // Wait one frame
        _animator.SetBool("IsJump", true);
    }
    
    void Dash()
    {
        if (GetDashInput() && dashCooldownTimer <= 0)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            if (_animator != null)
            {
                _animator.SetBool("IsDash", true);
            }
        }
    }

    // Spawn a projectile in the facing direction
    void SpawnProjectile()
    {
        if (projectilePrefab == null) return;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + new Vector3(facingRight ? 0.6f : -0.6f, 0.35f, 0f);
        var go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        float dir = facingRight ? 1f : -1f;
        var rbProj = go.GetComponent<Rigidbody2D>();
        if (rbProj != null)
        {
            rbProj.linearVelocity = new Vector2(dir * projectileSpeed, 0f);
            rbProj.gravityScale = 0f; // ensure straight horizontal motion
        }
        // Flip projectile sprite to match facing
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Assume sprite faces right by default; flip when facing left
            sr.flipX = !facingRight;
        }
        else
        {
            // Fallback: flip local scale X
            Vector3 ls = go.transform.localScale;
            ls.x = Mathf.Abs(ls.x) * (facingRight ? 1f : -1f);
            go.transform.localScale = ls;
        }

        // Also flip any child SpriteRenderers (common for multi-sprite prefabs)
        var childRenderers = go.GetComponentsInChildren<SpriteRenderer>();
        foreach (var cr in childRenderers)
        {
            cr.flipX = !facingRight;
        }

        // If a RocketProjectile component exists, initialize it for consistent behavior
        var rocket = go.GetComponent<RocketProjectile>();
        if (rocket != null)
        {
            rocket.Initialize(this, dir, projectileSpeed, projectileKnockback, projectileLifetime);
        }
        // Fallback lifetime if projectile script is absent
        Destroy(go, projectileLifetime);
    }
    
    void Attack()
    {
        // Light Attack - Check if moving (A or D held) and H pressed
        bool isMoving = Mathf.Abs(GetMoveInput()) > 0.1f;
        
        if (GetLightAttackInput() && lightAttackTimer <= 0)
        {
            lightAttackComboCount++;
            
            // Reset combo if outside window
            if (lightAttackComboTimer <= 0)
            {
                lightAttackComboCount = 1;
            }
            
            // Only allow 2 hits in combo
            if (lightAttackComboCount <= 2)
            {
                Debug.Log($"[Player {playerNumber}] Light attack {lightAttackComboCount} triggered");
                lightAttackTimer = lightAttackCooldown;
                lightAttackComboTimer = lightAttackComboWindow;
                
                // Set delay timer for this combo hit
                if (lightAttackComboCount == 1)
                {
                    combo1DelayTimer = lightAttackKnockbackDelay;
                }
                else if (lightAttackComboCount == 2)
                {
                    combo2DelayTimer = lightAttackKnockbackDelay;
                }
                
                // Trigger light forward animation if moving, otherwise light attack
                if (_animator != null)
                {
                    if (lightAttackComboCount == 1)
                    {
                        if (isMoving)
                        {
                            Debug.Log($"[Player {playerNumber}] Setting IsLightForward to true");
                            _animator.SetBool("IsLightForward", true);
                            _animator.SetBool("IsLightAttack", false);
                        }
                        else
                        {
                            Debug.Log($"[Player {playerNumber}] Setting IsLightAttack to true");
                            _animator.SetBool("IsLightAttack", true);
                            _animator.SetBool("IsLightForward", false);
                        }
                    }
                    else if (lightAttackComboCount == 2)
                    {
                        Debug.Log($"[Player {playerNumber}] Setting IsLightAttack2 to true");
                        _animator.SetBool("IsLightAttack2", true);
                        _animator.SetBool("IsLightForward", false);
                    }
                }
                else
                {
                    Debug.LogError($"[Player {playerNumber}] Animator is NULL!");
                }
            }
        }
        
        // Apply light attack knockback after delay
        if (combo1DelayTimer <= 0 && lightAttackTimer > 0 && combo1DelayTimer > -0.5f && !hasHitCombo1)
        {
            PerformAttack(lightAttackHitbox, lightAttackKnockback);
            hasHitCombo1 = true;
            combo1DelayTimer = -1; // Prevent repeated hits
        }
        
        if (combo2DelayTimer <= 0 && lightAttackTimer > 0 && combo2DelayTimer > -0.5f && !hasHitCombo2)
        {
            PerformAttack(lightAttackHitbox, lightAttackKnockback);
            hasHitCombo2 = true;
            combo2DelayTimer = -1; // Prevent repeated hits
        }
        
        // Reset light attack bools when cooldown ends
        if (lightAttackTimer <= 0 && _animator != null)
        {
            _animator.SetBool("IsLightAttack", false);
            _animator.SetBool("IsLightAttack2", false);
            _animator.SetBool("IsLightForward", false);
        }
        
        // Reset combo hits when combo timer expires
        if (lightAttackComboTimer <= 0)
        {
            hasHitCombo1 = false;
            hasHitCombo2 = false;
        }
        
        // Heavy Attack (supports forward variant when moving)
        if (GetHeavyAttackInput() && heavyAttackTimer <= 0)
        {
            Debug.Log($"[Player {playerNumber}] Heavy attack triggered");
            heavyAttackTimer = heavyAttackCooldown;
            heavyKnockbackDelayTimer = heavyAttackKnockbackDelay;
            
            // Trigger heavy attack animation
            if (_animator != null)
            {
                bool movingForHeavyForward = Mathf.Abs(GetMoveInput()) > 0.1f;
                if (movingForHeavyForward)
                {
                    Debug.Log($"[Player {playerNumber}] Setting IsHeavyForward to true");
                    _animator.SetBool("IsHeavyForward", true);
                    _animator.SetBool("IsHeavyAttack", false);
                }
                else
                {
                    Debug.Log($"[Player {playerNumber}] Setting IsHeavyAttack to true");
                    _animator.SetBool("IsHeavyAttack", true);
                    _animator.SetBool("IsHeavyForward", false);
                }
                _animator.speed = 1; // Ensure animator speed is normal
            }
            else
            {
                Debug.LogError($"[Player {playerNumber}] Animator is NULL!");
            }
        }
        
        // Apply heavy attack knockback after delay
        if (heavyKnockbackDelayTimer <= 0 && heavyAttackTimer > 0 && heavyKnockbackDelayTimer > -0.5f)
        {
            PerformAttack(heavyAttackHitbox, heavyAttackKnockback, true);
            heavyKnockbackDelayTimer = -1; // Prevent repeated hits
            
            // Reset heavy attack bool immediately after knockback
            if (_animator != null)
            {
                _animator.SetBool("IsHeavyAttack", false);
                _animator.SetBool("IsHeavyForward", false);
            }
        }
    }
    
    void PerformAttack(Transform hitbox, float knockbackForce)
    {
        PerformAttack(hitbox, knockbackForce, false);
    }
    
    void PerformAttack(Transform hitbox, float knockbackForce, bool isHeavyAttack)
    {
        if (hitbox == null) return;
        
        // Get the collider from the hitbox child object
        Collider2D hitboxCollider = hitbox.GetComponent<Collider2D>();
        if (hitboxCollider == null) return;
        
        // Find all overlapping colliders
        Collider2D[] hits = new Collider2D[10];
        int hitCount = hitboxCollider.Overlap(ContactFilter2D.noFilter, hits);
        
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i] != null && hits[i].gameObject != gameObject)
            {
                // Attack any object with a Rigidbody2D (except self)
                Rigidbody2D targetRb = hits[i].GetComponent<Rigidbody2D>();
                if (targetRb != null)
                {
                    // Calculate knockback direction
                    Vector2 dir = (hits[i].transform.position - transform.position).normalized;
                    Vector2 knockback;
                    
                    if (targetRb.TryGetComponent<PlayerController>(out var targetPlayer))
                    {
                        // Ignore damage if target is invulnerable
                        if (targetPlayer.isInvulnerable)
                        {
                            continue;
                        }
                        // Increment hit count before deciding final knockback
                        targetPlayer.hitsTaken++;
                        bool skyrocket = targetPlayer.hitsTaken > targetPlayer.hitsThreshold;

                        if (skyrocket)
                        {
                            // Base horizontal component with extra boost
                            float horizontal = dir.x * knockbackForce * targetPlayer.skyrocketHorizontalMultiplier * targetPlayer.skyrocketHorizontalMultiplierBoost;
                            // Enforce minimum horizontal speed to ensure player exits platform
                            if (Mathf.Abs(horizontal) < targetPlayer.skyrocketMinHorizontalSpeed)
                            {
                                horizontal = Mathf.Sign(horizontal == 0 ? dir.x : horizontal) * targetPlayer.skyrocketMinHorizontalSpeed;
                            }
                            float vertical = knockbackForce * targetPlayer.skyrocketVerticalMultiplier;
                            knockback = new Vector2(horizontal, vertical);
                            targetPlayer.hitsTaken = 0; // Reset after trigger
                            targetPlayer.justSkyrocketed = true;
                        }
                        else if (isHeavyAttack)
                        {
                            knockback = new Vector2(dir.x * knockbackForce, knockbackForce * 1.5f);
                        }
                        else
                        {
                            knockback = new Vector2(dir.x * knockbackForce, knockbackForce * 0.8f);
                        }

                        // Apply knockback
                        targetRb.linearVelocity = knockback;
                        targetPlayer.knockbackLockTimer = 0.2f;

                        // Animation flags
                        Animator targetAnimator = targetPlayer.GetAnimator();
                        if (targetAnimator != null)
                        {
                            targetAnimator.SetBool("IsHit", true);
                            if (skyrocket)
                            {
                                // Optional: if you add an animator bool for special launch, set it here
                                // targetAnimator.SetTrigger("Skyrocket");
                            }
                        }
                    }
                    else
                    {
                        // Non-player target: calculate generic knockback
                        if (isHeavyAttack)
                        {
                            knockback = new Vector2(dir.x * knockbackForce, knockbackForce * 1.5f);
                        }
                        else
                        {
                            knockback = new Vector2(dir.x * knockbackForce, knockbackForce * 0.8f);
                        }
                        targetRb.linearVelocity = knockback;
                    }
                    
                    Debug.Log($"Hit {hits[i].gameObject.name} with {knockbackForce} knockback!");
                }
            }
        }
    }

    public void ForceRespawn()
    {
        HandleDeath();
    }
    public bool IsInvulnerable() => isInvulnerable;

    void HandleDeath()
    {
        if (!enableRespawn || isRespawning || isEliminated)
        {
            return;
        }

        // Check lives: if remaining, consume and respawn; else eliminate
        if (respawnsUsed < maxRespawns)
        {
            // Reset accumulated hit count on death before scheduling respawn
            hitsTaken = 0;
            justSkyrocketed = false;
            respawnsUsed++;
            StartCoroutine(RespawnAfterDelay());
            Debug.Log($"[Player {playerNumber}] Death detected. Scheduling respawn {respawnsUsed}/{maxRespawns} in {respawnDelay}s.");
        }
        else
        {
            Eliminate();
        }
    }

    void Respawn()
    {
        // Reset physics
        rb.linearVelocity = Vector2.zero;
        Vector3 target = respawnPoint != null ? respawnPoint.position : spawnPosition;
        transform.position = target;

        // Restore default facing direction if changed during previous life
        if (facingRight != initialFacingRight)
        {
            FlipDirection();
        }

        // Reset state
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        lightAttackTimer = 0f;
        heavyAttackTimer = 0f;
        lightKnockbackDelayTimer = 0f;
        heavyKnockbackDelayTimer = 0f;
        lightAttackComboTimer = 0f;
        combo1DelayTimer = 0f;
        combo2DelayTimer = 0f;
        hasHitCombo1 = false;
        hasHitCombo2 = false;
        lightAttackComboCount = 0;
        jumps = 0;
        knockbackLockTimer = 0f;

        // Invulnerability window
        isInvulnerable = true;
        invulnTimer = respawnInvulnTime;

        // Ensure hits reset after respawn as well (safety)
        hitsTaken = 0;
        justSkyrocketed = false;

        // Reset animator flags
        if (_animator != null)
        {
            _animator.SetBool("IsHit", false);
            _animator.SetBool("IsJump", false);
            _animator.SetBool("IsLightAttack", false);
            _animator.SetBool("IsLightAttack2", false);
            _animator.SetBool("IsLightForward", false);
            _animator.SetBool("IsHeavyAttack", false);
            _animator.SetBool("IsDash", false);
        }

        // Optional: minor visual flash or sound here
        Debug.Log($"[Player {playerNumber}] Respawned at {transform.position}");
    }

    System.Collections.IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;
        // Disable visuals & collider during delay (optional fade could be added later)
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;
        isRespawning = false;
    }

    void Eliminate()
    {
        isEliminated = true;
        enableRespawn = false;
        // Disable interaction
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (_animator != null) _animator.enabled = false;
        // Final reset for clarity
        hitsTaken = 0;
        justSkyrocketed = false;
        Debug.Log($"[Player {playerNumber}] Eliminated (no respawns left).");
    }
    
    void FlipDirection()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        // Counter parent horizontal flip so text stays readable (double-flip technique)
        if (playerLabel != null)
        {
            var ls = playerLabel.transform.localScale;
            ls.x = scale.x < 0 ? -1f : 1f; // If parent is negative, set child negative to yield positive world scale
            playerLabel.transform.localScale = ls;
        }
        if (playerLabelShadow != null)
        {
            var ls = playerLabelShadow.transform.localScale;
            ls.x = scale.x < 0 ? -1f : 1f;
            playerLabelShadow.transform.localScale = ls;
        }
    }
    
    void UpdateAnimations()
    {
        if (_animator == null) return;
        
        // Handle hit flash effect
        if (_animator.GetBool("IsHit"))
        {
            hitFlashTimer += Time.deltaTime;
            
            // Flash between bright yellow and original color
            // Flash faster when knocked upward
            float flashSpeed = rb.linearVelocity.y > 0.5f ? 0.05f : hitFlashDuration; // Faster flash when airborne
            
            if (spriteRenderer != null)
            {
                if ((hitFlashTimer / flashSpeed) % 2 < 1)
                {
                    spriteRenderer.color = new Color(1f, 1f, 0f, 1f); // Bright yellow
                }
                else
                {
                    spriteRenderer.color = originalColor;
                }
            }
            
            // Flip sprite based on knockback direction
            if (rb.linearVelocity.x > 0.5f && facingRight)
            {
                FlipDirection(); // Being knocked right, face right
            }
            else if (rb.linearVelocity.x < -0.5f && !facingRight)
            {
                FlipDirection(); // Being knocked left, face left
            }
        }
        else
        {
            // If invulnerable (and not hit), blink; otherwise reset color
            if (isInvulnerable)
            {
                hitFlashTimer += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    float t = Mathf.PingPong(hitFlashTimer * 10f, 1f);
                    Color c = originalColor;
                    c.a = Mathf.Lerp(0.3f, 1f, t);
                    spriteRenderer.color = c;
                }
            }
            else
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
                hitFlashTimer = 0f;
            }
        }
        
        // Set IsRun based on movement
        bool isMoving = Mathf.Abs(currentVelocityX) > 0.1f;
        _animator.SetBool("IsRun", isMoving && isGrounded);
        
        // Reset IsHit only when grounded
        if (isGrounded && _animator.GetBool("IsHit"))
        {
            _animator.SetBool("IsHit", false);
        }
        
        // Reset light attack after animation finishes
        if (lightAttackTimer <= 0)
        {
            _animator.SetBool("IsLightAttack", false);
        }

        // Jump animation control: slow playback and hold last frame while airborne
        bool jumpFlag = _animator.GetBool("IsJump");
        if (jumpFlag && !isGrounded)
        {
            var st = _animator.GetCurrentAnimatorStateInfo(0);
            // If nearing end of jump clip, freeze animator to hold the pose
            if (!jumpHoldApplied && st.normalizedTime >= jumpHoldThreshold)
            {
                _animator.speed = 0f;
                jumpHoldApplied = true;
            }
            else
            {
                _animator.speed = Mathf.Max(0.01f, jumpPlaySpeed);
            }
        }
        else
        {
            // Restore normal speed when grounded or not in jump
            if (_animator.speed != 1f)
            {
                _animator.speed = 1f;
            }
            jumpHoldApplied = false;
        }

        // Update label visibility & (optional) dynamic height with smoothing
        if (showPlayerLabel && playerLabel != null)
        {
            bool visible = !isEliminated && (spriteRenderer == null || spriteRenderer.enabled);
            if (playerLabel.enabled != visible)
            {
                playerLabel.enabled = visible;
            }
            if (playerLabelShadow != null && playerLabelShadow.enabled != visible)
            {
                playerLabelShadow.enabled = visible;
            }
            // Determine target local Y offset
            float targetY = playerLabelOffset.y;
            if (dynamicLabelHeight && spriteRenderer != null)
            {
                // Convert world sprite height to local Y (counter scale if scaled)
                float rawHeight = spriteRenderer.bounds.size.y;
                float scaleY = Mathf.Max(transform.localScale.y, 0.0001f);
                targetY = (rawHeight / scaleY) + playerLabelPadding; // configurable padding
            }
            Vector3 currentLocal = playerLabel.transform.localPosition;
            Vector3 targetLocal = new Vector3(playerLabelOffset.x, targetY, playerLabelOffset.z);
            playerLabel.transform.localPosition = Vector3.Lerp(currentLocal, targetLocal, Time.deltaTime * playerLabelSmoothing);

            if (playerLabelShadow != null)
            {
                Vector3 shadowTarget = playerLabel.transform.localPosition + new Vector3(0.03f, -0.03f, 0f);
                playerLabelShadow.transform.localPosition = Vector3.Lerp(playerLabelShadow.transform.localPosition, shadowTarget, Time.deltaTime * playerLabelSmoothing);
            }
        }
    }
    
    // Input abstraction methods - these support both keyboard and gamepad
    float GetMoveInput()
    {
        if (moveAction == null)
        {
            Debug.LogWarning($"[Player {playerNumber}] moveAction is null!");
            return 0f;
        }
        if (!moveAction.enabled)
        {
            Debug.LogWarning($"[Player {playerNumber}] moveAction is not enabled!");
            return 0f;
        }
        Vector2 moveValue = Vector2.zero;
        try
        {
            moveValue = moveAction.ReadValue<Vector2>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Player {playerNumber}] Exception reading moveAction: {ex.Message}");
        }
        Debug.Log($"[Player {playerNumber}] moveAction value: {moveValue}");
        if (Mathf.Abs(moveValue.x) > 0.001f) return moveValue.x;

        // Fallback for Player 1: keyboard A/D or Left/Right arrows
        if (playerNumber == 1 && Keyboard.current != null)
        {
            float x = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            return x;
        }

        return 0f;
    }
    
    bool GetJumpInput()
    {
        if (jumpAction != null && jumpAction.WasPressedThisFrame()) return true;

        // Fallback for Player 1: space or up arrow
        if (playerNumber == 1 && Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame;
        }
        return false;
    }
    
    bool GetDashInput()
    {
        if (playerControls == null)
            return false;

        if (dashAction != null && dashAction.WasPressedThisFrame()) return true;

        // Fallback for Player 1: LeftShift or RightShift
        if (playerNumber == 1 && Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame;
        }
        return false;
    }
    
    bool GetLightAttackInput()
    {
        if (lightAttackAction != null && lightAttackAction.WasPressedThisFrame()) return true;

        // Fallback for Player 1: J key or Z
        if (playerNumber == 1 && Keyboard.current != null)
        {
            return Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame;
        }
        return false;
    }
    
    bool GetHeavyAttackInput()
    {
        if (heavyAttackAction != null && heavyAttackAction.WasPressedThisFrame()) return true;

        // Fallback for Player 1: K key or X
        if (playerNumber == 1 && Keyboard.current != null)
        {
            return Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame;
        }
        return false;
    }
    
    bool IsHeavyAttackHeld()
    {
        if (playerControls == null)
            return false;

        if (heavyAttackAction != null && heavyAttackAction.IsPressed()) return true;

        if (playerNumber == 1 && Keyboard.current != null)
        {
            return Keyboard.current.kKey.isPressed || Keyboard.current.xKey.isPressed;
        }
        return false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw ground check position
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 checkPos = new Vector2(transform.position.x, col.bounds.min.y - 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(checkPos, groundDistance);
        }
    }

    void SetRespawnPoint(Transform t)
    {
        respawnPoint = t;
        if (t != null)
        {
            spawnPosition = t.position;
        }
    }

    void SetSpawnPosition(Vector3 pos)
    {
        spawnPosition = pos;
    }

    // --- Controller dynamic pairing support ---
    void OnEnable()
    {
        // Ensure this GameObject is always tagged as 'Player' at runtime
        if (gameObject.tag != "Player")
        {
            Debug.LogWarning($"[PlayerController] GameObject '{gameObject.name}' did not have 'Player' tag. Setting it now.");
            gameObject.tag = "Player";
        }
        if (!allPlayers.Contains(this)) allPlayers.Add(this);
        InputSystem.onDeviceChange += OnDeviceChange;
        TryAssignUnpairedGamepads();
    }

    void OnDisable()
    {
        allPlayers.Remove(this);
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.InputDeviceChange change)
    {
        if (device is Gamepad && (change == UnityEngine.InputSystem.InputDeviceChange.Added || change == UnityEngine.InputSystem.InputDeviceChange.Reconnected))
        {
            TryAssignUnpairedGamepads();
        }
        if (device is Gamepad && (change == UnityEngine.InputSystem.InputDeviceChange.Removed || change == UnityEngine.InputSystem.InputDeviceChange.Disconnected))
        {
            // Mark players using this pad as inactive
            foreach (var pc in allPlayers)
            {
                if (pc != null && pc.hasGamepad)
                {
                    foreach (var d in pc.inputUser.pairedDevices)
                    {
                        if (ReferenceEquals(d, device))
                        {
                            pc.hasGamepad = false;
                            Debug.Log($"[Player {pc.playerNumber}] Gamepad disconnected. Player inactive until new pad pairs.");
                        }
                    }
                }
            }
            // Attempt reassignment in case other pads remain
            TryAssignUnpairedGamepads();
        }
    }

    static void TryAssignUnpairedGamepads()
    {
        var pads = Gamepad.all;
        if (pads.Count == 0) return;
        // Collect used pads
        var used = new System.Collections.Generic.HashSet<UnityEngine.InputSystem.InputDevice>();
        foreach (var pc in allPlayers)
        {
            if (pc != null && pc.hasGamepad && pc.inputUser.valid)
            {
                foreach (var d in pc.inputUser.pairedDevices)
                {
                    if (d is Gamepad) used.Add(d);
                }
            }
        }
        // Assign free pads to players without one (in playerNumber order)
        foreach (var pc in allPlayers)
        {
            if (pc == null || pc.hasGamepad) continue;
            if (!pc.inputUser.valid)
            {
                // Recreate user if previously invalid (e.g., component disabled & re-enabled)
                pc.inputUser = InputUser.CreateUserWithoutPairedDevices();
                pc.inputUser.AssociateActionsWithUser(pc.playerControls);
            }
            foreach (var pad in pads)
            {
                if (!used.Contains(pad))
                {
                    InputUser.PerformPairingWithDevice(pad, pc.inputUser);
                    pc.hasGamepad = true;
                    used.Add(pad);
                    Debug.Log($"[Player {pc.playerNumber}] Auto-paired with gamepad: {pad.displayName}");
                    if (!pc.enabled)
                    {
                        pc.enabled = true; // Reactivate gameplay now that a controller is present
                        Debug.Log($"[Player {pc.playerNumber}] Component re-enabled after controller pairing.");
                    }
                    break;
                }
            }
        }
    }

    Color GetColorForPlayer(int number)
    {
        switch (number)
        {
            case 1: return new Color(0.95f, 0.2f, 0.2f); // Red
            case 2: return new Color(0.2f, 0.5f, 1f);    // Blue
            case 3: return new Color(0.2f, 0.85f, 0.35f); // Green
            case 4: return new Color(0.95f, 0.8f, 0.2f); // Yellow
            default: return Color.white;
        }
    }
}
