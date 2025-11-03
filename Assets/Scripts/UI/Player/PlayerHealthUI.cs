using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Heart UI Settings")]
    public Image[] hearts;       // Assign 3 heart Images in inspector
    public Sprite[] heartStages; // 5 stages: full -> empty
    public float fadeDuration = 1f;
    public float popupScale = 1.3f;

    [Header("Follow Settings")]
    public Transform player;
    public Vector3 offset = new Vector3(0, 2f, 0);
    public float followSpeed = 5f;

    private Vector3 originalScale;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (hearts == null || hearts.Length == 0)
            Debug.LogError("Assign heart images in PlayerHealthUI!");
        if (heartStages == null || heartStages.Length == 0)
            Debug.LogError("Assign heart stage sprites in PlayerHealthUI!");

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalScale = transform.localScale;
        canvasGroup.alpha = 0f;

        // Auto-assign player if missing
        if (player == null)
            player = FindObjectOfType<PlayerController>()?.transform;
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
    }

    public void UpdateHeart(int heartIndex, float lightPercent)
    {
        if (hearts == null || hearts.Length == 0 || heartStages == null || heartStages.Length == 0)
            return;

        heartIndex = Mathf.Clamp(heartIndex, 0, hearts.Length - 1);
        int stageIndex = Mathf.Clamp(Mathf.RoundToInt((1 - lightPercent) * (heartStages.Length - 1)), 0, heartStages.Length - 1);
        hearts[heartIndex].sprite = heartStages[stageIndex];
    }

    /// <summary>
    /// Show all hearts as empty immediately (used on player death)
    /// </summary>
    public void ShowEmptyHearts()
    {
        if (hearts == null || hearts.Length == 0 || heartStages == null || heartStages.Length == 0)
            return;

        int emptyStage = heartStages.Length - 1;
        foreach (Image heart in hearts)
            if (heart != null) heart.sprite = heartStages[emptyStage];

        transform.localScale = originalScale;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Reset hearts to full and hide UI (used on respawn)
    /// </summary>
    public void ResetHearts()
    {
        if (hearts == null || hearts.Length == 0 || heartStages == null || heartStages.Length == 0)
            return;

        foreach (Image heart in hearts)
            if (heart != null) heart.sprite = heartStages[0];

        transform.localScale = originalScale;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public IEnumerator AnimateAllHeartsDeplete()
    {
        if (hearts == null || hearts.Length == 0) yield break;

        int emptyStage = heartStages.Length - 1;
        foreach (Image heart in hearts)
            if (heart != null) heart.sprite = heartStages[emptyStage];

        transform.localScale = originalScale * popupScale;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t / fadeDuration);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        transform.localScale = originalScale;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public IEnumerator AnimateHeartDeplete(int heartIndex)
    {
        if (hearts == null || hearts.Length == 0 || heartIndex < 0 || heartIndex >= hearts.Length) yield break;

        int emptyStage = heartStages.Length - 1;
        if (hearts[heartIndex] != null) hearts[heartIndex].sprite = heartStages[emptyStage];

        transform.localScale = originalScale * popupScale;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t / fadeDuration);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        transform.localScale = originalScale;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    // ==================== Poison Heart Extension ====================
    [Header("Poison Settings")]
    public Color poisonColor = new Color(0.6f, 0f, 0.6f, 1f); // purple tint
    public float poisonPulseSpeed = 2f; // speed of pulse effect

    private bool isPoisoned = false;
    private float poisonTimer = 0f;

    void Update()
    {
        // Existing LateUpdate() behavior runs above
        HandlePoisonPulse();
    }

    public void StartPoisonEffect(float duration)
    {
        if (!isPoisoned)
            StartCoroutine(PoisonCoroutine(duration));
    }

    IEnumerator PoisonCoroutine(float duration)
    {
        isPoisoned = true;
        poisonTimer = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isPoisoned = false;
        ResetHeartColors();
    }

    void HandlePoisonPulse()
    {
        if (!isPoisoned || hearts == null) return;

        // Pulsing purple effect
        float pulse = (Mathf.Sin(Time.time * poisonPulseSpeed) + 1f) / 2f; // 0 → 1
        Color tint = Color.Lerp(Color.white, poisonColor, pulse);

        foreach (var heart in hearts)
        {
            if (heart != null)
                heart.color = tint;
        }
    }

    void ResetHeartColors()
    {
        if (hearts == null) return;
        foreach (var heart in hearts)
        {
            if (heart != null)
                heart.color = Color.white;
        }
    }
    public IEnumerator PoisonHeartEffect(float duration)
    {
        if (hearts == null || hearts.Length == 0) yield break;

        float timer = 0f;
        Color poisonColor = new Color(0.6f, 0f, 0.6f, 1f); // Purple tint

        // Capture original colors to restore later
        Color[] originalColors = new Color[hearts.Length];
        for (int i = 0; i < hearts.Length; i++)
            if (hearts[i] != null) originalColors[i] = hearts[i].color;

        // Random phase offsets for asynchronous pulsing
        float[] phaseOffsets = new float[hearts.Length];
        for (int i = 0; i < hearts.Length; i++)
            phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] != null)
                {
                    // Only pulse hearts that are not fully empty
                    int stageIndex = System.Array.IndexOf(heartStages, hearts[i].sprite);
                    if (stageIndex >= 0 && stageIndex < heartStages.Length - 1)
                    {
                        float pulse = (Mathf.Sin(Time.time * 4f + phaseOffsets[i]) + 1f) / 2f;
                        hearts[i].color = Color.Lerp(originalColors[i], poisonColor, pulse);
                    }
                }
            }
            yield return null;
        }

        // Reset all heart colors
        for (int i = 0; i < hearts.Length; i++)
            if (hearts[i] != null)
                hearts[i].color = originalColors[i];
    }



}
