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
    public float scaleSpeed = 2f;

    [Header("Light Orb Settings")]
    public float collectAmount = 0.5f;
    public float collectPulseScale = 1.2f;

    [Header("GameOver")]
    public GameOverManager gameOverManager; // Assign in Inspector

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
        if (isDead || (gameOverManager != null && gameOverManager.isPaused)) return;

        DimLight();
        SmoothScale();

        if (currentIntensity <= minIntensity)
            Die();
    }

    private void DimLight()
    {
        currentIntensity -= dimSpeed * Time.deltaTime;
        currentIntensity = Mathf.Clamp(currentIntensity, minIntensity, maxIntensity);
        UpdateLight();
        targetScale = Mathf.Lerp(minScale, maxScale, currentIntensity / maxIntensity);
    }

    public void CollectLight()
    {
        if (isDead) return;

        currentIntensity += collectAmount;
        currentIntensity = Mathf.Clamp(currentIntensity, minIntensity, maxIntensity);
        UpdateLight();
        targetScale = maxScale * collectPulseScale;
    }

    private void UpdateLight()
    {
        Color color = lightSprite.color;
        color.a = currentIntensity;
        lightSprite.color = color;
    }

    private void SmoothScale()
    {
        if (lightCircle != null)
        {
            float newScale = Mathf.Lerp(lightCircle.transform.localScale.x, targetScale, Time.deltaTime * scaleSpeed);
            lightCircle.transform.localScale = new Vector3(newScale, newScale, 1f);

            if (targetScale > maxScale)
                targetScale = Mathf.Lerp(targetScale, maxScale, Time.deltaTime * scaleSpeed);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetTrigger("Death");

        StartCoroutine(ShowGameOverAfterDeath(anim));
    }

    private IEnumerator ShowGameOverAfterDeath(Animator anim)
    {
        float animLength = 0.5f;
        if (anim != null)
        {
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            animLength = state.length;
        }

        float timer = 0f;
        while (timer < animLength)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (gameOverManager != null)
            gameOverManager.TriggerGameOver();
    }
}
