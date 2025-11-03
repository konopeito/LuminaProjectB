using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHearts = 3;          // Number of hearts
    public int currentHearts;          // Current hearts
    public PlayerHealthUI healthUI;    // Reference to your existing UI script

    [Header("Poison Settings")]
    public bool isPoisoned = false;

    void Start()
    {
        currentHearts = maxHearts;

        // Reset hearts visually on start
        if (healthUI != null)
            healthUI.ResetHearts();
    }

    public void TakeDamage(int amount)
    {
        if (currentHearts <= 0) return;

        for (int i = 0; i < amount; i++)
        {
            currentHearts--;
            currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);

            if (healthUI != null && currentHearts < healthUI.hearts.Length)
                StartCoroutine(healthUI.AnimateHeartDeplete(currentHearts));

            if (currentHearts <= 0)
            {
                Die();
                break;
            }
        }
    }

    public void Heal(int amount)
    {
        currentHearts += amount;
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);

        if (healthUI != null)
        {
            for (int i = 0; i < currentHearts; i++)
            {
                healthUI.UpdateHeart(i, 0f); // 0f = full heart
            }
        }
    }

    public void ApplyPoison(float duration)
    {
        if (!isPoisoned)
        {
            // Start visual poison effect
            if (healthUI != null)
                healthUI.StartCoroutine(healthUI.PoisonHeartEffect(duration));

            // Start poison damage over time
            StartCoroutine(PoisonCoroutine(duration));
        }
    }

    IEnumerator PoisonCoroutine(float duration)
    {
        isPoisoned = true;
        float timer = 0f;

        while (timer < duration && currentHearts > 0)
        {
            TakeDamage(1); // 1 heart per second
            yield return new WaitForSeconds(1f);
            timer += 1f;
        }

        isPoisoned = false;
    }

    void Die()
    {
        Debug.Log("Player died!");

        if (healthUI != null)
            StartCoroutine(healthUI.AnimateAllHeartsDeplete());

        // Additional death logic (disable movement, trigger death animation, etc.)
    }
}
