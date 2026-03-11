using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(PlayerComponentCache))]
[RequireComponent(typeof(PlayerLocalSetupController))]
[RequireComponent(typeof(PlayerRoundLifecycleController))]
public class PlayerInitializeController : MonoBehaviourPunCallbacks
{
    [Header("HP Bar")]
    [SerializeField] private Canvas[] visibilityCanvas;

    [Header("References")]
    [SerializeField, ReadOnly] private PlayerComponentCache componentCache;
    [SerializeField, ReadOnly] private PlayerLocalSetupController localSetupController;
    [SerializeField, ReadOnly] private PlayerRoundLifecycleController roundLifecycleController;

    [Header("Runtime")]
    [SerializeField, ReadOnly] private bool isReady;

    private int currentWeaponId = -1;
    private int currentCharacterId = -1;

    public PlayerHealthController HealthController => componentCache != null ? componentCache.HealthController : null;
    public PlayerWeaponController WeaponController => componentCache != null ? componentCache.WeaponController : null;

    private void Awake()
    {
        if (componentCache == null)
        {
            componentCache = GetComponent<PlayerComponentCache>();
        }

        if (localSetupController == null)
        {
            localSetupController = GetComponent<PlayerLocalSetupController>();
        }

        if (roundLifecycleController == null)
        {
            roundLifecycleController = GetComponent<PlayerRoundLifecycleController>();
        }
    }

    private void Start()
    {
        if (!TryInitialize())
        {
            Debug.Log("Player Initialize 실패");
        }
    }

    private void OnDestroy()
    {
        if (roundLifecycleController != null)
        {
            roundLifecycleController.UnbindRoundEvents();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (photonView == null) return;
        if (targetPlayer == null || targetPlayer != photonView.Owner) return;
        if (changedProps == null) return;

        bool changedCharacter = changedProps.ContainsKey(NetKeys.PlayerKey.CHARACTER_ID);
        bool changedWeapon = changedProps.ContainsKey(NetKeys.PlayerKey.WEAPON_ID);

        if (!changedCharacter && !changedWeapon) return;

        if (!isReady)
        {
            if(!TryInitialize())
            {
                Debug.Log("Player Initialize 실패");
            }
            
            return;
        }

        if (changedWeapon)
        {
            TryApplyWeaponFromProps();
        }
    }

    #region Initialize

    private bool TryInitialize()
    {
        if (isReady) return true;
        if (photonView == null) return false;
        if (componentCache == null) return false;
        if (!componentCache.HasAllCoreComponents()) return false;

        if (!TryInitializeCharacter()) return false;
        if (!TryInitializeWeapon()) return false;
        if (!InitializeVisibilityTargets()) return false;

        InitializeStats();
        InitializeAnimation();

        if (localSetupController != null)
        {
            localSetupController.SetupLocalPlayer();
        }

        if (roundLifecycleController != null)
        {
            roundLifecycleController.BindRoundEvents();
            roundLifecycleController.ApplyCurrentPhaseLock();
            roundLifecycleController.TryResetCurrentRound();
        }

        isReady = true;
        return true;
    }

    private bool TryInitializeCharacter()
    {
        Player ownerPlayer = photonView.Owner;
        if (ownerPlayer == null) return false;

        if (!NetKeys.TryGetPlayerInt(ownerPlayer, NetKeys.PlayerKey.CHARACTER_ID, out int characterId))
        {
            return false;
        }

        componentCache.CharacterController.Initialize(characterId);

        CharacterSO characterSO = componentCache.CharacterController.CurrentCharacterSO;
        if (characterSO == null || componentCache.CharacterController.CharacterView == null) return false;
        if (componentCache.CharacterController.VisibilityTarget == null) return false;

        currentCharacterId = characterId;
        return true;
    }

    private bool TryInitializeWeapon()
    {
        Player ownerPlayer = photonView.Owner;
        if (ownerPlayer == null) return false;

        if (!NetKeys.TryGetPlayerInt(ownerPlayer, NetKeys.PlayerKey.WEAPON_ID, out int weaponId))
        {
            return false;
        }

        componentCache.WeaponController.Initialize(weaponId, componentCache.CharacterController.CharacterView);

        currentWeaponId = weaponId;
        return true;
    }

    private bool InitializeVisibilityTargets()
    {
        FovVisibilityTarget target = componentCache.CharacterController.VisibilityTarget;
        if (target == null) return false;

        Renderer[] characterRenderers = componentCache.CharacterController.CharacterView != null
            ? componentCache.CharacterController.CharacterView.BodyRenderers
            : null;

        Renderer[] weaponRenderers = componentCache.WeaponController != null
            ? componentCache.WeaponController.GetWeaponRenderers()
            : null;

        int characterCount = characterRenderers != null ? characterRenderers.Length : 0;
        int weaponCount = weaponRenderers != null ? weaponRenderers.Length : 0;
        int totalCount = characterCount + weaponCount;

        Renderer[] merged = new Renderer[totalCount];
        int index = 0;

        for (int i = 0; i < characterCount; i++)
        {
            merged[index++] = characterRenderers[i];
        }

        for (int i = 0; i < weaponCount; i++)
        {
            merged[index++] = weaponRenderers[i];
        }

        target.SetManagedRenderers(merged);
        target.SetManagedCanvases(visibilityCanvas);
        target.ResetVisibilityState();

        return true;
    }

    private void InitializeStats()
    {
        componentCache.HealthController.Initialize(currentCharacterId);
        componentCache.MoveController.Initialize(currentCharacterId);
    }

    private void InitializeAnimation()
    {
        componentCache.AnimationController.Initialize(
            componentCache.CharacterController.CharacterAnimator,
            currentWeaponId
        );
    }

    private void TryApplyWeaponFromProps()
    {
        if (componentCache == null) return;
        if (componentCache.WeaponController == null) return;
        if (componentCache.CharacterController == null || componentCache.CharacterController.CharacterView == null) return;

        Player ownerPlayer = photonView.Owner;
        if (ownerPlayer == null) return;

        if (!NetKeys.TryGetPlayerInt(ownerPlayer, NetKeys.PlayerKey.WEAPON_ID, out int weaponId)) return;
        if (weaponId == currentWeaponId) return;

        componentCache.WeaponController.Initialize(weaponId, componentCache.CharacterController.CharacterView);
        currentWeaponId = weaponId;

        InitializeVisibilityTargets();
        InitializeAnimation();

        if (localSetupController != null)
        {
            localSetupController.RefreshLocalBindingsAfterWeaponChange();
        }
    }

    #endregion
}