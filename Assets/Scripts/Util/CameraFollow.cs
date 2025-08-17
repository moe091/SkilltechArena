using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    public float smoothTime = 0.12f;

    Vector3 _vel;
    void LateUpdate()
    {
        if (!target) return;
        Vector3 goal = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref _vel, smoothTime);
    }
}