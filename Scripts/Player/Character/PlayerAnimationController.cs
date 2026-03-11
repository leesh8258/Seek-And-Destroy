using Photon.Pun;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviourPun, IPunObservable
{
    private const string ANIMATION_MOVE_X = "MoveX";
    private const string ANIMATION_MOVE_Y = "MoveY";
    private const string ANIMATION_MOVE = "Move";
    private const string ANIMATION_RELOAD = "Reload";
    private const string ANIMATION_DEAD = "Dead";
    private const string ANIMATION_RELOAD_SPEED = "ReloadSpeed";

    [Header("Animation Damp")]
    [SerializeField] private float animationDamp = 0.1f;
    [SerializeField] private float remoteDamp = 0.1f;

    [Header("Animation Base Clip")]
    [SerializeField] private AnimationClip Base_Idle;
    [SerializeField] private AnimationClip Base_Reload;

    private Animator animator;
    private AnimatorOverrideController overrideController;
    private RuntimeAnimatorController originalController;

    private float localMoveX;
    private float localMoveY;
    private bool localMove;

    private float remoteTargetMoveX;
    private float remoteTargetMoveY;
    private bool remoteTargetMove;

    private float remoteAppliedMoveX;
    private float remoteAppliedMoveY;

    private float reloadAnimationTime;
    private float reloadRealTime;

    public void Initialize(Animator animator, int weaponID)
    {
        this.animator = animator;
        if (this.animator == null) return;

        InitializeOverrideController();

        if (!DBManager.Instance.TryGet<WeaponSO>(weaponID, out WeaponSO weapon)) return;

        reloadAnimationTime = weapon.reloadAnimationTime;
        reloadRealTime = weapon.reloadTime;

        SwapAnimationClip(weapon);

        SetReloadAnimationSpeedParameter();
    }

    private void Update()
    {
        TickRemoteAnimationSmoothing();
    }

    private void TickRemoteAnimationSmoothing()
    {
        if (photonView == null || photonView.IsMine) return;
        if (animator == null) return;

        float deltaTime = Time.deltaTime;
        float lerpFactor = deltaTime / Mathf.Max(0.0001f, remoteDamp);

        remoteAppliedMoveX = Mathf.Lerp(remoteAppliedMoveX, remoteTargetMoveX, lerpFactor);
        remoteAppliedMoveY = Mathf.Lerp(remoteAppliedMoveY, remoteTargetMoveY, lerpFactor);

        ApplyMoveParameters(remoteAppliedMoveX, remoteAppliedMoveY, remoteTargetMove);
    }

    private void InitializeOverrideController()
    {
        if (overrideController != null) return;
        if (originalController == null)
        {
            originalController = animator.runtimeAnimatorController;
        }

        overrideController = new AnimatorOverrideController(originalController);
        animator.runtimeAnimatorController = overrideController;
    }

    private void SwapAnimationClip(WeaponSO weapon)
    {
        if (overrideController == null) return;
        if (weapon == null) return;

        overrideController[Base_Idle] = weapon.idleAnimation;
        overrideController[Base_Reload] = weapon.reloadAnimation;
    }


    public void ApplyLocalMoveParameters(Vector2 moveInput, Vector3 worldMoveDir)
    {
        localMove = moveInput.sqrMagnitude > 0.0001f;

        Vector3 dir = worldMoveDir;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.000001f)
        {
            localMoveX = 0f;
            localMoveY = 0f;
        }

        else
        {
            dir.Normalize();

            Vector3 characterForward = transform.forward;
            characterForward.y = 0f;
            
            Vector3 characterRight = transform.right;
            characterRight.y = 0f;

            if (characterForward.sqrMagnitude > 0.000001f) characterForward.Normalize();
            if (characterRight.sqrMagnitude > 0.000001f) characterRight.Normalize();

            localMoveX = Vector3.Dot(dir, characterRight);
            localMoveY = Vector3.Dot(dir, characterForward);
        }

        if (photonView == null || !photonView.IsMine) return;

        ApplyMoveParameters(localMoveX, localMoveY, localMove);
    }

    public void StopMove()
    {
        localMoveX = 0f;
        localMoveY = 0f;
        localMove = false;

        if (photonView == null || !photonView.IsMine) return;

        ApplyMoveParameters(0f, 0f, false);
    }

    private void ApplyMoveParameters(float moveX, float moveY, bool move)
    {
        if (animator == null) return;
        
        float deltaTime = Time.deltaTime;

        animator.SetFloat(ANIMATION_MOVE_X, moveX, animationDamp, deltaTime);
        animator.SetFloat(ANIMATION_MOVE_Y, moveY, animationDamp, deltaTime);
        animator.SetBool(ANIMATION_MOVE, move);
    }

    private void SetReloadAnimationSpeedParameter()
    {
        if (animator == null) return;

        float speed = reloadAnimationTime / Mathf.Max(0.01f, reloadRealTime);
        animator.SetFloat(ANIMATION_RELOAD_SPEED, speed);
    }

    public void SetWeaponReloadParameter()
    {
        if (animator == null) return;

        animator.SetTrigger(ANIMATION_RELOAD);
    }

    public void SetDeadParameter(bool value)
    {
        if (animator == null) return;

        animator.SetBool(ANIMATION_DEAD, value);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(localMoveX);
            stream.SendNext(localMoveY);
            stream.SendNext(localMove);
        }

        else
        {
            remoteTargetMoveX = (float)stream.ReceiveNext();
            remoteTargetMoveY = (float)stream.ReceiveNext();
            remoteTargetMove = (bool)stream.ReceiveNext();
        }
    }
}
