using UnityEngine;
using UnityEngine.Splines;

public class DelaySplinePlay : MonoBehaviour
{
    public SplineAnimate[] animates;
    public float[] delayTimes;

    void Start()
    {
        for (int i = 0; i < animates.Length; i++)
        {
            animates[i].gameObject.SetActive(false);
            StartCoroutine(StartAfterDelay(animates[i], delayTimes[i]));
        }
    }

    System.Collections.IEnumerator StartAfterDelay(SplineAnimate anim, float delay)
    {
        yield return new WaitForSeconds(delay);

        anim.gameObject.SetActive(true);
        anim.Play();
    }
}