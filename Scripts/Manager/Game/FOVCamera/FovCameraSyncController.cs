using UnityEngine;

public class FovCameraSyncController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private Camera targetCamera;

    private void LateUpdate()
    {
        if (sourceCamera == null || targetCamera == null)
        {
            return;
        }

        Transform sourceTransform = sourceCamera.transform;
        Transform targetTransform = targetCamera.transform;

        targetTransform.position = sourceTransform.position;
        targetTransform.rotation = sourceTransform.rotation;

        targetCamera.orthographic = sourceCamera.orthographic;
        targetCamera.fieldOfView = sourceCamera.fieldOfView;
        targetCamera.orthographicSize = sourceCamera.orthographicSize;
        targetCamera.nearClipPlane = sourceCamera.nearClipPlane;
        targetCamera.farClipPlane = sourceCamera.farClipPlane;
        targetCamera.aspect = sourceCamera.aspect;
    }
}