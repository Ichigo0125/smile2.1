using UnityEngine;
using System.Collections;

public class StaffFadeOutMonoBehaviour : MonoBehaviour
{
    [Header("Fade Settings")]
    [Min(0f)]
    public float fadeDuration = 2f;

    [Tooltip("Shader 的 _Fade 最大值，例如 2。")]
    [Min(0f)]
    public float maxFade = 2f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int FadeID = Shader.PropertyToID("_Fade");

    private float currentFade = 0f;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();

        // 初始為 0
        SetFade(0f);
    }

    /// <summary>
    /// 收到 signal 後呼叫這個函式
    /// </summary>
    public void ReceiveSignal()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        float startFade = currentFade;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / fadeDuration);
            float value = Mathf.Lerp(startFade, maxFade, t);

            SetFade(value);

            yield return null;
        }

        SetFade(maxFade);
    }

    /// <summary>
    /// 直接設定 Shader 的 _Fade
    /// </summary>
    public void SetFade(float value)
    {
        currentFade = Mathf.Clamp(value, 0f, maxFade);

        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FadeID, currentFade);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
