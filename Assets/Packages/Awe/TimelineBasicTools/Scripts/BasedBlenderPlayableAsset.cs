using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class BasedBlenderPlayable : PlayableBehaviour {
    public TimelineBasicTools.BasedBlender m_fader;
    public bool isFadeIn = true;
    public bool canDisableRenderer = true;
    public float minWeight, maxWeight = 0.0f;
    public float minWeight2nd, maxWeight2nd = 0.0f;
    public int fadeMode = 0;
    public AnimationCurve _timeMapCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // int graphIndex = -1;
	// // Called when the state of the playable is set to Play
    // public override void OnPlayableCreate(Playable playable){
        
    // }

    public float findWeight(float w, float minV = 0, float maxV = 0, bool isFI = true){
        w = (isFI) ? w : 1 - w;
        return minV + (maxV - minV) * w;
    }

    public override void OnGraphStart(Playable playable){
        base.OnGraphStart(playable);
        // if( GM.TimeLinePlayer.instance != null ){
        //     if( GM.TimeLinePlayer.instance._nowState == GM.PlayStatus._pause ){
        //         return;
        //     }
        // }

        if(m_fader == null){
            Debug.Log("You should setup the Object to Control!");
            return;
        }

        var m_parentPlayable = playable.GetOutput(0);
        var clipsNum = m_parentPlayable.GetInputCount();
        // if(!m_parentPlayable.IsPlayableOfType<BasedBlenderPlayable>()){
        //     Debug.LogError("Get BasedBlenderPlayable Error");
        // }

        for( int i=0 ; i<clipsNum ; i++ ) {
            var childClip = m_parentPlayable.GetInput(i);

            if( !playable.Equals(childClip) && childClip.GetPlayableType() == typeof(BasedBlenderPlayable) ){
                // Not the First Clip
                break;
            }

            if( playable.Equals(childClip) && childClip.GetPlayableType() == typeof(BasedBlenderPlayable) ){
                // Means First One of this type of clips
                Debug.Log($"[{i}/{clipsNum}] {m_fader.gameObject.name}: {playable.GetDuration()}");
                m_fader.blend_playable(
                    findWeight(_timeMapCurve.Evaluate(0), minWeight, maxWeight, isFadeIn), 
                    findWeight(_timeMapCurve.Evaluate(0), minWeight2nd, maxWeight2nd, isFadeIn), 
                    fadeMode);
                break;
            }
        }

        // if( playable.Equals(m_parentPlayable.GetInput(0)) ){
        //     // If it is First Clip: Set to first frame of this clip
        //     Debug.Log($"{m_fader.gameObject.name}: {playable.GetDuration()}");

            

        //     m_fader.blend_playable(
        //         findWeight(_timeMapCurve.Evaluate(0), minWeight, maxWeight, isFadeIn), 
        //         findWeight(_timeMapCurve.Evaluate(0), minWeight2nd, maxWeight2nd, isFadeIn), 
        //         fadeMode);
        // }
    }
    // public override void OnGraphStop(Playable playable){
    //     if(m_fader == null){
    //         Debug.Log("You should setup the Object to Control!");
    //         return;
    //     }
    //     
    //     // m_fader.fading_playable(1, isFadeIn, fadeMode);
    // }
    void humanEditorHandler(ref Playable playable){
        if( Application.isEditor && !Application.isPlaying ){
            var nTime = playable.GetTime() / playable.GetDuration();
            // Debug.Log("Pause:"+(float)nTime);
            if( nTime < 0.1 ){
                // Trigger Begin of Clip
                m_fader.blend_playable(
                    findWeight(_timeMapCurve.Evaluate(0), minWeight, maxWeight, isFadeIn), 
                    findWeight(_timeMapCurve.Evaluate(0), minWeight2nd, maxWeight2nd, isFadeIn), 
                    fadeMode);
            }else if( nTime > 0.9 ){
                // Trigger End of Clip
                m_fader.blend_playable(
                    findWeight(_timeMapCurve.Evaluate(1), minWeight, maxWeight, isFadeIn), 
                    findWeight(_timeMapCurve.Evaluate(1), minWeight2nd, maxWeight2nd, isFadeIn), 
                    fadeMode);
            }
        }
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info){
        // Debug.Log($"Play {(float)playable.GetTime()} | info: {info.deltaTime} || {info.effectivePlayState}");
        if(m_fader == null){
            Debug.Log("You should setup the Object to Control!");
            return;
        }

        if( info.effectivePlayState == PlayState.Playing && info.deltaTime > 0 ){
            // m_fader.reset();
            if( isFadeIn )
                m_fader.activeAllRenders(true);
            
            m_fader.blend_playable(
                findWeight(_timeMapCurve.Evaluate(0), minWeight, maxWeight, isFadeIn), 
                findWeight(_timeMapCurve.Evaluate(0), minWeight2nd, maxWeight2nd, isFadeIn), 
                fadeMode);
        }

#if UNITY_EDITOR
        humanEditorHandler(ref playable);
#endif
    }
    
    // Called when the state of the playable is set to Paused
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // Debug.Log("Pause:"+(float)playable.GetTime());
        if(m_fader == null){
            Debug.Log("You should setup the Object to Control!");
            return;
        }

#if UNITY_EDITOR
        humanEditorHandler(ref playable);
#endif

        var duration = playable.GetDuration();
        var count = playable.GetTime() + info.deltaTime;

        if ((info.effectivePlayState == PlayState.Paused && count > duration) || playable.GetGraph().GetRootPlayable(0).IsDone()){
            m_fader.blend_playable(
                findWeight(_timeMapCurve.Evaluate(1), minWeight, maxWeight, isFadeIn), 
                findWeight(_timeMapCurve.Evaluate(1), minWeight2nd, maxWeight2nd, isFadeIn), 
                fadeMode);

            if( !isFadeIn && canDisableRenderer ){
                m_fader.activeAllRenders(false);
            }
        }
	}

    public override void ProcessFrame(Playable playable, FrameData info, object playerData){
        if(m_fader == null){
            return;
        }
        
        float nTime = (float)playable.GetTime() / (float)playable.GetDuration();
        nTime = _timeMapCurve.Evaluate(nTime);
        // nTime = findWeight(nTime);

        m_fader.blend_playable(
            findWeight(_timeMapCurve.Evaluate(nTime), minWeight, maxWeight, isFadeIn), 
            findWeight(_timeMapCurve.Evaluate(nTime), minWeight2nd, maxWeight2nd, isFadeIn), 
            fadeMode);

        // base.PrepareFrame(playable, info);
    }
}
[System.Serializable]
public class BasedBlenderPlayableAsset : PlayableAsset
{
    public enum FadeSequence{
        FadeIn,
        FadeOut
    }

    // public ExposedReference<Text> m_DialogContainer;
    public ExposedReference<TimelineBasicTools.BasedBlender> m_fader;
    // public bool isFadeIn = true;
    public FadeSequence isFadeIn = FadeSequence.FadeIn;
    public bool canDisableRenderer = true;
    // public int fadeMode = 0;
    public TimelineBasicTools.FadeMode fadeMode;
    [Space(5)]
    // public int usedOrder = 0;
    // public TimelineBasicTools.FuncOrder usedOrder;
    [Space(5)]
    [Range(0, 1)]
    public float minWeight = 0.0f;
    [Range(0, 1)]
    public float maxWeight = 1.0f;
    [Range(0, 1)]
    public float minWeight2nd = 0.0f;
    [Range(0, 1)]
    public float maxWeight2nd = 0.0f;
    public bool withDefaultOrderVal = false;
    public AnimationCurve _timeMapCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var playable        = ScriptPlayable<BasedBlenderPlayable>.Create(graph);
        var behaviourObj    = playable.GetBehaviour();
        behaviourObj.m_fader                = m_fader.Resolve(graph.GetResolver());
        behaviourObj.isFadeIn               = (isFadeIn == FadeSequence.FadeIn) ? true : false;
        behaviourObj.canDisableRenderer     = canDisableRenderer;
        behaviourObj.fadeMode               = (int) fadeMode;
        behaviourObj.minWeight              = Mathf.Min(minWeight, maxWeight);
        behaviourObj.maxWeight              = Mathf.Max(minWeight, maxWeight);
        behaviourObj.minWeight2nd           = Mathf.Min(minWeight2nd, maxWeight2nd);
        behaviourObj.maxWeight2nd           = Mathf.Max(minWeight2nd, maxWeight2nd);
        behaviourObj._timeMapCurve          = _timeMapCurve;
        return playable;
    }
}

// [TrackColor(1f, 0f, 0f)]
// [TrackClipType(typeof(BasedBlenderPlayableAsset))]
// [TrackBindingType(typeof(TimelineBasicTools.BasedBlender))]
// public class BasedBlenderTrack : TrackAsset {
//     public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount){
//         return ScriptPlayable<BasedBlenderPlayable>.Create(graph, inputCount);
//     }
// }
