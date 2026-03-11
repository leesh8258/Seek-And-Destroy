using System;
using UnityEngine;
using VInspector;

[Serializable]
public class WeaponAmmoRuntime
{
    [Header("Ammo")]
    [SerializeField, ReadOnly] private int currentAmmo;
    [SerializeField, ReadOnly] private int maxAmmo;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool IsEmpty => currentAmmo <= 0;
    public bool IsFull => currentAmmo >= maxAmmo;

    public void Initialize(int maxAmmoValue)
    {
        maxAmmo = Mathf.Max(0, maxAmmoValue);
        currentAmmo = maxAmmo;
    }

    public void ResetForNewRound()
    {
        currentAmmo = maxAmmo;
    }

    public bool TryConsume(int amount)
    {
        if (amount <= 0) return false;
        if (currentAmmo < amount) return false;

        currentAmmo -= amount;
        return true;
    }

    public void RefillFull()
    {
        currentAmmo = maxAmmo;
    }

    public void Clear()
    {
        currentAmmo = 0;
        maxAmmo = 0;
    }
}