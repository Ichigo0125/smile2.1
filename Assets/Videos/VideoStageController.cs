using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using System.Collections;

public class MultiPlayerStageController : MonoBehaviour
{
    [System.Serializable]
    public class Stage
    {
        public VideoClip videoForThisStage;
        public float delayAfterEnd = 3f;
    }

    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Stages (0=Stage0, 1=Stage1)")]
    public Stage[] stages;

    [Header("Animator Switch Settings")]
    public Animator targetAnimator;
    public RuntimeAnimatorController out0001Controller;

    private RuntimeAnimatorController originalController;

    [Header("Timeline")]
    public PlayableDirector timeline3;

    private int currentStage = 0;

    void Start()
    {
        if (targetAnimator != null)
            originalController = targetAnimator.runtimeAnimatorController;

        if (timeline3 != null)
            timeline3.Stop();

        PlayStage(0);
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // Stage0 → Stage1
            if (currentStage == 0)
            {
                videoPlayer.Stop();
                StopAllCoroutines();
                PlayStage(1);
            }

            // Stage1 → Timeline
            else if (currentStage == 1)
            {
                videoPlayer.Stop();
                StopAllCoroutines();

                if (timeline3 != null)
                    timeline3.Play();
            }
        }
    }

    void PlayStage(int index)
    {
        if (stages == null || stages.Length == 0)
            return;

        if (index >= stages.Length)
            return;

        currentStage = index;
        Stage stage = stages[index];

        // Stage1 切 Animator
        if (index == 1 && targetAnimator != null && out0001Controller != null)
        {
            targetAnimator.runtimeAnimatorController = out0001Controller;
        }
        else if (targetAnimator != null && originalController != null)
        {
            targetAnimator.runtimeAnimatorController = originalController;
        }

        PlayVideo(stage.videoForThisStage);
    }

    void PlayVideo(VideoClip clip)
    {
        if (clip == null)
        {
            StartCoroutine(WaitAndNext(stages[currentStage].delayAfterEnd));
            return;
        }

        videoPlayer.loopPointReached -= HandleVideoEnd;
        videoPlayer.loopPointReached += HandleVideoEnd;

        videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    void HandleVideoEnd(VideoPlayer vp)
    {
        videoPlayer.loopPointReached -= HandleVideoEnd;

        // Stage1 播完 → Timeline
        if (currentStage == 1)
        {
            if (timeline3 != null)
                timeline3.Play();

            return;
        }

        StartCoroutine(WaitAndNext(stages[currentStage].delayAfterEnd));
    }

    IEnumerator WaitAndNext(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayStage(currentStage + 1);
    }
}