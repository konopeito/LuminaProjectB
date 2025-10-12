using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DeathZone : MonoBehaviour
{
    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            // Call respawn in PlayerController at last checkpoint
            player.Respawn();
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawCube(box.bounds.center, box.bounds.size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}
