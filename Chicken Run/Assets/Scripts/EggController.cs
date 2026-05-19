using UnityEngine;

public class EggController : MonoBehaviour
{
    [SerializeField] private Sprite[] crackSprites;   // drag egg-cracking frames here
    [SerializeField] private AnimationClip hatchClip; // drag hatch animation here
    [SerializeField] private float peckRadius = 1f;   // how close chicken must be to peck
    [SerializeField] private GameObject chickPrefab;  // drag the Chick prefab here

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

        // Spawn the Chick prefab at the egg's position
        if (chickPrefab != null)
        {
            GameObject chick = Instantiate(chickPrefab, transform.position, Quaternion.identity);
            // Store reference to avoid repeated calls
            StartCoroutine(WaitForChickCollision(chick));
        }
    }

    System.Collections.IEnumerator WaitForChickCollision(GameObject chick)
    {
        ChickController chickController = chick.GetComponent<ChickController>();
        float timeLimit = 3f;
        float elapsed = 0f;

        while (elapsed < timeLimit && chickController != null)
        {
            if (Vector2.Distance(transform.position, chick.transform.position) < 0.5f)
            {
                GetComponent<Collider2D>().enabled = false;
                chickController.Escape();
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}