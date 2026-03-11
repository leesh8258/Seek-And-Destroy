using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(PlayerComponentCache))]
public class PlayerWeaponController : MonoBehaviourPun
{
    private enum HitKind
    {
        None = 0,
        Environment = 1,
        Player = 2,
    }

    private const float AimSqrEpsilon = 0.000001f;

    [Header("LayerMask")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask environmentMask;

    [Header("References")]
    [SerializeField, ReadOnly] private PlayerComponentCache componentCache;

    [Header("Weapon Runtime")]
    [SerializeField, ReadOnly] private WeaponLoadoutRuntime loadoutRuntime = new WeaponLoadoutRuntime();
    [SerializeField, ReadOnly] private WeaponAmmoRuntime ammoRuntime = new WeaponAmmoRuntime();
    [SerializeField, ReadOnly] private WeaponReloadRuntime reloadRuntime = new WeaponReloadRuntime();
    [SerializeField, ReadOnly] private WeaponFireRuntime fireRuntime = new WeaponFireRuntime();
    [SerializeField, ReadOnly] private WeaponHitResolver hitResolver = new WeaponHitResolver();
    [SerializeField, ReadOnly] private WeaponVisualEventPlayer visualEventPlayer = new WeaponVisualEventPlayer();

    [Header("Display Runtime")]
    [SerializeField, ReadOnly] private float cooldownRemainingSeconds;
    [SerializeField, ReadOnly] private float reloadRemainingSeconds;

    private Transform owner;
    private int masterLastProcessedHitSequence;

    private Vector3[] shotEndPoints;
    private Vector3[] shotHitNormals;
    private int[] shotHitKinds;

    public Transform WeaponAnchor => loadoutRuntime.WeaponAnchor;
    public int CurrentAmmo => ammoRuntime.CurrentAmmo;
    public int MaxAmmo => ammoRuntime.MaxAmmo;
    public WeaponSO CurrentWeaponSO => loadoutRuntime.CurrentWeaponSO;
    public bool IsReloading => reloadRuntime.IsReloading;
    public float ReloadRemainingSeconds => reloadRuntime.ReloadRemainingSeconds;
    public float CooldownRemainingSeconds => cooldownRemainingSeconds;

    #region Public API

    private void Awake()
    {
        if (componentCache == null)
        {
            componentCache = GetComponent<PlayerComponentCache>();
        }

        hitResolver.Configure(hitMask, playerMask, environmentMask);
    }

    public void Initialize(int weaponId, CharacterView characterView)
    {
        bool equipped = loadoutRuntime.TryEquip(weaponId, characterView);
        if (!equipped)
        {
            Debug.LogError($"[PlayerWeaponController] Weapon equip failed. weaponId={weaponId}");
            ClearWeaponState();
            return;
        }

        ResetRuntimeState();
        EnsureShotBuffers();
        RefreshDisplayTimers();
    }

    public void ResetForNewRound()
    {
        ammoRuntime.ResetForNewRound();
        reloadRuntime.ResetForNewRound();
        fireRuntime.ResetForNewRound();

        masterLastProcessedHitSequence = 0;
        cooldownRemainingSeconds = 0f;
        reloadRemainingSeconds = 0f;
    }

    public void Tick(bool isFireHold, bool isReloadPressed, Vector3 aimDir)
    {
        if (!photonView.IsMine) return;

        if (isReloadPressed)
        {
            TryStartReloadLocal();
        }

        Vector3 aim = aimDir;
        aim.y = 0f;

        bool aimValid = aim.sqrMagnitude > AimSqrEpsilon;
        if (aimValid)
        {
            aim.Normalize();
        }

        if (!isFireHold || !aimValid) return;

        TryFireLocal(aim);
    }

    public Renderer[] GetWeaponRenderers()
    {
        return loadoutRuntime.GetWeaponRenderers();
    }

    #endregion

    #region Unity

    private void Update()
    {
        TickReloadTimers();
        RefreshDisplayTimers();
    }

    private void TickReloadTimers()
    {
        if (photonView == null) return;

        if (photonView.IsMine)
        {
            TickReloadLocal();
        }
        else
        {
            TickReloadRemote();
        }
    }

    #endregion

    #region Initialization Helpers

    private void ResetRuntimeState()
    {
        ammoRuntime.Initialize(loadoutRuntime.MagazineSize);
        reloadRuntime.ResetForNewRound();
        fireRuntime.ResetForNewRound();

        owner = loadoutRuntime.CharacterView != null ? loadoutRuntime.CharacterView.transform.root : null;

        masterLastProcessedHitSequence = 0;
        cooldownRemainingSeconds = 0f;
        reloadRemainingSeconds = 0f;
    }

    private void ClearWeaponState()
    {
        loadoutRuntime.Clear();
        ammoRuntime.Clear();
        reloadRuntime.Clear();
        fireRuntime.ResetForNewRound();

        cooldownRemainingSeconds = 0f;
        reloadRemainingSeconds = 0f;

        owner = null;
        masterLastProcessedHitSequence = 0;

        shotEndPoints = null;
        shotHitNormals = null;
        shotHitKinds = null;
    }

    private void EnsureShotBuffers()
    {
        int bulletCount = Mathf.Max(1, loadoutRuntime.BulletsPerShot);

        bool ok =
            shotEndPoints != null && shotEndPoints.Length == bulletCount &&
            shotHitNormals != null && shotHitNormals.Length == bulletCount &&
            shotHitKinds != null && shotHitKinds.Length == bulletCount;

        if (ok) return;

        shotEndPoints = new Vector3[bulletCount];
        shotHitNormals = new Vector3[bulletCount];
        shotHitKinds = new int[bulletCount];
    }

    #endregion

    #region Display

    private void RefreshDisplayTimers()
    {
        double now = PhotonNetwork.Time;

        fireRuntime.RefreshDisplayTimer(now);
        reloadRuntime.RefreshDisplayTimer(now);

        cooldownRemainingSeconds = fireRuntime.CooldownRemainingSeconds;
        reloadRemainingSeconds = reloadRuntime.ReloadRemainingSeconds;
    }

    #endregion

    #region RPC - Hit (Master)

    [PunRPC]
    private void NetworkRequestHit(int fireSequence, Vector3 shootOrigin, Vector3 shootDir, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player ownerPlayer = photonView != null ? photonView.Owner : null;
        if (ownerPlayer != null && info.Sender != ownerPlayer) return;

        if (!IsPlayingPhase()) return;
        if (IsOwnerDead(ownerPlayer)) return;

        if (fireSequence <= masterLastProcessedHitSequence) return;
        masterLastProcessedHitSequence = fireSequence;

        Vector3 dir = shootDir;

        if (dir.sqrMagnitude > 4.0f)
        {
            dir = dir - shootOrigin;
        }

        dir.y = 0f;
        if (dir.sqrMagnitude <= AimSqrEpsilon) return;

        dir.Normalize();

        int actorNumber = ownerPlayer != null ? ownerPlayer.ActorNumber : -1;
        int bulletCount = Mathf.Max(1, loadoutRuntime.BulletsPerShot);

        for (int i = 0; i < bulletCount; i++)
        {
            Vector3 spreadDir = fireRuntime.GetSpreadDirection(dir, loadoutRuntime.BulletSpreadAngle, actorNumber, fireSequence, i);
            WeaponResolvedHit hitResult = hitResolver.ResolveHit(shootOrigin, spreadDir, loadoutRuntime.ShotRange, owner);

            if (hitResult.hasHit && hitResult.hitPlayer)
            {
                ApplyDamageOnMaster(hitResult.hitCollider, loadoutRuntime.Damage);
            }
        }
    }

    private void ApplyDamageOnMaster(Collider hitCollider, int damageValue)
    {
        if (!PhotonNetwork.IsMasterClient || hitCollider == null) return;

        PlayerHealthController targetHealth = hitCollider.GetComponentInParent<PlayerHealthController>();
        if (targetHealth == null) return;

        Player victimPlayer = targetHealth.photonView.Owner;
        if (victimPlayer != null && NetKeys.TryGetPlayerBool(victimPlayer, NetKeys.PlayerKey.DEAD, out bool dead) && dead) return;

        Player attackerPlayer = photonView != null ? photonView.Owner : null;
        int attackerActorNumber = attackerPlayer != null ? attackerPlayer.ActorNumber : -1;
        targetHealth.photonView.RPC(nameof(PlayerHealthController.RPCTakeDamage), RpcTarget.AllViaServer, damageValue, attackerActorNumber);
    }

    #endregion

    #region RPC - Visuals

    [PunRPC]
    private void NetworkPlayShotVisuals(int fireSequence, Vector3[] endPoints, int[] hitKinds, Vector3[] hitNormals)
    {
        visualEventPlayer.PlayShotVisuals(loadoutRuntime.CurrentWeaponSO, loadoutRuntime.WeaponView, endPoints, hitKinds, hitNormals);
    }

    [PunRPC]
    private void NetworkStartReload(int reloadSequence, double reloadEndTime, PhotonMessageInfo info)
    {
        Player ownerPlayer = photonView != null ? photonView.Owner : null;
        if (ownerPlayer != null && info.Sender != ownerPlayer) return;

        if (!IsPlayingPhase()) return;
        if (IsOwnerDead(ownerPlayer)) return;

        if (!reloadRuntime.TryStartRemote(reloadSequence, reloadEndTime))
        {
            return;
        }

        PlayerAnimationController animationController = componentCache != null ? componentCache.AnimationController : null;
        visualEventPlayer.PlayReloadStarted(loadoutRuntime.CurrentWeaponSO, loadoutRuntime.WeaponView, animationController);
    }

    #endregion

    #region Local - Fire

    private void TryFireLocal(Vector3 aimDirNormalized)
    {
        if (loadoutRuntime.CurrentWeaponSO == null) return;
        if (!IsPlayingPhase()) return;
        if (reloadRuntime.IsReloading) return;

        if (ammoRuntime.IsEmpty)
        {
            TryStartReloadLocal();
            return;
        }

        WeaponView weaponView = loadoutRuntime.WeaponView;
        if (weaponView == null || weaponView.Muzzle == null) return;

        double now = PhotonNetwork.Time;
        if (!fireRuntime.TryStartShot(now, loadoutRuntime.ShotInterval, out int fireSequence))
        {
            return;
        }

        if (!ammoRuntime.TryConsume(1))
        {
            TryStartReloadLocal();
            return;
        }

        Vector3 shootOrigin = fireRuntime.GetFireOrigin(
            weaponView,
            weaponView.Muzzle.position,
            aimDirNormalized,
            hitMask,
            owner
        );

        EnsureShotBuffers();
        BuildShotVisualBuffers(shootOrigin, aimDirNormalized, fireSequence);

        photonView.RPC(nameof(NetworkPlayShotVisuals), RpcTarget.AllViaServer, fireSequence, shotEndPoints, shotHitKinds, shotHitNormals);
        photonView.RPC(nameof(NetworkRequestHit), RpcTarget.MasterClient, fireSequence, shootOrigin, aimDirNormalized);

        if (ammoRuntime.IsEmpty)
        {
            TryStartReloadLocal();
        }
    }

    private void BuildShotVisualBuffers(Vector3 shootOrigin, Vector3 shootDir, int fireSequence)
    {
        int bulletCount = Mathf.Max(1, loadoutRuntime.BulletsPerShot);
        int actorNumber = GetOwnerActorNumber();

        for (int i = 0; i < bulletCount; i++)
        {
            Vector3 spreadDir = fireRuntime.GetSpreadDirection(
                shootDir,
                loadoutRuntime.BulletSpreadAngle,
                actorNumber,
                fireSequence,
                i
            );

            WeaponResolvedHit hitResult = hitResolver.ResolveHit(
                shootOrigin,
                spreadDir,
                loadoutRuntime.ShotRange,
                owner
            );

            shotEndPoints[i] = hitResult.endPoint;

            if (!hitResult.hasHit)
            {
                shotHitKinds[i] = (int)HitKind.None;
                shotHitNormals[i] = Vector3.up;
                continue;
            }

            if (hitResult.hitPlayer)
            {
                shotHitKinds[i] = (int)HitKind.Player;
            }
            else if (hitResult.hitEnvironment)
            {
                shotHitKinds[i] = (int)HitKind.Environment;
            }
            else
            {
                shotHitKinds[i] = (int)HitKind.None;
            }

            shotHitNormals[i] = hitResult.normal;
        }
    }

    #endregion

    #region Local - Reload

    private void TryStartReloadLocal()
    {
        if (loadoutRuntime.CurrentWeaponSO == null) return;
        if (!IsPlayingPhase()) return;

        WeaponView weaponView = loadoutRuntime.WeaponView;
        if (weaponView == null || weaponView.Muzzle == null) return;

        if (!reloadRuntime.CanStartReload(ammoRuntime.CurrentAmmo, ammoRuntime.MaxAmmo))
        {
            return;
        }

        if (!reloadRuntime.TryStartLocal(PhotonNetwork.Time, loadoutRuntime.ReloadTime, out int reloadSequence, out double endTime))
        {
            return;
        }

        photonView.RPC(nameof(NetworkStartReload), RpcTarget.All, reloadSequence, endTime);
    }

    private void TickReloadLocal()
    {
        if (!reloadRuntime.TickLocal(PhotonNetwork.Time))
        {
            return;
        }

        ammoRuntime.RefillFull();
    }

    private void TickReloadRemote()
    {
        if (photonView.IsMine) return;

        reloadRuntime.TickRemote(PhotonNetwork.Time);
    }

    #endregion

    #region Game State Checks

    private bool IsPlayingPhase()
    {
        RoundManager roundManager = RoundManager.Instance;
        if (roundManager == null) return false;

        return roundManager.CurrentPhase == RoundManager.RoundPhase.Playing;
    }

    private bool IsOwnerDead(Player ownerPlayer)
    {
        if (ownerPlayer == null) return false;

        if (NetKeys.TryGetPlayerBool(ownerPlayer, NetKeys.PlayerKey.DEAD, out bool dead) && dead) return true;

        return false;
    }

    private int GetOwnerActorNumber()
    {
        if (photonView == null) return -1;

        Player ownerPlayer = photonView.Owner;
        if (ownerPlayer == null) return -1;

        return ownerPlayer.ActorNumber;
    }

    #endregion
}