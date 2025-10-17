using UnityEngine;
using System.Collections;

public class PlayerLight : MonoBehaviour
{
    [Header("Light Settings")]
    public GameObject lightCircle;
    public float maxIntensity = 1f;
    public float minIntensity = 0f;
    public float dimSpeed = 0.1f;

    [Header("Scaling Settings")]
    public float maxScale = 1.5f;
    public float minScale = 0.5f;
    public float scaleSpeed = 5f;

    [Header("Light Orb Settings")]
    public float collectAmount = 0.5f;
    public float collectPulseScale = 1.2f;

    [Header("UI")]
    public PlayerHealthUI healthUI;

    private SpriteRenderer lightSprite;
    private float currentIntensity;
    private float targetScale;
    private bool isDead = false;

    void Start()
    {
        if (lightCircle == null)
        {
            Debug.LogError("Assign a lightCircle GameObject!");
            return;
        }

        lightSprite = lightCircle.GetComponent<SpriteRenderer>();
        currentIntensity = maxIntensity;
        targetScale = maxScale;
        UpdateLight();
    }

    void Update()
    {
        if (isDead || (GameMenusManager.Instance != null && GameMenusManager.Instance.isPaused)) return;

        currentIntensity -= dimSpeed * Time.deltaTime;
        currentIntensity = Mathf.Clamp(currentIntensity, minIntensity, maxIntensity);

        UpdateLight();
        targetScale = Mathf.Lerp(minScale, maxScale, currentIntensity / maxIntensity);
        SmoothScale();

        if (healthUI != null)
        {
            float percentPerHeart = 1f / 3f;
            for (int i = 0; i < 3; i++)
            {
                float heartPercent = Mathf.Clamp01((currentIntensity - (percentPerHeart * i)) / percentPerHeart);
                healthUI.UpdateHeart(i, heartPercent);
            }
        }

        if (currentIntensity <= minIntensity)
            Die(false);
    }

    public void CollectLight()
    {
        if (isDead) return;

        currentIntensity += collectAmount;
        currentIntensity = Mathf.Clamp(currentIntensity, minIntensity, maxIntensity);
        targetScale = Mathf.Min(maxScale * collectPulseScale, maxScale * 1.3f);
    }

    private void UpdateLight()
    {
        if (lightSprite != null)
        {
            Color color = lightSprite.color;
            color.a = currentIntensity;
            lightSprite.color = color;
        }
    }

    private void SmoothScale()
    {
        if (lightCircle == null) return;

        float currentScale = lightCircle.transform.localScale.x;
        float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleSpeed);
        lightCircle.transform.localScale = new Vector3(newScale, newScale, 1f);

        if (targetScale > maxScale)
            targetScale = Mathf.Lerp(targetScale, maxScale, Time.deltaTime * (scaleSpeed * 0.5f));
    }

    public void Die(bool isPitDeath)
    {
        if (isDead) return;
        isDead = true;

        GetComponent<Animator>()?.SetTrigger("Death");

        // Empty hearts immediately
        healthUI?.ResetHearts();

        // Trigger Game Over
        GameMenusManager.Instance?.TriggerGameOver();

        if (isPitDeath)
        {
            GetComponent<PlayerController>()?.Respawn();
            return;
        }

        // Don't respawn immediately for normal deaths
        // Respawn will be triggered from Game Over menu if desired
    }


    public void ResetLight()
    {
        isDead = false;
        healthUI?.ResetHearts();
        if (healthUI != null)
            healthUI.gameObject.SetActive(true);
        StartCoroutine(FadeInLight());
    }

    private IEnumerator FadeInLight()
    {
        float duration = 1.2f;
        float timer = 0f;

        float startIntensity = 0f;
        float startScale = minScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            currentIntensity = Mathf.Lerp(startIntensity, maxIntensity, t);
            targetScale = Mathf.Lerp(startScale, maxScale, t);

            UpdateLight();
            SmoothScale();

            yield return null;
        }

        currentIntensity = maxIntensity;
        targetScale = maxScale;
        UpdateLight();
        SmoothScale();
    }
}
