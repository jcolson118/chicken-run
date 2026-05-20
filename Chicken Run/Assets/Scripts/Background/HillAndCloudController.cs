using UnityEngine;

public class HillAndCloudController : MonoBehaviour
{
    [SerializeField] public float scrollSpeed = 0.5f;

    public float width = 1f;

    private float startX;
    private GameObject left;
    private GameObject right;

    void Start()
    {
        startX = transform.position.x;

        float screenHeightInWorldUnits = Camera.main.orthographicSize * 2.0f;
        float screenWidthInWorldUnits = screenHeightInWorldUnits * Screen.width / Screen.height;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float spriteNativeWidth = sr.sprite.bounds.size.x;
        float scaleX = screenWidthInWorldUnits / spriteNativeWidth;
        transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        width = screenWidthInWorldUnits;

        left = Instantiate(gameObject, new Vector3(startX - width, transform.position.y, transform.position.z), Quaternion.identity);
        right = Instantiate(gameObject, new Vector3(startX + width, transform.position.y, transform.position.z), Quaternion.identity);

        // Disable clones' scripts so only this one manages everything
        left.GetComponent<HillAndCloudController>().enabled = false;
        right.GetComponent<HillAndCloudController>().enabled = false;
    }

    void Update()
    {

    }
    void LateUpdate()
    {
        float camX = Camera.main.transform.position.x;
        float newX = Mathf.Repeat(camX * scrollSpeed, width);
        transform.position = new Vector3(startX + newX, transform.position.y, transform.position.z);

        left.transform.position = new Vector3(transform.position.x - width, transform.position.y, transform.position.z);
        right.transform.position = new Vector3(transform.position.x + width, transform.position.y, transform.position.z);

        // If camera is closer to right clone, make it the new center
        if (Mathf.Abs(camX - right.transform.position.x) < Mathf.Abs(camX - transform.position.x))
        {
            startX += width;
            left.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        }
        // If camera is closer to left clone, make it the new center
        else if (Mathf.Abs(camX - left.transform.position.x) < Mathf.Abs(camX - transform.position.x))
        {
            startX -= width;
            right.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        }
    }
}
