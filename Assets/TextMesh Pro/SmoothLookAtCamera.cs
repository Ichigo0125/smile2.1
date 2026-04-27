using UnityEngine;

public class SmoothLookAtCamera : MonoBehaviour
{
    public Transform cameraTransform;

    public float distance = 2f;
    public float followSpeed = 2f;

    void Update()
    {
        // 目標位置：玩家前方
        Vector3 targetPos = cameraTransform.position + cameraTransform.forward * distance;

        // 平滑移動
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // 看向玩家
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(transform.position - cameraTransform.position),
            Time.deltaTime * followSpeed
        );
    }
}