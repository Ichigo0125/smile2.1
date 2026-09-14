using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AstronutDrifter : MonoBehaviour, INotificationReceiver
{
    [Header("Drift Settings")]
    [Tooltip("漂移總距離 (公尺)")]
    public float distanceM = 1.0f;
    [Tooltip("漂移總耗時 (秒)")]
    public float durationN = 3.0f;
    [Tooltip("是否使用本地座標系 (Local Z) 漂移")]
    public bool useLocalSpace = true;

    [Tooltip("緩動曲線，預設平滑進出")]
    public AnimationCurve driftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Timeline 要觸發的訊號；留空時接收所有 SignalEmitter")]
    public SignalAsset driftStartSignal;

    private Coroutine driftRoutine;
    private Vector3 driftOffset;

    public Vector3 LocalDriftOffset => useLocalSpace ? driftOffset : Vector3.zero;

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is SignalEmitter emitter &&
            (driftStartSignal == null || emitter.asset == driftStartSignal))
        {
            StartDrift();
        }
    }

    [ContextMenu("Start Drift")]
    public void StartDrift()
    {
        if (driftRoutine != null)
            StopCoroutine(driftRoutine);

        driftRoutine = StartCoroutine(DriftRoutine());
    }

    private IEnumerator DriftRoutine()
    {
        Vector3 startPos = useLocalSpace ? transform.localPosition : transform.position;
        Vector3 targetPos = startPos + (useLocalSpace ? Vector3.forward : Vector3.forward) * distanceM;

        float time = 0f;
        float safeDuration = Mathf.Max(0.01f, durationN);

        while (time < safeDuration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / safeDuration);
            float curveProgress = driftCurve.Evaluate(progress);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, curveProgress);

            driftOffset = currentPos - startPos;

            yield return null;
        }

        driftOffset = targetPos - startPos;

        driftRoutine = null;
    }
}