using UnityEngine;

public readonly struct WeaponShotVfx
{
    public readonly GameObject muzzlePrefab;
    public readonly GameObject trailPrefab;
    public readonly GameObject impactPrefab;

    public readonly float shotRange;
    public readonly float timeToMaxRange;
    public readonly float minTrailTime;

    public readonly float impactReturnDelaySeconds;
    public readonly float muzzleReturnDelaySeconds;

    public WeaponShotVfx(
        GameObject muzzlePrefab,
        GameObject trailPrefab,
        GameObject impactPrefab,
        float shotRange,
        float timeToMaxRange,
        float minTrailTime,
        float impactReturnDelaySeconds,
        float muzzleReturnDelaySeconds)
    {
        this.muzzlePrefab = muzzlePrefab;
        this.trailPrefab = trailPrefab;
        this.impactPrefab = impactPrefab;

        this.shotRange = shotRange;
        this.timeToMaxRange = timeToMaxRange;
        this.minTrailTime = minTrailTime;

        this.impactReturnDelaySeconds = impactReturnDelaySeconds;
        this.muzzleReturnDelaySeconds = muzzleReturnDelaySeconds;
    }
}
