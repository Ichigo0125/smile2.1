using UnityEngine;

public class HeadOffset : MonoBehaviour
{
    public Transform headBone;
    public float angle = 10f;

    private Quaternion originalRotation;

    void Start()
    {
        originalRotation = headBone.localRotation;
    }

    void LateUpdate()
{
    headBone.localRotation *= Quaternion.Euler(0f, 0f, angle);
}
}