using UnityEngine;
using System.Collections;

public class ChickController : MonoBehaviour
{
    [SerializeField] private float flySpeed = 2.5f;
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmplitude = 0.3f;
    [SerializeField] private Camera mainCamera;

    public float aniMult = 0.5f;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private float timeElapsed = 0;
    private bool flying = false;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        animator.enabled = false;

        // Auto-find main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Update()
    {
        if (!flying)
        {
            return;
        }
        timeElapsed += Time.deltaTime;

        // Natural bobbing using sine wave
        float bobOffset = Mathf.Sin(timeElapsed * bobFrequency) * bobAmplitude;

        // Move up and to the right, with bobbing
        transform.position += new Vector3(flySpeed * 0.25f, flySpeed * 0.75f + bobOffset, 0f) * Time.deltaTime;

        // Check if off screen
        Vector3 screenPoint = mainCamera.WorldToViewportPoint(transform.position);
        bool isOffScreen = screenPoint.x > 1f || screenPoint.y > 1f;
        aniMult *= 1.01f;
        flySpeed *= 1.01f;
        animator.SetFloat("speed", aniMult);

        if (isOffScreen)
            Destroy(gameObject);
    }

    public void Escape()
    {

        StartCoroutine(FlyAway());
    }

    IEnumerator FlyAway()
    {
        yield return new WaitForSeconds(1f);

        animator.enabled = true;
        animator.SetTrigger("hatchDone");
        yield return new WaitForSeconds(0.5f);
        flying = true;
    }
}
