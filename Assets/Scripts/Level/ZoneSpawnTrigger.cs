using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class ZoneSpawnTrigger : MonoBehaviour
{
    [Header("Zone Settings")]
    public string zoneKey = "Zone2";            // internal key, must match PlayerController spawn points
    public string zoneTypeDisplay = "Zone 2";   // UI display for zone type
    public string zoneDisplayName = "The Wastelands"; // UI display for zone name

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered) return; // prevent multiple triggers

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            hasTriggered = true;

            // Start delayed transition to ensure black fade renders correctly
            StartCoroutine(StartTransitionWithDelay(player));
        }
    }

    private IEnumerator StartTransitionWithDelay(PlayerController player)
    {
        // Wait one frame to ensure all physics & collisions are processed
        yield return null;

        // Optional: tiny additional delay if needed
        // yield return new WaitForSecondsRealtime(0.05f);

        // Start the actual transition coroutine
        player.StartCoroutine(player.TransitionToNextZone(zoneTypeDisplay, zoneKey, zoneDisplayName));
    }
}
