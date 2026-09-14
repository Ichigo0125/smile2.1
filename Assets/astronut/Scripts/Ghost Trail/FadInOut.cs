using UnityEngine;

// 控制 GameObject 在指定生命周期内逐渐进行 Dither Fade 并销毁
public class FadInOut : MonoBehaviour
{
    [Min(0f)]
    public float lifeCycle = 2.0f;

    [Tooltip("Shader 的 _Fade 最大值，例如 DitherLit 是 0~2。")]
    [Min(0f)]
    public float maxFade = 2.0f;

    private static readonly int FadeID = Shader.PropertyToID("_Fade");

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private float startTime;

    void Start()
    {
        startTime = Time.time;
        renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();

        SetFade(0f);
    }

    void Update()
    {
        if (lifeCycle <= 0f)
        {
            SetFade(maxFade);
            Destroy(gameObject);
            return;
        }

        float elapsed = Time.time - startTime;
        SetFade(Mathf.Clamp01(elapsed / lifeCycle) * maxFade);

        if (elapsed >= lifeCycle)
        {
            Destroy(gameObject);
        }
    }

    private void SetFade(float value)
    {
        float shaderFade = Mathf.Clamp(value, 0f, maxFade);

        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FadeID, shaderFade);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
