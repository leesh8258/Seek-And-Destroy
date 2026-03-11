using System;
using UnityEngine;
using VInspector;

[Serializable]
public class WeaponLoadoutRuntime
{
    [Header("SO / Prefab / View")]
    [SerializeField, ReadOnly] private WeaponSO currentWeaponSO;
    [SerializeField, ReadOnly] private GameObject weaponInstance;
    [SerializeField, ReadOnly] private WeaponView weaponView;
    [SerializeField, ReadOnly] private CharacterView characterView;
    [SerializeField, ReadOnly] private Transform weaponAnchor;

    [Header("Weapon Info")]
    [SerializeField, ReadOnly] private int damage;
    [SerializeField, ReadOnly] private float shotRange;
    [SerializeField, ReadOnly] private float shotInterval;
    [SerializeField, ReadOnly] private float reloadTime;
    [SerializeField, ReadOnly] private int magazineSize;
    [SerializeField, ReadOnly] private int bulletsPerShot;
    [SerializeField, ReadOnly] private float bulletSpreadAngle;
    [SerializeField, ReadOnly] private float trailTimeToMaxRange;

    public WeaponSO CurrentWeaponSO => currentWeaponSO;
    public GameObject WeaponInstance => weaponInstance;
    public WeaponView WeaponView => weaponView;
    public CharacterView CharacterView => characterView;
    public Transform WeaponAnchor => weaponAnchor;

    public int Damage => damage;
    public float ShotRange => shotRange;
    public float ShotInterval => shotInterval;
    public float ReloadTime => reloadTime;
    public int MagazineSize => magazineSize;
    public int BulletsPerShot => bulletsPerShot;
    public float BulletSpreadAngle => bulletSpreadAngle;
    public float TrailTimeToMaxRange => trailTimeToMaxRange;

    public bool TryEquip(int weaponId, CharacterView ownerCharacterView)
    {
        if (ownerCharacterView == null)
        {
            Clear();
            return false;
        }

        if (!DBManager.Instance.TryGet<WeaponSO>(weaponId, out WeaponSO weaponSO))
        {
            Clear();
            return false;
        }

        return TryEquip(weaponSO, ownerCharacterView);
    }

    public void Clear()
    {
        if (weaponInstance != null)
        {
            UnityEngine.Object.Destroy(weaponInstance);
            weaponInstance = null;
        }

        currentWeaponSO = null;
        weaponView = null;
        characterView = null;
        weaponAnchor = null;

        damage = 0;
        shotRange = 0f;
        shotInterval = 0f;
        reloadTime = 0f;
        magazineSize = 0;
        bulletsPerShot = 0;
        bulletSpreadAngle = 0f;
        trailTimeToMaxRange = 0f;
    }

    public Renderer[] GetWeaponRenderers()
    {
        if (weaponView == null)
        {
            return Array.Empty<Renderer>();
        }

        Renderer[] renderers = weaponView.WeaponRenderers;
        if (renderers == null)
        {
            return Array.Empty<Renderer>();
        }

        return renderers;
    }

    private bool TryEquip(WeaponSO weaponSO, CharacterView ownerCharacterView)
    {
        Clear();

        if (weaponSO == null || ownerCharacterView == null)
        {
            return false;
        }

        if (ownerCharacterView.WeaponAnchor == null)
        {
            return false;
        }

        if (weaponSO.weaponPrefab == null)
        {
            return false;
        }

        currentWeaponSO = weaponSO;
        characterView = ownerCharacterView;
        weaponAnchor = ownerCharacterView.WeaponAnchor;

        weaponAnchor.localPosition = weaponSO.weaponPosition;
        weaponAnchor.localRotation = Quaternion.Euler(weaponSO.weaponRotation);

        weaponInstance = UnityEngine.Object.Instantiate(weaponSO.weaponPrefab, weaponAnchor);
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;
        weaponInstance.transform.localScale = Vector3.one;

        weaponView = weaponInstance.GetComponent<WeaponView>();
        if (weaponView == null)
        {
            Clear();
            return false;
        }

        damage = weaponSO.damage;
        shotRange = weaponSO.shotRange;
        shotInterval = weaponSO.shotInterval;
        reloadTime = weaponSO.reloadTime;
        magazineSize = weaponSO.magazineSize;
        bulletsPerShot = weaponSO.bulletPerShot;
        bulletSpreadAngle = weaponSO.bulletSpreadAngle;
        trailTimeToMaxRange = weaponSO.timeToMaxRange;

        return true;
    }
}