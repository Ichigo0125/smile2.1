using UnityEngine;
using UnityEngine.Playables;

public class Astronut_me_alwaysFloating : MonoBehaviour
{
    [Header("Director Reference")]
    public PlayableDirector director;

    [Tooltip("Director 開始播放後，等待幾秒才開始漂浮")]
    public float startDelay = 0f;

    [Tooltip("開始漂浮後持續幾秒")]
    public float activeDuration = 262f;

    [Header("Base Motion")]
    public float motionSpeed = 0.2f;
    public float upDownFrequency = 0.6f;
    public float upDownAmount = 0.08f;
    public float zRotateAmplitude = 2f;
    public float zRotateFrequency = 0.4f;

    [Header("Random Variation")]
    public float randomTimeOffset = 2f;
    public float randomSpeedRange = 0.3f;
    public float randomAmountRange = 0.05f;
    public float randomRotateRange = 2f;

    // 初始與隨機快取數值
    private Vector3 basePos;
    private Quaternion baseRot;
    private float timeOffset;
    private float speedMul;
    private float amountMul;
    private float rotateMul;
    private Vector3 localCenter;
    private bool hasCenter;
    private AstronutDrifter drifter;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        drifter = GetComponent<AstronutDrifter>();

        timeOffset = Random.Range(0f, randomTimeOffset);
        speedMul = 1f + Random.Range(-randomSpeedRange, randomSpeedRange);
        amountMul = 1f + Random.Range(-randomAmountRange, randomAmountRange);
        rotateMul = 1f + Random.Range(-randomRotateRange, randomRotateRange);

        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            localCenter = transform.InverseTransformPoint(renderer.bounds.center);
            hasCenter = true;
        }
    }

    private void LateUpdate()
    {
        if (director == null)
            return;

        bool active = director.state == PlayState.Playing &&
                      director.time >= startDelay &&
                      director.time <= startDelay + activeDuration;

        if (active)
        {
            float elapsedTime = (float)director.time - startDelay;
            float time = (elapsedTime + timeOffset) * motionSpeed * speedMul;

            // 上下浮動
            float upDown = Mathf.Sin(time * Mathf.PI * 2f * upDownFrequency);
            Vector3 offset = new Vector3(0f, upDownAmount * amountMul * upDown, 0f);

            // Z 軸微幅旋轉
            float z = Mathf.Sin(time * Mathf.PI * 2f * zRotateFrequency) * zRotateAmplitude * rotateMul;
            Quaternion zRot = Quaternion.AngleAxis(z, Vector3.forward);

            Vector3 finalPos = basePos + offset;
            if (drifter != null)
                finalPos += drifter.LocalDriftOffset;
            if (hasCenter)
            {
                Vector3 pivotAdjust = localCenter - zRot * localCenter;
                finalPos += pivotAdjust;
            }

            transform.localPosition = finalPos;
            transform.localRotation = baseRot * zRot;
        }
        else
        {
            // 非作用期間回歸初始位置與旋轉
            transform.localPosition = basePos;
            transform.localRotation = baseRot;
        }
    }
} 
