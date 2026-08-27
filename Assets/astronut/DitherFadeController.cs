using UnityEngine;
using System.Collections;

public class DitherFadeController : MonoBehaviour
{
    [Header("Fade Settings")]
    [Min(0f)]
    public float fadeDuration = 2f;

    [Tooltip("Shader 的 _Fade 最大值，例如你的 Shader 是 0~2 就填 2。")]
    [Min(0f)]
    public float maxFade = 2f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int FadeID = Shader.PropertyToID("_Fade");

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();

        // 初始隱藏
        SetFade(0f);
    }

    void OnEnable()
    {
        FadeIn();
    }

    /// <summary>
    /// 開始淡入
    /// </summary>
    public void FadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / fadeDuration);
            float value = Mathf.Lerp(0f, maxFade, t);

            SetFade(value);

            yield return null;
        }

        SetFade(maxFade);
    }

    /// <summary>
    /// 設定所有 Renderer 的 Fade
    /// </summary>
    public void SetFade(float value)
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(FadeID, value);

            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}