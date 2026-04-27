using UnityEngine;

[ExecuteAlways]
public class AlphaFadeController : MonoBehaviour
{
    [Range(0f, 1f)]
    public float Alpha = 1.0f;

    [SerializeField]
    private Renderer[] _linkedRenderers;
    
    private static readonly int MasterAlphaId = Shader.PropertyToID("_MasterAlpha");
    private MaterialPropertyBlock _propBlock;

    void OnEnable()
    {
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        UpdateRenderers();
    }

    public void UpdateRenderers()
    {
        if (_linkedRenderers == null || _linkedRenderers.Length == 0) return;
        
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

        bool isVisible = Alpha > 0.01f;

        foreach (var renderer in _linkedRenderers)
        {
            if (renderer == null) continue;

            // Optimization: Disable renderer if fully transparent to avoid "Invisible Ghost" depth blocking
            // and save performance.
            if (renderer.enabled != isVisible) renderer.enabled = isVisible;

            if (isVisible)
            {
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(MasterAlphaId, Alpha);
                renderer.SetPropertyBlock(_propBlock);
            }
        }
    }

    [ContextMenu("Find Child Renderers")]
    public void FindChildRenderers()
    {
        _linkedRenderers = GetComponentsInChildren<Renderer>(true);
    }
}
