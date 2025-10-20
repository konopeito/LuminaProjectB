using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int inventorySize = 3;
    public List<Item> items = new List<Item>();
    public int selectedIndex = 0;

    [Header("References")]
    public PlayerController player;
    public GameObject droppedItemPrefab;

    [Header("Effects")]
    public GameObject useItemParticlePrefab; // assign a particle prefab
    public Transform particleSpawnPoint;     // optional: above player

    [Header("UI")]
    public BuffUIManager buffUIManager;

    // Event to notify UI to refresh
    public event Action OnInventoryChanged;

    private void Start()
    {
        if (player == null)
            player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // Legacy keyboard input for testing
        if (items.Count > 0)
        {
            if (Keyboard.current.uKey.wasPressedThisFrame) UseItem();
            if (Keyboard.current.qKey.wasPressedThisFrame) DropItem();
        }
    }

    public bool AddItem(Item item)
    {
        if (items.Count >= inventorySize) return false;

        items.Add(item);

        // Ensure selectedIndex points to a valid item
        if (selectedIndex >= items.Count)
            selectedIndex = items.Count - 1;

        Debug.Log($"Picked up {item.itemName}");
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void UseItem()
    {
        if (items.Count == 0) return;

        Item item = items[selectedIndex];
        ApplyItemEffect(item);

        // Spawn particle effect
        if (useItemParticlePrefab != null)
        {
            Vector3 spawnPos = particleSpawnPoint != null ? particleSpawnPoint.position : transform.position;
            GameObject particle = Instantiate(useItemParticlePrefab, spawnPos, Quaternion.identity);

            ParticleSystem ps = particle.GetComponent<ParticleSystem>();
            if (ps != null)
                Destroy(particle, ps.main.duration + ps.main.startLifetime.constantMax);
            else
                Destroy(particle, 2f); // fallback
        }

        items.RemoveAt(selectedIndex);

        if (selectedIndex >= items.Count)
            selectedIndex = Mathf.Max(0, items.Count - 1);

        OnInventoryChanged?.Invoke();
    }

    public void DropItem()
    {
        if (items.Count == 0 || droppedItemPrefab == null) return;

        Item item = items[selectedIndex];
        GameObject go = Instantiate(droppedItemPrefab, transform.position, Quaternion.identity);
        go.GetComponent<ItemPickup>().item = item;

        items.RemoveAt(selectedIndex);

        if (selectedIndex >= items.Count)
            selectedIndex = Mathf.Max(0, items.Count - 1);

        OnInventoryChanged?.Invoke();
    }

    // -----------------------
    // Selection Methods
    // -----------------------
    public void NextItem()
    {
        if (items.Count == 0) return;
        selectedIndex = (selectedIndex + 1) % items.Count;
        OnInventoryChanged?.Invoke();
    }

    public void PreviousItem()
    {
        if (items.Count == 0) return;
        selectedIndex = (selectedIndex - 1 + items.Count) % items.Count;
        OnInventoryChanged?.Invoke();
    }

    public void OnNextItem(InputAction.CallbackContext context)
    {
        if (context.performed) NextItem();
    }

    public void OnPreviousItem(InputAction.CallbackContext context)
    {
        if (context.performed) PreviousItem();
    }

    // -----------------------
    // Item Effects
    // -----------------------
    private void ApplyItemEffect(Item item)
    {
        if (player == null) return;

        switch (item.type)
        {
            case ItemType.SpeedBoost:
                StartCoroutine(player.ApplySpeedBoost(item.duration, item.value));
                buffUIManager?.ShowBuff(item.type, item.duration);
                break;
            case ItemType.DoubleJump:
                StartCoroutine(player.ApplyDoubleJump(item.duration));
                buffUIManager?.ShowBuff(item.type, item.duration);
                break;
            case ItemType.LightShield:
                StartCoroutine(player.ApplyShield(item.duration));
                buffUIManager?.ShowBuff(item.type, item.duration);
                break;
            case ItemType.OrbMagnet:
                StartCoroutine(player.ApplyOrbMagnet(item.duration));
                buffUIManager?.ShowBuff(item.type, item.duration);
                break;
            case ItemType.HealthPotion:
                player.GetComponent<PlayerLight>()?.CollectLight();
                break;
        }
    }

    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();
}
