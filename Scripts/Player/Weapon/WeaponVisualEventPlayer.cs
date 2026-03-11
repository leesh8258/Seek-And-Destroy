using System;
using UnityEngine;

[Serializable]
public class WeaponVisualEventPlayer
{
    public void PlayShotVisuals(WeaponSO weaponSO, WeaponView weaponView, Vector3[] endPoints, int[] hitKinds, Vector3[] hitNormals)
    {
        if (WeaponEffectsManager.Instance == null) return;
        if (weaponSO == null) return;
        if (weaponView == null || weaponView.Muzzle == null) return;

        SoundManager.Instance.Play3DSound(weaponSO.shotSound, weaponView.Muzzle.position);

        WeaponShotVfx spec = weaponSO.BuildShotVfxSpec();
        WeaponEffectsManager.Instance.PlayMuzzle(weaponView.Muzzle, spec);
        WeaponEffectsManager.Instance.PlayShotTrailsAndImpacts(weaponView.Muzzle.position, endPoints, hitKinds, hitNormals, spec);
    }

    public void PlayReloadStarted(WeaponSO weaponSO, WeaponView weaponView, PlayerAnimationController animationController)
    {
        if (weaponSO == null) return;
        if (weaponView == null || weaponView.Muzzle == null) return;

        if (animationController != null)
        {
            animationController.SetWeaponReloadParameter();
        }

        SoundManager.Instance.Play3DSound(weaponSO.reloadSound, weaponView.Muzzle.position);
    }
}