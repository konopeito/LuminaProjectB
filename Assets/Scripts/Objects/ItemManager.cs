using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public ItemPickup[] itemPrefabs;      // Prefabs with SpriteRenderer + collider 
    public Transform[] spawnPoints;       // Where items can spawn
    public float spawnInterval = 10f;     // Seconds between spawns

    void Start()
    {
        // Spawn items repeatedly
        InvokeRepeating(nameof(SpawnRandomItem), 1f, spawnInterval);
    }

    void SpawnRandomItem()
    {
        if (itemPrefabs.Length == 0 || spawnPoints.Length == 0) return;

        // Pick a random prefab and random spawn point
        ItemPickup prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Spawn the item
        Instantiate(prefab, spawn.position, Quaternion.identity);
    }
}
