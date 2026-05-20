using UnityEngine;
using System.Collections;

public class ChickenController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float glideGravity = 0.5f;
    [SerializeField] private float normalGravity = 3f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isGliding;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Camera.main.transform.position = new Vector3(
        transform.position.x,
        Camera.main.transform.position.y,
        Camera.main.transform.position.z
    );
        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        float horizontalInput = 0f;

        // A/Left Arrow = move left, D/Right Arrow = move right
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            horizontalInput = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            horizontalInput = -1f;

        // Move the chicken
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        // Flip sprite direction
        if (horizontalInput > 0)
            spriteRenderer.flipX = false;
        else if (horizontalInput < 0)
            spriteRenderer.flipX = true;

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetTrigger("jump");
        }

        // Peck
        if (Input.GetKeyDown(KeyCode.Mouse0) && isGrounded)
        {
            animator.SetTrigger("peck");
            StartCoroutine(PeckDelay());
        }
        // Glide: only when falling (velocity.y < 0) and space is held
        bool isFalling = rb.linearVelocity.y < 0;
        isGliding = !isGrounded && isFalling && Input.GetKey(KeyCode.Space);

        if (isGliding)
            rb.gravityScale = glideGravity;
        else
            rb.gravityScale = normalGravity;

        // Animator
        bool isWalking = horizontalInput != 0 && isGrounded;
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isFlying", isGliding);
    }
    void OnDrawGizmosSelected()
    {
        float peckDistance = 0.5f;
        float direction = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        Vector2 peckPoint = (Vector2)transform.position + new Vector2(direction * peckDistance, 0f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(peckPoint, 0.2f);
    }

    IEnumerator PeckDelay()
    {
        yield return new WaitForSeconds(0.5f);

        float peckDistance = 0.5f;
        float direction = spriteRenderer.flipX ? -1f : 1f;
        Vector2 peckPoint = (Vector2)transform.position + new Vector2(direction * peckDistance, 0f);

        Debug.Log($"[PeckDelay] flipX={spriteRenderer.flipX}, direction={direction}, peckPoint={peckPoint}, chickPos={transform.position}");

        // Get all colliders in the peck area and find the egg, excluding the chicken itself
        Collider2D[] hits = Physics2D.OverlapCircleAll(peckPoint, 0.2f);
        Collider2D eggHit = null;

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Egg"))
            {
                eggHit = hit;
                break;
            }
        }

        Debug.Log($"[PeckDelay] OverlapCircleAll found {hits.Length} colliders, egg hit: {(eggHit != null ? eggHit.gameObject.name : "nothing")}");

        if (eggHit != null)
        {
            Debug.Log("[PeckDelay] Hitting egg!");
            eggHit.GetComponent<EggController>().Peck(transform.position);
        }
    }
}