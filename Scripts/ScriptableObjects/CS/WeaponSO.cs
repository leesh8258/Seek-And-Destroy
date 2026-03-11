using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponSO")]
public class WeaponSO : ScriptableObject
{
    [Header("Prefab")]
    public GameObject weaponPrefab;

    [Header("Info")]
    public Sprite weaponSprite;

    [Header("WeaponAnchor")]
    public Vector3 weaponPosition;
    public Vector3 weaponRotation;

    [Header("Range / Damage")]
    public int damage;
    public float shotRange;

    [Header("Reload / ShotSpeed")]
    public float timeToMaxRange;
    public float shotInterval;
    public float reloadTime;
    public float reloadAnimationTime;

    [Header("bullet")]
    public int magazineSize;
    public int bulletPerShot;
    public float bulletSpreadAngle;

    [Header("VFX Prefab")]
    public GameObject impactEffectPrefab;
    public GameObject bulletTrailEffectPrefab;
    public GameObject muzzleFlashEffectPrefab;

    [Header("VFX")]
    public float minTrailTime = 0.01f;
    public float impactReturnDelaySeconds = 2.0f;
    public float muzzleReturnDelaySeconds = 1.0f;

    [Header("SFX")]
    public SoundSO shotSound;
    public SoundSO reloadSound;

    [Header("Animation")]
    public AnimationClip idleAnimation;
    public AnimationClip reloadAnimation;

    public WeaponShotVfx BuildShotVfxSpec()
    {
        WeaponShotVfx spec = new WeaponShotVfx(
            muzzleFlashEffectPrefab,
            bulletTrailEffectPrefab,
            impactEffectPrefab,
            shotRange,
            timeToMaxRange,
            minTrailTime,
            impactReturnDelaySeconds,
            muzzleReturnDelaySeconds
        );

        return spec;
    }
}
