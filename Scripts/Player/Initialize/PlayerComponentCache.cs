using Photon.Pun;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PlayerCharacterController))]
[RequireComponent(typeof(PlayerHealthController))]
[RequireComponent(typeof(PlayerMoveController))]
[RequireComponent(typeof(PlayerWeaponController))]
[RequireComponent(typeof(PlayerAimController))]
[RequireComponent(typeof(PlayerInputController))]
[RequireComponent(typeof(PlayerUpdateController))]
[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerFovController))]
public class PlayerComponentCache : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField, ReadOnly] private PhotonView photonView;
    [SerializeField, ReadOnly] private PlayerCharacterController characterController;
    [SerializeField, ReadOnly] private PlayerHealthController healthController;
    [SerializeField, ReadOnly] private PlayerMoveController moveController;
    [SerializeField, ReadOnly] private PlayerWeaponController weaponController;
    [SerializeField, ReadOnly] private PlayerAimController aimController;
    [SerializeField, ReadOnly] private PlayerInputController inputController;
    [SerializeField, ReadOnly] private PlayerUpdateController updateController;
    [SerializeField, ReadOnly] private PlayerAnimationController animationController;
    [SerializeField, ReadOnly] private PlayerFovController fovController;

    public PhotonView PhotonView => photonView;
    public PlayerCharacterController CharacterController => characterController;
    public PlayerHealthController HealthController => healthController;
    public PlayerMoveController MoveController => moveController;
    public PlayerWeaponController WeaponController => weaponController;
    public PlayerAimController AimController => aimController;
    public PlayerInputController InputController => inputController;
    public PlayerUpdateController UpdateController => updateController;
    public PlayerAnimationController AnimationController => animationController;
    public PlayerFovController FovController => fovController;

    private void Awake()
    {
        RefreshCache();
    }

    public void RefreshCache()
    {
        photonView = GetComponent<PhotonView>();
        characterController = GetComponent<PlayerCharacterController>();
        healthController = GetComponent<PlayerHealthController>();
        moveController = GetComponent<PlayerMoveController>();
        weaponController = GetComponent<PlayerWeaponController>();
        aimController = GetComponent<PlayerAimController>();
        inputController = GetComponent<PlayerInputController>();
        updateController = GetComponent<PlayerUpdateController>();
        animationController = GetComponent<PlayerAnimationController>();
        fovController = GetComponent<PlayerFovController>();
    }

    public bool HasAllCoreComponents()
    {
        return photonView != null
            && characterController != null
            && healthController != null
            && moveController != null
            && weaponController != null
            && aimController != null
            && inputController != null
            && updateController != null
            && animationController != null
            && fovController != null;
    }
}