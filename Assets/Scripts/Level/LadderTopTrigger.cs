using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LadderTopTrigger : MonoBehaviour
{
    [Header("Zone Settings")]
    public string zoneKey = "Zone2";
    public string zoneTypeDisplay = "Zone 2";
    public string zoneDisplayName = "The Wastelands";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null && player.IsClimbing())
        {
            player.StopClimbing(); // safely stops climbing
            player.StartCoroutine(player.TransitionToNextZone(zoneTypeDisplay, zoneKey, zoneDisplayName));
        }
    }
}
