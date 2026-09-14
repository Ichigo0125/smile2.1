using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Fade In")]
    [SerializeField] private float fadeInDuration = 2f;

    [Header("Motion")]
    [SerializeField] private float motionSpeed = 1f;

    [SerializeField] private float upDownFrequency = 0.6f;
    [SerializeField] private float upDownAmount = 0.08f;

    [SerializeField] private float zRotateAmplitude = 4f;
    [SerializeField] private float zRotateFrequency = 0.4f;

    [Header("Random Variation")]
    [SerializeField] private float randomTimeOffset = 2f;
    [SerializeField] private float randomSpeedRange = 0.3f;
    [SerializeField] private float randomAmountRange = 0.05f;
    [SerializeField] private float randomRotateRange = 2f;

    private Vector3 basePos;
    private Quaternion baseRot;

    private float spawnTime;

    private float timeOffset;
    private float speedMul;
    private float amountMul;
    private float rotateMul;

    private Vector3 localCenter;
    private bool hasCenter;

    void Start()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;

        spawnTime = Time.time;

        timeOffset = Random.Range(0f, randomTimeOffset);
        speedMul = 1f + Random.Range(-randomSpeedRange, randomSpeedRange);
        amountMul = 1f + Random.Range(-randomAmountRange, randomAmountRange);
        rotateMul = 1f + Random.Range(-randomRotateRange, randomRotateRange);

        Renderer r = GetComponentInChildren<Renderer>();

        if (r != null)
        {
            localCenter = transform.InverseTransformPoint(r.bounds.center);
            hasCenter = true;
        }
    }

    void Update()
    {
        float time = (Time.time + timeOffset) * motionSpeed * speedMul;

        // 0 → 1，控制浮動逐漸開始
        float blend = Mathf.Clamp01((Time.time - spawnTime) / fadeInDuration);

        // 上下浮動
        float upDown = Mathf.Sin(time * Mathf.PI * 2f * upDownFrequency);
        Vector3 offset = Vector3.up * (upDownAmount * amountMul * upDown * blend);

        // 左右搖晃
        float z = Mathf.Sin(time * Mathf.PI * 2f * zRotateFrequency)
                    * zRotateAmplitude * rotateMul * blend;

        Quaternion zRot = Quaternion.AngleAxis(z, Vector3.forward);

        Vector3 finalPos = basePos + offset;

        // 讓旋轉看起來以 Renderer 中心為中心
        if (hasCenter)
        {
            Vector3 pivotAdjust = (localCenter - zRot * localCenter) * blend;
            finalPos += pivotAdjust;
        }

        transform.localPosition = finalPos;
        transform.localRotation = baseRot * zRot;
    }
}