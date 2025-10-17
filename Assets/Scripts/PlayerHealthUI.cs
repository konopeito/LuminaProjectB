using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Heart UI Settings")]
    public Image[] hearts;           // Assign 3 heart Images in inspector
    public Sprite[] heartStages;     // 5 stages: full -> empty
    public float fadeDuration = 1f;
    public float popupScale = 1.3f;

    [Header("Follow Settings")]
    public Transform player;         // Assign the Player GameObject
    public Vector3 offset = new Vector3(0, 2f, 0); // Above the head
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

        // Smoothly follow player with offset
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
    /// Animate all hearts depleting and fading out above the player.
    /// </summary>
    public IEnumerator AnimateAllHeartsDeplete()
    {
        if (hearts == null || hearts.Length == 0) yield break;

        int emptyStage = heartStages.Length - 1;
        foreach (Image heart in hearts)
            heart.sprite = heartStages[emptyStage];

        transform.localScale = originalScale * popupScale;
        canvasGroup.alpha = 1f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        transform.localScale = originalScale;
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Reset hearts to full and hide UI.
    /// </summary>
    public void ResetHearts()
    {
        if (hearts == null || hearts.Length == 0 || heartStages == null || heartStages.Length == 0)
            return;

        foreach (Image heart in hearts)
            heart.sprite = heartStages[0];

        transform.localScale = originalScale;
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Optional: animate a single heart depleting for individual damage events.
    /// </summary>
    public IEnumerator AnimateHeartDeplete(int heartIndex)
    {
        if (hearts == null || hearts.Length == 0 || heartIndex < 0 || heartIndex >= hearts.Length) yield break;

        int emptyStage = heartStages.Length - 1;
        hearts[heartIndex].sprite = heartStages[emptyStage];

        transform.localScale = originalScale * popupScale;
        canvasGroup.alpha = 1f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        transform.localScale = originalScale;
        canvasGroup.alpha = 0f;
    }
}
