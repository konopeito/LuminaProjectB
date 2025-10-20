using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item Settings")]
    public Item item;

    [Header("Hover Settings")]
    public float hoverAmplitude = 0.1f; // Adjusted to a smaller hover
    public float hoverSpeed = 2f;

    [Header("Pickup Settings")]
    public float pickupDistance = 2f; // distance from player
    public GameObject pickupPromptPrefab;

    private GameObject pickupPromptInstance;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;

        if (pickupPromptPrefab != null)
        {
            pickupPromptInstance = Instantiate(pickupPromptPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            pickupPromptInstance.SetActive(false);
        }
    }

    void Update()
    {
        Hover();
        UpdatePrompt();
    }

    private void Hover()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
    }

    private void UpdatePrompt()
    {
        if (pickupPromptInstance == null) return;

        // Always follow the item
        pickupPromptInstance.transform.position = transform.position + Vector3.up * 0.5f;

        // Check for nearby player
        PlayerController player = FindClosestPlayer();
        if (player != null && Vector2.Distance(player.transform.position, transform.position) <= pickupDistance)
            pickupPromptInstance.SetActive(true);
        else
            pickupPromptInstance.SetActive(false);
    }

    public PlayerController FindClosestPlayer()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        PlayerController closest = null;
        float closestDist = Mathf.Infinity;

        foreach (var p in players)
        {
            float dist = Vector2.Distance(p.transform.position, transform.position);
            if (dist < closestDist)
            {
                closest = p;
                closestDist = dist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Called by PlayerController when pressing Q
    /// </summary>
    public bool TryPickup(PlayerInventory inventory)
    {
        if (inventory == null) return false;

        // Only allow pickup if in range
        if (Vector2.Distance(inventory.transform.position, transform.position) > pickupDistance)
            return false;

        bool added = inventory.AddItem(item);
        if (added)
        {
            if (pickupPromptInstance != null)
                Destroy(pickupPromptInstance);

            Destroy(gameObject);
            return true;
        }

        return false;
    }
}
