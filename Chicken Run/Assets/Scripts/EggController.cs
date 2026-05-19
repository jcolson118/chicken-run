using UnityEngine;

public class EggController : MonoBehaviour
{
    [SerializeField] private Sprite[] crackSprites;   // drag egg-cracking frames here
    [SerializeField] private AnimationClip hatchClip; // drag hatch animation here
    [SerializeField] private float peckRadius = 1f;   // how close chicken must be to peck

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private int currentCrackIndex = 0;
    private bool isHatching = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Disable animator until hatch so it doesn't override sprites
        animator.enabled = false;

        if (crackSprites.Length > 0)
            spriteRenderer.sprite = crackSprites[0];
    }

    public void Peck(Vector2 chickenPosition)
    {
        if (isHatching) return;

        currentCrackIndex++;

        if (currentCrackIndex >= crackSprites.Length)
        {
            Hatch();
        }
        else
        {
            spriteRenderer.sprite = crackSprites[currentCrackIndex];
        }
    }

    void Hatch()
    {
        isHatching = true;
        animator.enabled = true;
        animator.SetTrigger("hatch");
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isHatching && other.CompareTag("Chick"))
        {
            GetComponent<Collider2D>().enabled = false; // disable after chick is found
            other.GetComponent<ChickController>().Escape();
        }
    }
}