using System;
using UnityEngine;
using VInspector;

[Serializable]
public class WeaponFireRuntime
{
    private const int MaxHits = 5;
    private const float AimSqrEpsilon = 0.000001f;

    [Header("Fire Runtime")]
    [SerializeField, ReadOnly] private double nextShotTimeNetwork;
    [SerializeField, ReadOnly] private int localFireSequence;
    [SerializeField, ReadOnly] private float cooldownRemainingSeconds;

    [Header("Fire Origin")]
    [SerializeField, ReadOnly] private float fireOriginCheckRadius = 0.01f;
    [SerializeField, ReadOnly] private float fireOriginOffset = 0.02f;

    private readonly RaycastHit[] rayHits = new RaycastHit[MaxHits];

    public float CooldownRemainingSeconds => cooldownRemainingSeconds;
    public int LocalFireSequence => localFireSequence;

    public void ResetForNewRound()
    {
        nextShotTimeNetwork = 0.0;
        localFireSequence = 0;
        cooldownRemainingSeconds = 0f;
    }

    public void RefreshDisplayTimer(double now)
    {
        float cooldown = 0f;
        double remain = nextShotTimeNetwork - now;
        if (remain > 0.0)
        {
            cooldown = (float)remain;
        }

        cooldownRemainingSeconds = cooldown;
    }

    public bool TryStartShot(double now, float shotInterval, out int fireSequence)
    {
        fireSequence = 0;

        if (now < nextShotTimeNetwork)
        {
            return false;
        }

        nextShotTimeNetwork = now + shotInterval;
        localFireSequence += 1;
        fireSequence = localFireSequence;
        return true;
    }

    public Vector3 GetFireOrigin(WeaponView weaponView, Vector3 muzzleOrigin, Vector3 shootDirection, LayerMask hitMask, Transform owner)
    {
        if (weaponView == null || weaponView.FireOrigin == null)
        {
            return muzzleOrigin;
        }

        Transform fireOrigin = weaponView.FireOrigin;
        Vector3 fireOriginPosition = fireOrigin.position;

        Vector3 originToMuzzle = muzzleOrigin - fireOriginPosition;
        float originToMuzzleDistance = Vector3.Dot(originToMuzzle, shootDirection);

        int hitCount = Physics.SphereCastNonAlloc(
            fireOriginPosition,
            fireOriginCheckRadius,
            shootDirection,
            rayHits,
            originToMuzzleDistance,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount <= 0)
        {
            return muzzleOrigin;
        }

        if (!TryGetClosestHit(hitCount, owner, out RaycastHit bestHit))
        {
            return muzzleOrigin;
        }

        float safeDistance = bestHit.distance - fireOriginOffset;
        if (safeDistance < 0f) safeDistance = 0f;
        if (safeDistance > originToMuzzleDistance) safeDistance = originToMuzzleDistance;

        Vector3 finalFireOrigin = fireOriginPosition + shootDirection * safeDistance;
        return finalFireOrigin;
    }

    public Vector3 GetSpreadDirection(Vector3 direction, float spreadAngle, int actorNumber, int fireSequence, int bulletIndex)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= AimSqrEpsilon)
        {
            return Vector3.forward;
        }

        Vector3 baseDir = direction.normalized;
        if (spreadAngle <= 0.000001f)
        {
            return baseDir;
        }

        float seed = GetSpreadRatio(actorNumber, fireSequence, bulletIndex);
        float angle = Mathf.Lerp(-spreadAngle, spreadAngle, seed);

        Vector3 finalDirection = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;
        finalDirection.y = 0f;

        if (finalDirection.sqrMagnitude <= AimSqrEpsilon)
        {
            return baseDir;
        }

        return finalDirection.normalized;
    }

    private bool TryGetClosestHit(int hitCount, Transform owner, out RaycastHit bestHit)
    {
        bestHit = new RaycastHit();

        int count = Mathf.Min(hitCount, MaxHits);

        bool hasBest = false;
        float bestDistance = 0f;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = rayHits[i];
            Collider collider = hit.collider;

            if (collider == null)
            {
                continue;
            }

            if (owner != null && collider.transform.IsChildOf(owner))
            {
                continue;
            }

            float distance = hit.distance;
            if (!hasBest || distance < bestDistance)
            {
                hasBest = true;
                bestHit = hit;
                bestDistance = distance;
            }
        }

        return hasBest;
    }

    // 각 클라이언트끼리 비쥬얼이 동일하도록 만든 해시
    private float GetSpreadRatio(int a, int b, int c)
    {
        long value = a * 7385693L + b * 1937L + c * 8349279L;

        value %= 100000L;
        value = value * value + value * 31L + 17L;
        value += a * c * 13L;
        value += b * c * 7L;

        if (value < 0L)
        {
            value = -value;
        }

        return (value % 10000L) / 9999f;
    }
}