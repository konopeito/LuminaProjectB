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

    [Header("Damage Feedback")]
    public float hurtFadeAmount = 0.25f;
    public float hurtFadeDuration = 0.25f;

    [Header("UI")]
    public PlayerHealthUI healthUI;

    private SpriteRenderer lightSprite;
    private float currentIntensity;
    private float targetScale;
    private bool isDead = false;
    private bool isFadingFromHurt = false;

    void Awake()
    {
        if (lightCircle == null)
        {
            Debug.LogError("Assign a lightCircle GameObject!");
            return;
        }

        lightSprite = lightCircle.GetComponent<SpriteRenderer>();
        if (lightSprite == null)
            Debug.LogError("lightCircle is missing a SpriteRenderer!");

        currentIntensity = maxIntensity;
        targetScale = maxScale;

        // Ensure healthUI reference
        if (healthUI == null)
            healthUI = FindObjectOfType<PlayerHealthUI>();

        UpdateLight();
    }

    void Update()
    {
        if (isDead || (GameMenusManager.Instance != null && GameMenusManager.Instance.isPaused))
            return;

        // Dim light
        currentIntensity = Mathf.Clamp(currentIntensity - dimSpeed * Time.deltaTime, minIntensity, maxIntensity);
        targetScale = Mathf.Lerp(minScale, maxScale, currentIntensity / maxIntensity);
        SmoothScale();
        UpdateLight();
        UpdateUI();

        if (currentIntensity <= minIntensity && !isDead)
            Die(false);
    }

    private void UpdateUI()
    {
        if (healthUI == null) return;

        float percentPerHeart = 1f / 3f;
        for (int i = 0; i < 3; i++)
        {
            float heartPercent = Mathf.Clamp01((currentIntensity - (percentPerHeart * i)) / percentPerHeart);
            healthUI.UpdateHeart(i, heartPercent);
        }
    }

    public void CollectLight()
    {
        if (isDead) return;

        currentIntensity = Mathf.Clamp(currentIntensity + collectAmount, minIntensity, maxIntensity);
        targetScale = Mathf.Min(maxScale * collectPulseScale, maxScale * 1.3f);
    }

    public void OnPlayerHurt()
    {
        if (isDead || isFadingFromHurt) return;
        StartCoroutine(HurtFlash());
    }

    private IEnumerator HurtFlash()
    {
        isFadingFromHurt = true;
        float original = currentIntensity;
        float dimmed = Mathf.Max(minIntensity, currentIntensity - hurtFadeAmount);

        float t = 0f;
        while (t < hurtFadeDuration)
        {
            t += Time.deltaTime;
            currentIntensity = Mathf.Lerp(original, dimmed, t / hurtFadeDuration);
            UpdateLight();
            yield return null;
        }

        t = 0f;
        while (t < hurtFadeDuration)
        {
            t += Time.deltaTime;
            currentIntensity = Mathf.Lerp(dimmed, original, t / hurtFadeDuration);
            UpdateLight();
            yield return null;
        }

        currentIntensity = original;
        UpdateLight();
        isFadingFromHurt = false;
    }

    private void UpdateLight()
    {
        if (lightSprite != null)
        {
            Color c = lightSprite.color;
            c.a = currentIntensity;
            lightSprite.color = c;
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

        // Show empty hearts immediately
        healthUI?.ShowEmptyHearts();

        GameMenusManager.Instance?.TriggerGameOver();

        if (isPitDeath)
        {
            GetComponent<PlayerController>()?.Respawn();
        }
    }

    private IEnumerator FadeOutLight()
    {
        float duration = 1f;
        float startIntensity = currentIntensity;
        float startScale = lightCircle.transform.localScale.x;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            currentIntensity = Mathf.Lerp(startIntensity, 0f, t);
            targetScale = Mathf.Lerp(startScale, minScale * 0.3f, t);

            UpdateLight();
            SmoothScale();
            yield return null;
        }

        currentIntensity = 0f;
        targetScale = minScale * 0.3f;
        UpdateLight();
        SmoothScale();
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
