using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GM{
    [RequireComponent(typeof(PlayableDirector))]
    public class FadeOutTrack : MonoBehaviour
    {
        public bool startFade = false;
        public bool manualIni = false;
        public float fadeTime = 2;

        private PlayableDirector playableDirector;
        private AnimationPlayableOutput output;

        // Assume playOnAwake on the playabledirector
        void Start ()
        {
            SomeIni();
        }

        void SomeIni(){
            Debug.Log("@O@?");
            playableDirector = GetComponent<PlayableDirector>();
            Debug.Log(playableDirector.playableGraph.IsValid());
            if (playableDirector.playableGraph.IsValid())
            {
                // assumes the first output is the one we want to fade
                var oldOutput = (AnimationPlayableOutput) playableDirector.playableGraph.GetOutputByType<AnimationPlayableOutput>(0);
                Debug.Log(oldOutput);
                if (oldOutput.IsOutputValid() && oldOutput.GetTarget() != null)
                {
                    Debug.Log(oldOutput.GetTarget().name);
                    // create a new output to replace the existing
                    output = AnimationPlayableOutput.Create(playableDirector.playableGraph, "fake", oldOutput.GetTarget());
                    var playable = oldOutput.GetSourcePlayable();
                    var port = oldOutput.GetSourceOutputPort();
                    oldOutput.SetSourcePlayable(Playable.Null, -1);
                    output.SetSourcePlayable(playable, port);
                    output.SetWeight(1.0f);
                    oldOutput.SetTarget(null);
                }
            }
        }

        // Update is called once per frame
        void Update () {
            if( manualIni ){
                manualIni = false;
                SomeIni();
            }
            
            if (startFade)
            {
                startFade = false;
                if (output.IsOutputValid())
                    StartCoroutine(FadeOut());
            }
        }

        IEnumerator FadeOut()
        {
            float t = 0;
            while (t < fadeTime)
            {
                float weight = 1 - Mathf.Clamp01(t / fadeTime);
                output.SetWeight(weight);
                yield return null;
                t += Time.deltaTime;
            }
            playableDirector.Stop();
        }
    }
}
