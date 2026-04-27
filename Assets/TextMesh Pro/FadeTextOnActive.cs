using UnityEngine;
using System.Collections;

public class FadeTMPOnActive : MonoBehaviour
{
    CanvasGroup canvasGroup;

    public float fadeInTime = 1.5f;
    public float stayTime = 3f;
    public float fadeOutTime = 1.5f;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (canvasGroup == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        canvasGroup.alpha = 0;

        // Fade In
        float t = 0;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / fadeInTime;
            yield return null;
        }

        canvasGroup.alpha = 1;

        yield return new WaitForSeconds(stayTime);

        // Fade Out
        t = 0;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1 - (t / fadeOutTime);
            yield return null;
        }

        canvasGroup.alpha = 0;
    }
}

