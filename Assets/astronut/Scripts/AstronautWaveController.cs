using System.Collections;
using UnityEngine;

public class AstronautWaveController : MonoBehaviour
{
    [Header("Trigger")]
    public bool startOnEnable;
    [Min(0f)] public float delayBeforeStart;

    [Header("Timing")]
    [Min(0.01f)] public float raiseDuration = 1.2f;
    [Min(0f)] public float holdDuration = 0.2f;
    [Min(0.01f)] public float waveDuration = 1.8f;
    [Min(0f)] public float lowerDuration = 0.8f;
    [Min(0)] public int waveCycles = 3;

    [Header("Motion")]
    public Vector3 raiseRotation = new Vector3(-55f, 0f, 0f);
    public Vector3 waveRotation = new Vector3(0f, 0f, 18f);
    public AnimationCurve raiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve waveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Bones")]
    public Transform upperArm;
    public Transform forearm;
    public Transform hand;
    public bool leftArm = true;
    public bool autoFindBones = true;

    private Quaternion upperArmRestRotation;
    private Quaternion forearmRestRotation;
    private Quaternion handRestRotation;
    private Coroutine waveRoutine;

    private void Awake()
    {
        if (autoFindBones)
            FindBones();

        CacheRestPose();
    }

    private void OnEnable()
    {
        if (startOnEnable)
            StartWave();
    }

    [ContextMenu("Find Bones")]
    public void FindBones()
    {
        upperArm = FindBone(upperArm, "upperarm", "upper_arm", "arm");
        forearm = FindBone(forearm, "forearm", "lowerarm", "lower_arm");
        hand = FindBone(hand, "hand", "wrist");
    }

    [ContextMenu("Cache Rest Pose")]
    public void CacheRestPose()
    {
        if (upperArm != null)
            upperArmRestRotation = upperArm.localRotation;
        if (forearm != null)
            forearmRestRotation = forearm.localRotation;
        if (hand != null)
            handRestRotation = hand.localRotation;
    }

    [ContextMenu("Start Wave")]
    public void StartWave()
    {
        if (!HasAnyBone())
        {
            Debug.LogWarning("[AstronautWaveController] Assign Upper Arm, Forearm, or Hand before starting the wave.", this);
            return;
        }

        if (waveRoutine != null)
            StopCoroutine(waveRoutine);

        waveRoutine = StartCoroutine(WaveRoutine());
    }

    [ContextMenu("Reset Pose")]
    public void ResetPose()
    {
        if (waveRoutine != null)
            StopCoroutine(waveRoutine);

        ApplyPose(0f, 0f);
    }

    private IEnumerator WaveRoutine()
    {
        if (delayBeforeStart > 0f)
            yield return new WaitForSeconds(delayBeforeStart);

        yield return AnimateRaise(0f, 1f, raiseDuration);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        float duration = Mathf.Max(0.01f, waveDuration);
        int cycles = Mathf.Max(0, waveCycles);
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / duration);
            float wave = Mathf.Sin(progress * cycles * Mathf.PI * 2f);
            ApplyPose(1f, wave);
            yield return null;
        }

        ApplyPose(1f, 0f);
        yield return AnimateRaise(1f, 0f, lowerDuration);
        waveRoutine = null;
    }

    private IEnumerator AnimateRaise(float from, float to, float duration)
    {
        float time = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (time < safeDuration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / safeDuration);
            float raise = Mathf.Lerp(from, to, raiseCurve.Evaluate(progress));
            ApplyPose(raise, 0f);
            yield return null;
        }

        ApplyPose(to, 0f);
    }

    private void ApplyPose(float raise, float wave)
    {
        Quaternion raiseOffset = Quaternion.Euler(raiseRotation * raise);
        Quaternion waveOffset = Quaternion.Euler(waveRotation * waveCurve.Evaluate(Mathf.Abs(wave)) * wave);

        if (upperArm != null)
            upperArm.localRotation = upperArmRestRotation * raiseOffset;
        if (forearm != null)
            forearm.localRotation = forearmRestRotation * raiseOffset;
        if (hand != null)
            hand.localRotation = handRestRotation * waveOffset;
    }

    private bool HasAnyBone()
    {
        return upperArm != null || forearm != null || hand != null;
    }

    private Transform FindBone(Transform current, params string[] names)
    {
        if (current != null)
            return current;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            string normalizedName = child.name.ToLowerInvariant().Replace(" ", "").Replace("-", "_");
            bool hasLeftMarker = normalizedName.Contains("left") || normalizedName.EndsWith("_l") || normalizedName.EndsWith(".l");
            bool hasRightMarker = normalizedName.Contains("right") || normalizedName.EndsWith("_r") || normalizedName.EndsWith(".r");
            if (hasLeftMarker != leftArm && (hasLeftMarker || hasRightMarker))
                continue;

            foreach (string name in names)
            {
                if (normalizedName.Contains(name))
                    return child;
            }
        }

        return null;
    }
}