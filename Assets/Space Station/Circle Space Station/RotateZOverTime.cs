using UnityEngine;

public class RotateZOverTime : MonoBehaviour
{
    public float speed = 50f; // 每秒旋轉幾度

    void Update()
    {
        transform.Rotate(0, 0, speed * Time.deltaTime);
    }
}