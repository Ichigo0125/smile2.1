using System.Collections;
using UnityEngine;

public class CurveMovementController : MonoBehaviour
{
    [Header("Target & Space")]
    [Tooltip("要移動的目標，若為空則預設為自己")]
    public Transform targetTransform;
    [Tooltip("是否沿著物件自身的 Local Z 軸移動")]
    public bool useLocalSpace = true;

    [Header("Distance Curve")]
    [Tooltip("橫軸為時間 (秒)，縱軸為位移距離 (0 ~ 3 公尺)")]
    public AnimationCurve distanceCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(3f, 3f) // 預設 3 秒內移動 3 公尺
    );

    private Vector3 initialPosition;
    private Coroutine moveRoutine;

    private void Awake()
    {
        if (targetTransform == null)
            targetTransform = transform;

        CacheInitialPosition();
    }

    [ContextMenu("Cache Initial Position")]
    public void CacheInitialPosition()
    {
        if (targetTransform != null)
        {
            initialPosition = useLocalSpace ? targetTransform.localPosition : targetTransform.position;
        }
    }

    [ContextMenu("Start Movement")]
    public void StartMovement()
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveWithCurveRoutine());
    }

    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        if (targetTransform != null)
        {
            if (useLocalSpace)
                targetTransform.localPosition = initialPosition;
            else
                targetTransform.position = initialPosition;
        }
    }

    private IEnumerator MoveWithCurveRoutine()
    {
        if (distanceCurve.length == 0)
            yield break;

        // 取得曲線最後一個 Keyframe 的時間作為總時長
        float totalDuration = distanceCurve.keys[distanceCurve.length - 1].time;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            
            // 讀取當前時間點對應的距離 (0 ~ 3m)
            float currentDistance = distanceCurve.Evaluate(elapsed);

            // 計算位移
            Vector3 offset = Vector3.forward * currentDistance;

            if (useLocalSpace)
            {
                targetTransform.localPosition = initialPosition + offset;
            }
            else
            {
                targetTransform.position = initialPosition + offset;
            }

            yield return null;
        }

        // 確保結束時精確落在曲線終點
        float finalDistance = distanceCurve.Evaluate(totalDuration);
        if (useLocalSpace)
            targetTransform.localPosition = initialPosition + (Vector3.forward * finalDistance);
        else
            targetTransform.position = initialPosition + (Vector3.forward * finalDistance);

        moveRoutine = null;
    }
}