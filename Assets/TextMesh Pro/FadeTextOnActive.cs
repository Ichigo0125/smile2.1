using UnityEngine;
using System.Collections;

public class FadeTMPOnActive : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    public float fadeInTime = 1.5f;
    public float stayTime = 3f;
    public float fadeOutTime = 1.5f;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning($"{nameof(FadeTMPOnActive)}: no CanvasGroup on {gameObject.name}", this);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator FadeRoutine()
    {
        canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeInTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(stayTime);

        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}