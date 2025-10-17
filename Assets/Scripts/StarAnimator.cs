using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StarAnimator : MonoBehaviour
{
    public Image targetImage; // assign your UI Image
    public Sprite[] stages;   // frames of the star animation
    public float frameTime = 0.2f; // time per frame
    public bool loop = true;  // loop animation

    private Coroutine animationCoroutine;

    void Start()
    {
        if (targetImage == null)
        {
            Debug.LogError("StarAnimator: Assign the Image!");
            return;
        }

        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("StarAnimator: Assign at least one sprite stage!");
            return;
        }

        // Start animating immediately
        animationCoroutine = StartCoroutine(AnimateStar());
    }

    private IEnumerator AnimateStar()
    {
        int index = 0;
        while (true)
        {
            targetImage.sprite = stages[index];
            index++;
            if (index >= stages.Length)
            {
                if (loop)
                    index = 0;
                else
                    yield break;
            }
            yield return new WaitForSeconds(frameTime);
        }
    }

    
    public void PlayAnimation()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateStar());
    }
}
