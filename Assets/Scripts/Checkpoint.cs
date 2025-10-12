using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public bool isActive = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActive) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            // Directly set respawn position in PlayerController
            player.SetCheckpoint(transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
    }
}
