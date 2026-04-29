using UnityEngine;

public class SimpleMove : MonoBehaviour
{
    // 在 Inspector 窗口可以手动调整速度
    public float moveSpeed = 5f; 

    void Update()
    {
        // 沿着物体的“前方”（蓝色轴 Z轴）移动
        // 关键：乘以 Time.deltaTime，位移才会随时间缩放而缩放
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
}