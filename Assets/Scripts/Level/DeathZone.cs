using UnityEngine;
using System.Collections;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerLight playerLight = collision.GetComponent<PlayerLight>();
        PlayerController playerController = collision.GetComponent<PlayerController>();

        if (playerLight != null && playerController != null)
        {
            // Trigger death animation & stop movement
            playerController.OnDie();

            // Disable player input/movement immediately
            playerController.enabled = false;

            // Start the death sequence: hearts animation then Game Over
            StartCoroutine(HandleDeathSequence(playerLight, playerController));
        }
    }

    private IEnumerator HandleDeathSequence(PlayerLight playerLight, PlayerController playerController)
    {
        // Animate all hearts depleting above head
        if (playerLight.healthUI != null)
        {
            yield return StartCoroutine(playerLight.healthUI.AnimateAllHeartsDeplete());
        }

        // Optional: keep player frozen here or fade out screen
        // Could add delay if needed: yield return new WaitForSeconds(0.5f);

        // Trigger Game Over
        if (GameMenusManager.Instance != null)
        {
            GameMenusManager.Instance.TriggerGameOver();
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
