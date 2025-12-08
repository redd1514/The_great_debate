using UnityEngine;

public class PlatformDrop : MonoBehaviour
{
    [Header("Platform Drop Settings")]
    [SerializeField] private float platformDropForce = -5f;
    [SerializeField] private float dropCooldown = 0.2f;
    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private float collisionReenableDelay = 0.5f;
    
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private float dropCooldownTimer;
    private bool isDropping = false;
    private Collider2D currentDropPlatform;

    // Reference to the big platform (the one where you can't drop through)
    [SerializeField] private GameObject bigPlatform;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        
        // Auto-find platform layer if not set
        if (platformLayerMask == 0)
        {
            platformLayerMask = LayerMask.GetMask("Ground", "Platform", "Platforms");
        }
    }

    void Update()
    {
        if (dropCooldownTimer > 0)
            dropCooldownTimer -= Time.deltaTime;

        // Check for S key press to drop through platform
        if (Input.GetKeyDown(KeyCode.S) && CanDropPlatform())
        {
            DropThroughPlatform();
        }
    }

    bool CanDropPlatform()
    {
        // Check if player is on a platform
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        Vector2 checkPos = new Vector2(transform.position.x, col.bounds.min.y - 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, 1f, platformLayerMask);

        if (hit.collider != null)
        {
            // Check if it's the big platform - can't drop through it
            if (bigPlatform != null && hit.collider.gameObject == bigPlatform)
            {
                return false;
            }

            // Check if cooldown is ready
            return dropCooldownTimer <= 0;
        }

        return false;
    }

    void DropThroughPlatform()
    {
        // Find the platform below
        Collider2D col = GetComponent<Collider2D>();
        Vector2 checkPos = new Vector2(transform.position.x, col.bounds.min.y - 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, 1f, platformLayerMask);

        if (hit.collider != null && (bigPlatform == null || hit.collider.gameObject != bigPlatform))
        {
            currentDropPlatform = hit.collider;
            
            // Ignore collision with this platform temporarily
            Physics2D.IgnoreCollision(playerCollider, hit.collider, true);

            // Apply downward velocity to move through platform
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, platformDropForce);

            // Re-enable collision after a delay
            Invoke(nameof(ReenableCollision), collisionReenableDelay);

            dropCooldownTimer = dropCooldown;
            isDropping = true;

            Debug.Log($"Dropping through platform: {hit.collider.gameObject.name}");
        }
    }

    void ReenableCollision()
    {
        if (currentDropPlatform != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, currentDropPlatform, false);
            Debug.Log($"Re-enabled collision with platform: {currentDropPlatform.gameObject.name}");
        }

        isDropping = false;
        currentDropPlatform = null;
    }

    public bool IsDropping() => isDropping;
}
