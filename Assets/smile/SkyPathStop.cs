using UnityEngine;
using UnityEngine.Splines;

public class PauseSplines : MonoBehaviour
{
    public SplineAnimate[] splines;

    private bool isPaused = false; // 用來切換暫停/播放

    // 這個方法會被 Signal Receiver 呼叫
    public void TogglePause()
    {
        if (!isPaused)
        {
            PauseAll();
        }
        else
        {
            PlayAll();
        }

        isPaused = !isPaused;
    }

    public void PauseAll()
    {
        Debug.Log("暫停");
        foreach (SplineAnimate s in splines)
        {
            if (s != null)
                s.Pause();
        }
    }

    public void PlayAll()
    {
        Debug.Log("播放");
        foreach (SplineAnimate s in splines)
        {
            if (s != null)
                s.Play();
        }
    }
}