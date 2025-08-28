using UnityEngine;

[ExecuteAlways]
public class ParallaxLayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Parallax settings")]
    [Tooltip("0.05 means this layer moves at 5% of the camera's speed on X and Y.")]
    [Range(-1f, 1f)]
    public float parallaxFactorX = 0.05f;
    [Tooltip("0.05 means this layer moves at 5% of the camera's speed on X and Y.")]
    [Range(-1f, 1f)]
    public float parallaxFactorY = 0.05f;

    [Header("Manual offset (world units)")]
    public Vector2 positionOffset;   // <— set this in Inspector (e.g. y = +2)

    private Vector3 startCamPos;
    private Vector3 startLayerPos;
    private bool initialized;

    private void Reset()
    {
        targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        startCamPos = targetCamera.transform.position;
        startLayerPos = transform.position;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            Initialize();
            if (!initialized) return;
        }

        Vector3 camPos = targetCamera.transform.position;
        Vector3 camDeltaFromStart = camPos - startCamPos;



        transform.position = startLayerPos
           + new Vector3(camDeltaFromStart.x * parallaxFactorX, camDeltaFromStart.y * parallaxFactorY, 0f)
           + (Vector3)positionOffset; // <— apply the offset
    }
}
