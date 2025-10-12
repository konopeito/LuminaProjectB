using UnityEngine;

public class OrbGlowPulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 0.8f;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        Color color = sr.color;
        color.a = alpha;
        sr.color = color;
    }
}
