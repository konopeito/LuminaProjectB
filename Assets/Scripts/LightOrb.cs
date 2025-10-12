using UnityEngine;

public class LightOrb : MonoBehaviour
{
    public float collectSpeed = 5f; // How fast orb moves to player
    private bool isCollected = false;
    private PlayerLight playerLight;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isCollected && playerLight != null)
        {
            // Move toward player
            transform.position = Vector3.MoveTowards(transform.position, playerLight.transform.position, collectSpeed * Time.deltaTime);

            // Fade out
            Color color = sr.color;
            color.a = Mathf.Lerp(color.a, 0f, Time.deltaTime * 5f);
            sr.color = color;

            // Destroy when close
            if (Vector3.Distance(transform.position, playerLight.transform.position) < 0.1f)
            {
                playerLight.CollectLight();
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isCollected)
        {
            playerLight = collision.GetComponent<PlayerLight>();
            if (playerLight != null)
            {
                isCollected = true;
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }
}
