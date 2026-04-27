using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// The runtime behaviour (Logic)
public class AlphaFadePlayableBehaviour : PlayableBehaviour
{
    public AlphaFadeController fadeController;
    public float startAlpha = 0f;
    public float endAlpha = 1f;
    public AnimationCurve easeCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (fadeController == null) return;

        // Calculate progress (0 to 1)
        float duration = (float)playable.GetDuration();
        float time = (float)playable.GetTime();
        
        float progress = 0f;
        if (duration > 0)
        {
            progress = time / duration;
        }

        // Apply easing
        float curveValue = easeCurve.Evaluate(progress);

        // Lerp alpha
        float finalAlpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);

        // Apply to controller
        fadeController.Alpha = finalAlpha;
        
        // Force update in Edit mode or if not animating via standard Update
        if (!Application.isPlaying)
        {
            fadeController.UpdateRenderers();
        }
    }
}

// The asset (Data) - This is what you drag into Timeline
[System.Serializable]
public class AlphaFadePlayableAsset : PlayableAsset
{
    public ExposedReference<AlphaFadeController> fadeController;
    
    [Range(0f, 1f)]
    public float startAlpha = 1f;
    
    [Range(0f, 1f)]
    public float endAlpha = 0f;

    public AnimationCurve easeCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<AlphaFadePlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        // Resolve reference
        behaviour.fadeController = fadeController.Resolve(graph.GetResolver());
        behaviour.startAlpha = startAlpha;
        behaviour.endAlpha = endAlpha;
        behaviour.easeCurve = easeCurve;

        return playable;
    }
}
