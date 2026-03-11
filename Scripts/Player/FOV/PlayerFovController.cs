using System.Collections.Generic;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(PlayerFovVisibilityController))]
public class PlayerFovController : MonoBehaviour
{
    private struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool hitValue, Vector3 pointValue, float dstValue, float angleValue)
        {
            hit = hitValue;
            point = pointValue;
            dst = dstValue;
            angle = angleValue;
        }
    }

    private struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 pointAValue, Vector3 pointBValue)
        {
            pointA = pointAValue;
            pointB = pointBValue;
        }
    }

    [Header("Reference")]
    [SerializeField] private Transform eyeTransform;
    [SerializeField] private MeshFilter coneFovMeshFilter;
    [SerializeField] private MeshFilter circleFovMeshFilter;
    [SerializeField, ReadOnly] private PlayerFovVisibilityController visibilityController;

    [Header("Cone FOV")]
    [Min(0.1f), SerializeField] private float coneViewRadius = 6f;
    [Range(0f, 360f), SerializeField] private float coneViewAngle = 120f;

    [Header("Circle FOV")]
    [SerializeField] private bool useCircleFov = true;
    [Min(0.1f), SerializeField] private float circleViewRadius = 2f;

    [Header("Layer")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Mesh")]
    [Min(1f), SerializeField] private float meshResolution = 1.0f;
    [Min(1), SerializeField] private int edgeResolveIterations = 4;
    [Min(1f), SerializeField] private float edgeDstThreshold = 0.5f;

    [Header("Targets")]
    [Min(0f), SerializeField] private float immediateCheckRadius = 0.1f;
    [Min(8), SerializeField] private int overlapBufferSize = 64;

    private PlayerAimController aimController;
    private FovVisibilityTarget selfVisibilityTarget;

    private Mesh coneFovMesh;
    private Mesh circleFovMesh;
    private bool isInitialized;
    private float coneCosHalfFov;

    private readonly List<Vector3> coneViewPoints = new List<Vector3>(512);
    private readonly List<Vector3> circleViewPoints = new List<Vector3>(512);
    private readonly List<Vector3> meshVertices = new List<Vector3>(1024);
    private readonly List<int> meshTriangles = new List<int>(2048);
    private readonly HashSet<FovVisibilityTarget> seenTargetsThisFrame = new HashSet<FovVisibilityTarget>();

    private Collider[] overlapResults;

    private void Awake()
    {
        if (visibilityController == null)
        {
            visibilityController = GetComponent<PlayerFovVisibilityController>();
        }
    }

    public void Initialize(PlayerAimController playerAimController, FovVisibilityTarget selfTarget)
    {
        aimController = playerAimController;
        selfVisibilityTarget = selfTarget;

        if (visibilityController == null)
        {
            visibilityController = GetComponent<PlayerFovVisibilityController>();
        }

        if (overlapResults == null || overlapResults.Length != overlapBufferSize)
        {
            overlapResults = new Collider[overlapBufferSize];
        }

        coneCosHalfFov = Mathf.Cos(coneViewAngle * 0.5f * Mathf.Deg2Rad);

        if (coneFovMesh == null)
        {
            coneFovMesh = new Mesh();
            coneFovMesh.name = "Player Cone FOV Mesh";
            coneFovMesh.MarkDynamic();
        }

        if (circleFovMesh == null)
        {
            circleFovMesh = new Mesh();
            circleFovMesh.name = "Player Circle FOV Mesh";
            circleFovMesh.MarkDynamic();
        }

        if (coneFovMeshFilter != null)
        {
            coneFovMeshFilter.sharedMesh = coneFovMesh;
        }

        if (circleFovMeshFilter != null)
        {
            circleFovMeshFilter.sharedMesh = circleFovMesh;
        }

        if (visibilityController != null)
        {
            visibilityController.ResetMemory();
        }

        isInitialized = true;
    }

    public void Tick()
    {
        if (!isInitialized) return;

        float now = Time.time;
        Vector3 aimDirection = GetAimDirection();

        ScanAndReportVisibleTargets(aimDirection, now);

        if (visibilityController != null)
        {
            visibilityController.Tick(now);
        }
    }

    public void LateTick()
    {
        if (!isInitialized) return;

        Vector3 aimDirection = GetAimDirection();

        DrawConeFieldOfViewMesh(aimDirection);

        if (useCircleFov)
        {
            DrawCircleFieldOfViewMesh();
        }
        else
        {
            ClearMesh(circleFovMesh);
        }
    }

    public bool IsWorldPointVisible(Vector3 worldPoint)
    {
        if (!isInitialized) return false;

        Vector3 origin = GetEyeOrigin();
        Vector3 aimDirection = GetAimDirection();

        return IsPointVisibleByAnyFov(origin, aimDirection, worldPoint);
    }

    public bool IsTargetVisibleNow(FovVisibilityTarget target)
    {
        if (!isInitialized) return false;
        if (target == null) return false;

        Vector3 checkPosition = target.GetCheckPosition();
        return IsWorldPointVisible(checkPosition);
    }

    public void ReportImmediateVisibility(FovVisibilityTarget target)
    {
        if (!isInitialized) return;
        if (target == null) return;
        if (visibilityController == null) return;

        float now = Time.time;
        visibilityController.ReportSeen(target, now);
    }

    private void OnDisable()
    {
        isInitialized = false;
        seenTargetsThisFrame.Clear();

        if (visibilityController != null)
        {
            visibilityController.ResetMemory();
        }

        ClearMesh(coneFovMesh);
        ClearMesh(circleFovMesh);
    }

    private void ScanAndReportVisibleTargets(Vector3 aimDirection, float now)
    {
        if (visibilityController != null && selfVisibilityTarget != null)
        {
            visibilityController.ReportSeen(selfVisibilityTarget, now);
        }

        Vector3 origin = GetEyeOrigin();
        seenTargetsThisFrame.Clear();

        float maxDetectionRadius = Mathf.Max(coneViewRadius, useCircleFov ? circleViewRadius : 0f);
        int hitCount = Physics.OverlapSphereNonAlloc(origin, maxDetectionRadius, overlapResults, targetMask);

        if (hitCount > overlapResults.Length)
        {
            hitCount = overlapResults.Length;
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = overlapResults[i];
            if (hitCollider == null)
            {
                continue;
            }

            FovVisibilityTarget target = hitCollider.GetComponentInParent<FovVisibilityTarget>();
            if (target == null)
            {
                continue;
            }

            if (!seenTargetsThisFrame.Add(target))
            {
                continue;
            }

            Vector3 targetPos = target.GetCheckPosition();
            if (!IsPointVisibleByAnyFov(origin, aimDirection, targetPos))
            {
                continue;
            }

            if (visibilityController != null)
            {
                visibilityController.ReportSeen(target, now);
            }
        }
    }

    private bool IsPointVisibleByAnyFov(Vector3 origin, Vector3 aimDirection, Vector3 worldPoint)
    {
        if (IsPointVisibleByCone(origin, aimDirection, worldPoint))
        {
            return true;
        }

        if (useCircleFov && IsPointVisibleByCircle(origin, worldPoint))
        {
            return true;
        }

        return false;
    }

    private bool IsPointVisibleByCone(Vector3 origin, Vector3 aimDirection, Vector3 worldPoint)
    {
        Vector3 dirToTarget = worldPoint - origin;
        dirToTarget.y = 0f;

        float sqrDst = dirToTarget.sqrMagnitude;
        if (sqrDst <= 0.0001f)
        {
            return true;
        }

        float radiusSqr = coneViewRadius * coneViewRadius;
        if (sqrDst > radiusSqr)
        {
            return false;
        }

        float dstToTarget = Mathf.Sqrt(sqrDst);
        Vector3 dirToTargetNorm = dirToTarget / dstToTarget;

        float dot = Vector3.Dot(aimDirection, dirToTargetNorm);
        if (dot < coneCosHalfFov)
        {
            return false;
        }

        if (Physics.Raycast(origin, dirToTargetNorm, dstToTarget + immediateCheckRadius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    private bool IsPointVisibleByCircle(Vector3 origin, Vector3 worldPoint)
    {
        Vector3 dirToTarget = worldPoint - origin;
        dirToTarget.y = 0f;

        float sqrDst = dirToTarget.sqrMagnitude;
        if (sqrDst <= 0.0001f)
        {
            return true;
        }

        float radiusSqr = circleViewRadius * circleViewRadius;
        if (sqrDst > radiusSqr)
        {
            return false;
        }

        float dstToTarget = Mathf.Sqrt(sqrDst);
        Vector3 dirToTargetNorm = dirToTarget / dstToTarget;

        if (Physics.Raycast(origin, dirToTargetNorm, dstToTarget + immediateCheckRadius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    private Vector3 GetAimDirection()
    {
        Vector3 aimDirection = aimController != null ? aimController.AimDirection : transform.forward;
        aimDirection.y = 0f;

        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            aimDirection = transform.forward;
            aimDirection.y = 0f;
        }

        aimDirection.Normalize();
        return aimDirection;
    }

    private void DrawConeFieldOfViewMesh(Vector3 aimDirection)
    {
        if (coneFovMeshFilter == null || coneFovMesh == null)
        {
            return;
        }

        Vector3 origin = GetEyeOrigin();
        float aimYaw = Mathf.Atan2(aimDirection.x, aimDirection.z) * Mathf.Rad2Deg;

        int stepCount = Mathf.Max(1, Mathf.RoundToInt(coneViewAngle * meshResolution));
        float stepAngleSize = coneViewAngle / stepCount;

        coneViewPoints.Clear();

        ViewCastInfo oldViewCast = default;

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = aimYaw - coneViewAngle * 0.5f + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(origin, angle, coneViewRadius);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                bool isEdgeCandidate = oldViewCast.hit != newViewCast.hit
                    || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded);

                if (isEdgeCandidate)
                {
                    EdgeInfo edge = FindEdge(origin, oldViewCast, newViewCast, coneViewRadius);
                    if (edge.pointA != Vector3.zero) coneViewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) coneViewPoints.Add(edge.pointB);
                }
            }

            coneViewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        BuildMesh(coneFovMeshFilter, coneFovMesh, origin, coneViewPoints);
    }

    private void DrawCircleFieldOfViewMesh()
    {
        if (circleFovMeshFilter == null || circleFovMesh == null)
        {
            return;
        }

        Vector3 origin = GetEyeOrigin();

        int stepCount = Mathf.Max(8, Mathf.RoundToInt(360f * meshResolution));
        float stepAngleSize = 360f / stepCount;

        circleViewPoints.Clear();

        ViewCastInfo oldViewCast = default;

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(origin, angle, circleViewRadius);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                bool isEdgeCandidate = oldViewCast.hit != newViewCast.hit
                    || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded);

                if (isEdgeCandidate)
                {
                    EdgeInfo edge = FindEdge(origin, oldViewCast, newViewCast, circleViewRadius);
                    if (edge.pointA != Vector3.zero) circleViewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) circleViewPoints.Add(edge.pointB);
                }
            }

            circleViewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        BuildMesh(circleFovMeshFilter, circleFovMesh, origin, circleViewPoints);
    }

    private void BuildMesh(MeshFilter targetMeshFilter, Mesh targetMesh, Vector3 origin, List<Vector3> points)
    {
        meshVertices.Clear();
        meshTriangles.Clear();

        Vector3 localOrigin = targetMeshFilter.transform.InverseTransformPoint(origin);
        meshVertices.Add(localOrigin);

        int pointCount = points.Count;
        for (int i = 0; i < pointCount; i++)
        {
            Vector3 localPoint = targetMeshFilter.transform.InverseTransformPoint(points[i]);
            meshVertices.Add(localPoint);
        }

        for (int i = 0; i < pointCount - 1; i++)
        {
            meshTriangles.Add(0);
            meshTriangles.Add(i + 1);
            meshTriangles.Add(i + 2);
        }

        targetMesh.Clear(false);
        targetMesh.SetVertices(meshVertices);
        targetMesh.SetTriangles(meshTriangles, 0, true);
        targetMesh.RecalculateNormals();
    }

    private Vector3 GetEyeOrigin()
    {
        float eyeY = eyeTransform.position.y;
        return new Vector3(transform.position.x, eyeY, transform.position.z);
    }

    private EdgeInfo FindEdge(Vector3 origin, ViewCastInfo minViewCast, ViewCastInfo maxViewCast, float radius)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;

        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) * 0.5f;
            ViewCastInfo newViewCast = ViewCast(origin, angle, radius);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;

            if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    private ViewCastInfo ViewCast(Vector3 origin, float globalAngle, float radius)
    {
        Vector3 dir = DirFromAngle(globalAngle);
        Vector3 rayOrigin = origin + dir * immediateCheckRadius;

        if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, Mathf.Max(0f, radius - immediateCheckRadius), obstacleMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 point = hit.point;
            point.y = origin.y;
            float dst = Vector3.Distance(origin, point);
            return new ViewCastInfo(true, point, dst, globalAngle);
        }

        Vector3 endPoint = origin + dir * radius;
        endPoint.y = origin.y;
        return new ViewCastInfo(false, endPoint, radius, globalAngle);
    }

    private Vector3 DirFromAngle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }

    private void ClearMesh(Mesh mesh)
    {
        if (mesh == null)
        {
            return;
        }

        mesh.Clear(false);
    }
}