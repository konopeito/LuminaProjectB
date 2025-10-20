using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffUIManager : MonoBehaviour
{
    [System.Serializable]
    public struct BuffIcon
    {
        public ItemType type;
        public Image iconImage;
    }

    public List<BuffIcon> buffIcons; // assign icons in inspector

    private Dictionary<ItemType, Coroutine> activeBuffs = new Dictionary<ItemType, Coroutine>();

    [Header("Pulse Settings")]
    public float pulseFrequency = 4f; // how fast it pulses
    public float pulseIntensity = 0.5f; // alpha variation amount (0-1)

    public void ShowBuff(ItemType type, float duration)
    {
        BuffIcon buff = buffIcons.Find(b => b.type == type);
        if (buff.iconImage == null) return;

        // Stop existing coroutine if buff already active
        if (activeBuffs.TryGetValue(type, out Coroutine existing))
            StopCoroutine(existing);

        Coroutine c = StartCoroutine(ShowBuffCoroutine(buff.iconImage, duration));
        activeBuffs[type] = c;
    }

    private IEnumerator ShowBuffCoroutine(Image icon, float duration)
    {
        icon.gameObject.SetActive(true);

        float elapsed = 0f;
        float flashStartTime = duration * 0.75f; // start pulsing in last 25%
        Color baseColor = icon.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Fade alpha over entire duration
            float fadeAlpha = Mathf.Clamp01(1f - (elapsed / duration));

            // Pulse effect in last 25%
            if (elapsed >= flashStartTime)
            {
                float pulse = Mathf.Sin((elapsed - flashStartTime) * Mathf.PI * 2f * pulseFrequency) * pulseIntensity;
                fadeAlpha = Mathf.Clamp01(fadeAlpha + pulse);
            }

            icon.color = new Color(baseColor.r, baseColor.g, baseColor.b, fadeAlpha);
            yield return null;
        }

        icon.gameObject.SetActive(false);
        activeBuffs.Remove(buffIcons.Find(b => b.iconImage == icon).type);
    }
}
