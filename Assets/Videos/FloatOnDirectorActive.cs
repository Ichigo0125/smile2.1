using UnityEngine;
using UnityEngine.Playables;

public class FloatOnDirectorActive : MonoBehaviour
{
    public PlayableDirector director;
    public Transform[] targets;

    public float activeDuration = 5f;

    [Header("Base Motion")]
    public float motionSpeed = 1f;
    public float upDownFrequency = 0.6f;
    public float upDownAmount = 0.08f;
    public float zRotateAmplitude = 4f;
    public float zRotateFrequency = 0.4f;

    [Header("Random Variation")]
    public float randomTimeOffset = 2f;
    public float randomSpeedRange = 0.3f;
    public float randomAmountRange = 0.05f;
    public float randomRotateRange = 2f;

    class TargetData
    {
        public Transform target;
        public Vector3 basePos;
        public Quaternion baseRot;
        public float timeOffset;
        public float speedMul;
        public float amountMul;
        public float rotateMul;
        public Vector3 localCenter;
        public bool hasCenter;
    }

    TargetData[] _data;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (targets == null)
        {
            _data = null;
            return;
        }

        _data = new TargetData[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            var d = new TargetData { target = t };

            if (t != null)
            {
                d.basePos = t.localPosition;
                d.baseRot = t.localRotation;

                d.timeOffset = Random.Range(0f, randomTimeOffset);
                d.speedMul = 1f + Random.Range(-randomSpeedRange, randomSpeedRange);
                d.amountMul = 1f + Random.Range(-randomAmountRange, randomAmountRange);
                d.rotateMul = 1f + Random.Range(-randomRotateRange, randomRotateRange);

                var renderer = t.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    d.localCenter = t.InverseTransformPoint(renderer.bounds.center);
                    d.hasCenter = true;
                }
            }

            _data[i] = d;
        }
    }

    void LateUpdate()
    {
        if (director == null || _data == null)
            return;

        bool active = director.state == PlayState.Playing &&
                      director.time >= 0 &&
                      director.time <= activeDuration;

        foreach (var d in _data)
        {
            if (d.target == null) continue;

            if (active)
            {
                float time = ((float)director.time + d.timeOffset) * motionSpeed * d.speedMul;

                float upDown = Mathf.Sin(time * Mathf.PI * 2f * upDownFrequency);
                Vector3 offset = new Vector3(0f, upDownAmount * d.amountMul * upDown, 0f);

                float z = Mathf.Sin(time * Mathf.PI * 2f * zRotateFrequency) * zRotateAmplitude * d.rotateMul;
                Quaternion zRot = Quaternion.AngleAxis(z, Vector3.forward);

                Vector3 finalPos = d.basePos + offset;
                if (d.hasCenter)
                {
                    Vector3 pivotAdjust = d.localCenter - zRot * d.localCenter;
                    finalPos += pivotAdjust;
                }

                d.target.localPosition = finalPos;
                d.target.localRotation = d.baseRot * zRot;
            }
            else
            {
                d.target.localPosition = d.basePos;
                d.target.localRotation = d.baseRot;
            }
        }
    }
}
