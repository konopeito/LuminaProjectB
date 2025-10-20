using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    SpeedBoost,
    DoubleJump,
    LightShield,
    OrbMagnet,
    HealthPotion
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public ItemType type;
    public float duration = 5f;       // Duration in seconds for buffs
    public Sprite icon;               // For inventory UI
    public float value = 1f;          // Optional effect value (e.g., speed multiplier, light restore)
}
