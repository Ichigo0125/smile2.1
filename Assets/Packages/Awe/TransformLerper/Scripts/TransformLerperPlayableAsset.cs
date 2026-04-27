using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace PlayerCtr {
    public class TransformLerperPlayable : PlayableBehaviour {
        public PlayerCtr.transformLerper m_transL;
        public bool activeInterMove = false;
        public int playIndex = 1;
        public bool isInverse = false;
        public AnimationCurve _timeMapCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float speedMultiplier = 1f;
        public float maxProgress = 1f;

    	// Called when the state of the playable is set to Play
        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            // Debug.Log("Play");
            if(m_transL == null){
                Debug.Log("You should setup the Object to Control!");
                return;
            }

            m_transL.initialIndex(playIndex);
        }
        
        // Called when the state of the playable is set to Paused
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // Debug.Log("Pause:"+(float)playable.GetTime()+" "+playable.GetLeadTime());
            double getTime = playable.GetTime();
            float iniTime = 0;
            if( isInverse ){
                iniTime = 1;
            }
            if(getTime < 0.2){
                m_transL.playableCtr( iniTime );
            }else if(getTime > playable.GetDuration()-0.2){
                m_transL.playableCtr( 1.0f - iniTime );
            }
    	}

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if(m_transL == null){
                return;
            }
            
            float nTime = (float)playable.GetTime() / (float)playable.GetDuration();
            nTime *= Mathf.Max(0f, speedMultiplier);
            nTime = Mathf.Clamp01(nTime);
            if (maxProgress < 1f) {
                nTime = Mathf.Min(nTime, Mathf.Clamp01(maxProgress));
            }
            if(isInverse){
                nTime = 1.0f - nTime;
            }
            nTime = _timeMapCurve.Evaluate(nTime);
            m_transL.playableCtr(playIndex, nTime);

            base.PrepareFrame(playable, info);
        }
    }

    // https://docs.unity3d.com/ScriptReference/PropertyDrawer.html
    // https://answers.unity.com/questions/1585678/how-to-edit-arraylist-property-with-custom-propert.html

    [System.Serializable]
    public class TransformLerperPlayableAsset : PlayableAsset
    {
        // public ExposedReference<Text> m_DialogContainer;
        public ExposedReference<PlayerCtr.transformLerper> m_transL;
        public bool activeInterMove = false;
        public int playIndex = 1;
        public bool isInverse = false;

        [Header("Speed Control")]
        [Min(0f)]
        public float speedMultiplier = 1f;
        [Range(0f, 1f)]
        public float maxProgress = 1f;

        [Header("Time Mapping Curve")]
        public AnimationCurve _timeMapCurve = AnimationCurve.Linear(0, 0, 1, 1);
        // public PlayerCtr.MoveParam moveParam;
    
        // Factory method that generates a playable based on this asset
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<TransformLerperPlayable>.Create(graph);
            playable.GetBehaviour().m_transL = m_transL.Resolve(graph.GetResolver());
            playable.GetBehaviour().activeInterMove = activeInterMove;
            playable.GetBehaviour().playIndex = playIndex;
            playable.GetBehaviour().isInverse = isInverse;
            playable.GetBehaviour().speedMultiplier = speedMultiplier;
            playable.GetBehaviour().maxProgress = maxProgress;
            playable.GetBehaviour()._timeMapCurve = _timeMapCurve;
            return playable;
        }

        public float findNTime(){
            return _timeMapCurve.Evaluate(isInverse ? 1.0f : 0.0f);
        }

    }

}
