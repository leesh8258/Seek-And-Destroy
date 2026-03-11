using UnityEngine;
using VInspector;

public class PlayerAimController : MonoBehaviour
{
    [Header("에임 타겟")]
    [SerializeField] private Transform aimObjectTransform;

    [Header("거리 Min/Max")]
    [Min(0.1f), SerializeField] private float minAimDistance = 1.0f;
    [Min(0.2f), SerializeField] private float maxAimDistance = 5.0f;

    [Header("에임 타겟 이동")]
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float stopEpsilon = 0.01f;
    [SerializeField] private float stopVelocity = 0.02f;

    [SerializeField, ReadOnly] private Camera mainCamera;
    [SerializeField, ReadOnly] private Vector3 aimWorldPosition;
    [SerializeField, ReadOnly] private Vector3 aimDirection;
    [SerializeField, ReadOnly] private float aimTargetDistance;
    [SerializeField, ReadOnly] private Transform weaponAnchor;
    [SerializeField, ReadOnly] private Vector3 desiredAimObjectPosition;
    [SerializeField, ReadOnly] private Vector3 aimObjectVelocity;

    public Vector3 AimDirection => aimDirection;

    public void Initialize(Camera camera, Transform weaponAnchorTransform)
    {
        mainCamera = camera;
        weaponAnchor = weaponAnchorTransform;

        aimWorldPosition = Vector3.zero;
        aimDirection = Vector3.forward;
        desiredAimObjectPosition = Vector3.zero;
        aimObjectVelocity = Vector3.zero;
    }

    public void Tick(Vector3 ownerWorldPosition, Vector2 screenPosition)
    {
        if (mainCamera == null || weaponAnchor == null || aimObjectTransform == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Vector3 origin = weaponAnchor.position;

        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
        if (!aimPlane.Raycast(ray, out float enter) || enter < 0f) return;

        aimWorldPosition = ray.GetPoint(enter);

        Vector3 direction = aimWorldPosition - origin;
        direction.y = 0.0f;

        if (direction.sqrMagnitude < minAimDistance * minAimDistance)
        {
            aimWorldPosition = origin + aimDirection * minAimDistance;
        }

        else
        {
            aimDirection = direction.normalized;
        }

        aimTargetDistance = Vector3.Distance(ownerWorldPosition, aimWorldPosition);
        float aimCameraDistance = Mathf.Clamp(aimTargetDistance, minAimDistance, maxAimDistance);

        desiredAimObjectPosition = ownerWorldPosition + aimDirection * aimCameraDistance;
    }

    public void LateTick()
    {
        if (aimObjectTransform == null) return;

        Vector3 newPos = Vector3.SmoothDamp(
            aimObjectTransform.position,
            desiredAimObjectPosition,
            ref aimObjectVelocity,
            smoothTime,
            maxSpeed,
            Time.deltaTime
        );

        float remainSqr = (desiredAimObjectPosition - newPos).sqrMagnitude;
        if (remainSqr <= stopEpsilon * stopEpsilon && aimObjectVelocity.sqrMagnitude <= stopVelocity * stopVelocity)
        {
            newPos = desiredAimObjectPosition;
            aimObjectVelocity = Vector3.zero;
        }

        aimObjectTransform.position = newPos;
    }
}
