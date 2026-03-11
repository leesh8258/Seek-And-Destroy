using System;
using UnityEngine;
using VInspector;

[Serializable]
public struct WeaponResolvedHit
{
    public bool hasHit;
    public bool hitPlayer;
    public bool hitEnvironment;
    public Collider hitCollider;
    public Vector3 endPoint;
    public Vector3 normal;
}

[Serializable]
public class WeaponHitResolver
{
    private const int MaxHits = 5;

    [Header("Mask")]
    [SerializeField, ReadOnly] private LayerMask hitMask;
    [SerializeField, ReadOnly] private LayerMask playerMask;
    [SerializeField, ReadOnly] private LayerMask environmentMask;

    private readonly RaycastHit[] rayHits = new RaycastHit[MaxHits];

    public void Configure(LayerMask hitMaskValue, LayerMask playerMaskValue, LayerMask environmentMaskValue)
    {
        hitMask = hitMaskValue;
        playerMask = playerMaskValue;
        environmentMask = environmentMaskValue;
    }

    public WeaponResolvedHit ResolveHit(Vector3 origin, Vector3 direction, float range, Transform owner)
    {
        WeaponResolvedHit result = new WeaponResolvedHit();
        result.hasHit = false;
        result.hitPlayer = false;
        result.hitEnvironment = false;
        result.hitCollider = null;
        result.endPoint = origin + (direction.normalized * range);
        result.normal = Vector3.up;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction,
            rayHits,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount <= 0)
        {
            return result;
        }

        if (!TryGetClosestHit(hitCount, owner, out RaycastHit bestHit))
        {
            return result;
        }

        Collider bestCollider = bestHit.collider;
        int layer = bestCollider.gameObject.layer;

        result.hasHit = true;
        result.hitPlayer = IsInLayerMask(layer, playerMask);
        result.hitEnvironment = IsInLayerMask(layer, environmentMask);
        result.hitCollider = bestCollider;
        result.endPoint = bestHit.point;
        result.normal = bestHit.normal;

        return result;
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

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        int bit = 1 << layer;
        return (mask.value & bit) != 0;
    }
}