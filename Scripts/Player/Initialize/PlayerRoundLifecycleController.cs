using UnityEngine;
using VInspector;

[RequireComponent(typeof(PlayerComponentCache))]
public class PlayerRoundLifecycleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, ReadOnly] private PlayerComponentCache componentCache;

    [Header("Runtime")]
    [SerializeField, ReadOnly] private bool isRoundHooked;
    [SerializeField, ReadOnly] private int lastResetRoundIndex = -1;

    private MapManager mapManager;

    private void Awake()
    {
        if (componentCache == null)
        {
            componentCache = GetComponent<PlayerComponentCache>();
        }
    }

    private void OnDestroy()
    {
        UnbindRoundEvents();
    }

    public void BindRoundEvents()
    {
        if (isRoundHooked) return;

        RoundManager roundManager = RoundManager.Instance;
        if (roundManager == null) return;

        roundManager.PhaseChanged += OnRoundPhaseChanged;
        roundManager.RoundIndexChanged += OnRoundIndexChanged;
        isRoundHooked = true;
    }

    public void UnbindRoundEvents()
    {
        if (!isRoundHooked) return;

        RoundManager roundManager = RoundManager.Instance;
        if (roundManager != null)
        {
            roundManager.PhaseChanged -= OnRoundPhaseChanged;
            roundManager.RoundIndexChanged -= OnRoundIndexChanged;
        }

        isRoundHooked = false;
    }

    public void ApplyCurrentPhaseLock()
    {
        RoundManager roundManager = RoundManager.Instance;
        if (roundManager == null) return;

        ApplyLocalControlLock(roundManager.CurrentPhase);
    }

    public void TryResetCurrentRound()
    {
        RoundManager roundManager = RoundManager.Instance;
        if (roundManager == null) return;

        int roundIndex = roundManager.CurrentRoundIndex;
        if (roundIndex >= 0)
        {
            TryResetForNewRound(roundIndex);
        }
    }

    private void OnRoundPhaseChanged(RoundManager.RoundPhase phase, int sequence)
    {
        ApplyLocalControlLock(phase);
    }

    private void OnRoundIndexChanged(int roundIndex)
    {
        TryResetForNewRound(roundIndex);
    }

    private void TryResetForNewRound(int nextRoundIndex)
    {
        if (nextRoundIndex == lastResetRoundIndex) return;

        lastResetRoundIndex = nextRoundIndex;

        if (componentCache != null && componentCache.HealthController != null)
        {
            componentCache.HealthController.ResetForNewRound();
        }

        if (componentCache != null && componentCache.WeaponController != null)
        {
            componentCache.WeaponController.ResetForNewRound();
        }

        if (componentCache != null && componentCache.PhotonView != null && componentCache.PhotonView.IsMine)
        {
            WarpLocalToRoundSpawnPoint();
        }
    }

    private void ApplyLocalControlLock(RoundManager.RoundPhase phase)
    {
        if (componentCache == null || componentCache.PhotonView == null) return;
        if (!componentCache.PhotonView.IsMine) return;

        bool canControl = phase == RoundManager.RoundPhase.Playing;

        if (componentCache.InputController != null)
        {
            componentCache.InputController.SetInputLocked(!canControl);
            componentCache.InputController.enabled = true;
        }

        if (!canControl)
        {
            if (componentCache.AnimationController != null)
            {
                componentCache.AnimationController.StopMove();
            }
        }
    }

    private void WarpLocalToRoundSpawnPoint()
    {
        if (mapManager == null)
        {
            mapManager = FindAnyObjectByType<MapManager>();
        }

        int actorNumber = 1;
        if (componentCache != null && componentCache.PhotonView != null && componentCache.PhotonView.Owner != null)
        {
            actorNumber = componentCache.PhotonView.Owner.ActorNumber;
        }

        Transform spawn = mapManager.GetSpawnPoint(actorNumber);
        if (spawn == null) return;

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = spawn.position;
        transform.rotation = spawn.rotation;

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }
}