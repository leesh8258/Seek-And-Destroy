using System;
using UnityEngine;
using VInspector;

[Serializable]
public class WeaponReloadRuntime
{
    [Header("Reload")]
    [SerializeField, ReadOnly] private bool isReloading;
    [SerializeField, ReadOnly] private double reloadEndTimeNetwork;
    [SerializeField, ReadOnly] private float reloadRemainingSeconds;

    [Header("Reload Sequence")]
    [SerializeField, ReadOnly] private int localReloadSequence;
    [SerializeField, ReadOnly] private int remoteLastProcessedReloadSequence;

    public bool IsReloading => isReloading;
    public double ReloadEndTimeNetwork => reloadEndTimeNetwork;
    public float ReloadRemainingSeconds => reloadRemainingSeconds;
    public int LocalReloadSequence => localReloadSequence;
    public int RemoteLastProcessedReloadSequence => remoteLastProcessedReloadSequence;

    public bool CanStartReload(int currentAmmo, int maxAmmo)
    {
        if (isReloading) return false;
        if (maxAmmo <= 0) return false;
        if (currentAmmo >= maxAmmo) return false;

        return true;
    }

    public bool TryStartLocal(double now, float reloadTime, out int reloadSequence, out double reloadEndTime)
    {
        reloadSequence = 0;
        reloadEndTime = 0.0;

        if (isReloading) return false;

        isReloading = true;
        reloadEndTimeNetwork = now + reloadTime;

        localReloadSequence += 1;
        reloadSequence = localReloadSequence;
        reloadEndTime = reloadEndTimeNetwork;

        return true;
    }

    public bool TryStartRemote(int reloadSequence, double reloadEndTime)
    {
        if (reloadSequence <= remoteLastProcessedReloadSequence)
        {
            return false;
        }

        remoteLastProcessedReloadSequence = reloadSequence;
        isReloading = true;
        reloadEndTimeNetwork = reloadEndTime;
        return true;
    }

    public bool TickLocal(double now)
    {
        if (!isReloading) return false;
        if (now < reloadEndTimeNetwork) return false;

        isReloading = false;
        reloadEndTimeNetwork = 0.0;
        reloadRemainingSeconds = 0f;
        return true;
    }

    public bool TickRemote(double now)
    {
        if (!isReloading) return false;
        if (now < reloadEndTimeNetwork) return false;

        isReloading = false;
        reloadEndTimeNetwork = 0.0;
        reloadRemainingSeconds = 0f;
        return true;
    }

    public void RefreshDisplayTimer(double now)
    {
        float reload = 0f;

        if (isReloading)
        {
            double remain = reloadEndTimeNetwork - now;
            if (remain > 0.0)
            {
                reload = (float)remain;
            }
        }

        reloadRemainingSeconds = reload;
    }

    public void ResetForNewRound()
    {
        isReloading = false;
        reloadEndTimeNetwork = 0.0;
        reloadRemainingSeconds = 0f;
        localReloadSequence = 0;
        remoteLastProcessedReloadSequence = 0;
    }

    public void Clear()
    {
        ResetForNewRound();
    }
}