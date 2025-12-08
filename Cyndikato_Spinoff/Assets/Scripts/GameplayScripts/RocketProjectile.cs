using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    [Header("Rocket Settings")]
    public float speed = 18f;
    public float lifetime = 3f;
    public float knockbackForce = 12f;
    public bool destroyOnHit = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerController owner;
    private float dir = 1f; // +1 right, -1 left
    private float age;

    public void Initialize(PlayerController owner, float direction, float speed, float knockback, float lifetime)
    {
        this.owner = owner;
        this.dir = Mathf.Sign(direction) == 0 ? 1f : Mathf.Sign(direction);
        this.speed = speed;
        this.knockbackForce = knockback;
        this.lifetime = lifetime;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        var projCol = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f; // rocket stays on a straight path
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = new Vector2(dir * speed, 0f);
        }

        // Flip sprite(s) to match travel direction
        if (sr != null) sr.flipX = dir < 0f;
        var childRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var cr in childRenderers)
        {
            cr.flipX = dir < 0f;
        }

        // Keep rotation neutral; rely on flipX for horizontal direction
        transform.rotation = Quaternion.identity;

        // Ignore collisions with the owner and all child colliders
        if (projCol != null && owner != null)
        {
            projCol.isTrigger = true; // use trigger-based hits
            var ownerCols = owner.GetComponentsInChildren<Collider2D>();
            foreach (var c in ownerCols)
            {
                if (c != null) Physics2D.IgnoreCollision(projCol, c, true);
            }
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        // Maintain straight motion
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(dir * speed, 0f);
        }
        else
        {
            // Fallback movement if no rigidbody
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D col)
    {
        if (col == null) return;
        // Ignore hitting the owner or any of its children
        if (owner != null)
        {
            var hitOwner = col.GetComponentInParent<PlayerController>();
            if (hitOwner != null && hitOwner == owner) return;
        }

        // Only process hits against players; ignore environment so rocket doesn't disappear
        if (col.TryGetComponent<PlayerController>(out var targetPlayer))
        {
            if (targetPlayer.IsInvulnerable()) return;

            // Use attached rigidbody or parent's rigidbody for reliable physics
            var targetRb = col.attachedRigidbody != null ? col.attachedRigidbody : col.GetComponentInParent<Rigidbody2D>();
            if (targetRb != null)
            {
                // Apply horizontal knockback with slight upward push
                Vector2 knock = new Vector2(dir * knockbackForce, knockbackForce * 0.5f);
                targetRb.linearVelocity = knock;
                targetPlayer.knockbackLockTimer = 0.2f;
                var anim = targetPlayer.GetAnimator();
                if (anim != null) anim.SetBool("IsHit", true);
            }

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
        // else: non-player collider, ignore (no destroy) so the rocket continues
    }
}
