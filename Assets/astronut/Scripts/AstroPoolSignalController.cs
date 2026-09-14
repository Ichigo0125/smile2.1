using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AstroPoolSignalController : MonoBehaviour, INotificationReceiver
{
    [Header("Alignment")]
    [Tooltip("所有 CanYing 子物件要對齊的目標。")]
    public Transform target;

    [Min(0f)]
    [Tooltip("移動與旋轉完成所需的秒數。")]
    public float moveDuration = 1f;

    [Min(0f)]
    [Tooltip("Dither Shader 的不可見值，DitherLit 預設為 2；0 為可見。")]
    public float fadeMax = 2f;

    [Tooltip("移動期間子物件的目標 Local Scale。")]
    public Vector3 targetScale = new Vector3(0.8f, 0.8f, 0.8f);

    [Tooltip("開啟後以 Renderer 視覺中心對齊 target；關閉則以 Transform Pivot 對齊。")]
    public bool alignRendererCenter = true;

    public AnimationCurve alignmentCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("留空時接收所有 Timeline Signal。")]
    public SignalAsset alignmentSignal;

    private Coroutine alignmentRoutine;
    private static readonly int FadeID = Shader.PropertyToID("_Fade");
    private MaterialPropertyBlock fadePropertyBlock;

    private void Awake()
    {
        if (fadeMax <= 0f)
        {
            fadeMax = 2f;
        }

        fadePropertyBlock = new MaterialPropertyBlock();
    }

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is SignalEmitter emitter &&
            (alignmentSignal == null || emitter.asset == alignmentSignal))
        {
            AlignToTarget();
        }
    }

    public void AlignToTarget()
    {
        if (target == null)
        {
            Debug.LogWarning("[AstroPoolSignalController] Target is not assigned.", this);
            return;
        }

        FloatingObject[] floatingObjects = GetComponentsInChildren<FloatingObject>(true);
        foreach (FloatingObject floatingObject in floatingObjects)
        {
            floatingObject.enabled = false;
        }

        DitherFadeController[] fadeControllers =
            GetComponentsInChildren<DitherFadeController>(true);
        foreach (DitherFadeController fadeController in fadeControllers)
        {
            fadeController.StopFade();
            fadeController.enabled = false;
        }

        CanYing[] ghostTrails = GetComponentsInChildren<CanYing>(true);
        Transform[] objectsToAlign = new Transform[ghostTrails.Length];

        for (int i = 0; i < ghostTrails.Length; i++)
        {
            ghostTrails[i].enabled = false;
            ghostTrails[i].gameObject.SetActive(true);
            objectsToAlign[i] = ghostTrails[i].transform;
        }

        if (alignmentRoutine != null)
        {
            StopCoroutine(alignmentRoutine);
        }

        alignmentRoutine = StartCoroutine(AlignRoutine(objectsToAlign));
    }

    private IEnumerator AlignRoutine(Transform[] objectsToAlign)
    {
        Vector3[] startPositions = new Vector3[objectsToAlign.Length];
        Quaternion[] startRotations = new Quaternion[objectsToAlign.Length];
        Vector3[] startScales = new Vector3[objectsToAlign.Length];
        Vector3[] startVisualCenters = new Vector3[objectsToAlign.Length];
        Vector3[] localVisualOffsets = new Vector3[objectsToAlign.Length];
        Renderer[][] objectRenderers = new Renderer[objectsToAlign.Length][];

        for (int i = 0; i < objectsToAlign.Length; i++)
        {
            startPositions[i] = objectsToAlign[i].position;
            startRotations[i] = objectsToAlign[i].rotation;
            startScales[i] = objectsToAlign[i].localScale;
            objectRenderers[i] = objectsToAlign[i].GetComponentsInChildren<Renderer>(true);
            if (objectRenderers[i].Length == 0)
            {
                Debug.LogWarning("[AstroPoolSignalController] 找不到可控制 Fade 的 Renderer。", objectsToAlign[i]);
            }
            startVisualCenters[i] = GetVisualCenter(objectRenderers[i], objectsToAlign[i].position);
            localVisualOffsets[i] = objectsToAlign[i].InverseTransformPoint(startVisualCenters[i]);
            SetFade(objectsToAlign[i], 0f);
        }

        Vector3 targetPosition = target.position;
        Quaternion targetRotation = target.rotation;
        float duration = Mathf.Max(0f, moveDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = duration <= 0f
                ? 1f
                : alignmentCurve.Evaluate(Mathf.Clamp01(timer / duration));
            ApplyScale(objectsToAlign, startScales, progress);
            ApplyAlignment(objectsToAlign, startPositions, startRotations, startVisualCenters,
                localVisualOffsets, targetPosition, targetRotation, progress);

            ApplyFade(objectRenderers, Mathf.Lerp(0f, fadeMax, progress));

            yield return null;
        }

        ApplyScale(objectsToAlign, startScales, 1f);
        ApplyAlignment(objectsToAlign, startPositions, startRotations, startVisualCenters,
            localVisualOffsets, targetPosition, targetRotation, 1f);

        ApplyFade(objectRenderers, fadeMax);

        for (int i = 0; i < objectsToAlign.Length; i++)
        {
            if (objectsToAlign[i] != null)
            {
                objectsToAlign[i].gameObject.SetActive(false);
            }
        }

        alignmentRoutine = null;
    }

    private void ApplyFade(Renderer[][] objectRenderers, float value)
    {
        for (int i = 0; i < objectRenderers.Length; i++)
        {
            foreach (Renderer renderer in objectRenderers[i])
            {
                SetFade(renderer, value);
            }
        }
    }

    private void ApplyScale(Transform[] objectsToAlign, Vector3[] startScales, float progress)
    {
        for (int i = 0; i < objectsToAlign.Length; i++)
        {
            if (objectsToAlign[i] != null)
            {
                objectsToAlign[i].localScale = Vector3.Lerp(startScales[i], targetScale, progress);
            }
        }
    }

    private Vector3 GetVisualCenter(Renderer[] renderers, Vector3 fallback)
    {
        if (renderers.Length == 0)
        {
            return fallback;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    private void SetFade(Transform objectTransform, float value)
    {
        Renderer[] renderers = objectTransform.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            SetFade(renderer, value);
        }
    }

    private void SetFade(Renderer renderer, float value)
    {
        renderer.GetPropertyBlock(fadePropertyBlock);
        fadePropertyBlock.SetFloat(FadeID, Mathf.Clamp(value, 0f, fadeMax));
        renderer.SetPropertyBlock(fadePropertyBlock);
    }

    private void ApplyAlignment(
        Transform[] objectsToAlign,
        Vector3[] startPositions,
        Quaternion[] startRotations,
        Vector3[] startVisualCenters,
        Vector3[] localVisualOffsets,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float progress)
    {
        for (int i = 0; i < objectsToAlign.Length; i++)
        {
            if (objectsToAlign[i] == null)
            {
                continue;
            }

            objectsToAlign[i].rotation = Quaternion.Slerp(startRotations[i], targetRotation, progress);

            Vector3 desiredPosition = Vector3.Lerp(startPositions[i], targetPosition, progress);
            Vector3 desiredVisualCenter = Vector3.Lerp(startVisualCenters[i], targetPosition, progress);
            objectsToAlign[i].position = desiredPosition;

            if (alignRendererCenter)
            {
                Vector3 currentVisualCenter = objectsToAlign[i].TransformPoint(localVisualOffsets[i]);
                objectsToAlign[i].position += desiredVisualCenter - currentVisualCenter;
            }
        }
    }
}
