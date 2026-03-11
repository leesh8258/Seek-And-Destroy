using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(PlayerComponentCache))]
public class PlayerHealthController : MonoBehaviourPun
{
    [SerializeField, ReadOnly] private int maxHealthPoint;
    [SerializeField, ReadOnly] private int currentHealthPoint;
    [SerializeField, ReadOnly] private PlayerComponentCache componentCache;

    private bool deathConfirmed;

    public int CurrentHealthPoint => currentHealthPoint;
    public int MaxHealthPoint => maxHealthPoint;

    public event Action<int, int> HealthChanged;

    private void Awake()
    {
        if (componentCache == null)
        {
            componentCache = GetComponent<PlayerComponentCache>();
        }
    }

    public void Initialize(int characterId)
    {
        if (!DBManager.Instance.TryGet<CharacterSO>(characterId, out CharacterSO characterSO)) return;

        maxHealthPoint = characterSO.healthPoint;
        currentHealthPoint = maxHealthPoint;

        deathConfirmed = false;

        InvokeHealthChanged();
    }

    public void ResetForNewRound()
    {
        currentHealthPoint = maxHealthPoint;
        deathConfirmed = false;

        if (componentCache != null && componentCache.AnimationController != null)
        {
            componentCache.AnimationController.SetDeadParameter(deathConfirmed);
        }

        InvokeHealthChanged();
    }

    [PunRPC]
    public void RPCTakeDamage(int value, int attackActorNumber, PhotonMessageInfo info)
    {
        Player sender = info.Sender;

        if (sender == null || !sender.IsMasterClient) return;
        if (deathConfirmed) return;

        TakeDamage(value);

        if (!PhotonNetwork.IsMasterClient) return;
        if (!IsDead()) return;

        Player player = photonView.Owner;
        if (player == null) return;

        if (NetKeys.TryGetPlayerBool(player, NetKeys.PlayerKey.DEAD, out bool alreadyDead) && alreadyDead) return;
        if (NetKeys.TryConfirmDeathOnMaster(player, attackActorNumber, out int confirmedSeq))
        {
            photonView.RPC(nameof(RPCOnDeathConfirmed), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPCOnDeathConfirmed()
    {
        deathConfirmed = true;

        currentHealthPoint = 0;

        if (componentCache != null && componentCache.AnimationController != null)
        {
            componentCache.AnimationController.SetDeadParameter(deathConfirmed);
        }

        InvokeHealthChanged();

        if (photonView != null && photonView.IsMine)
        {
            if (componentCache != null && componentCache.InputController != null)
            {
                componentCache.InputController.SetInputLocked(true);
            }

            if (componentCache != null && componentCache.AnimationController != null)
            {
                componentCache.AnimationController.StopMove();
            }
        }
    }

    public void TakeDamage(int value)
    {
        if (value <= 0) return;

        int tempHealth = currentHealthPoint - value;
        currentHealthPoint = Mathf.Max(tempHealth, 0);

        if (componentCache != null && componentCache.CharacterController != null)
        {
            componentCache.CharacterController.PlayHitFlash();
        }

        InvokeHealthChanged();
    }

    public bool IsDead()
    {
        return currentHealthPoint <= 0;
    }

    private void InvokeHealthChanged()
    {
        HealthChanged?.Invoke(currentHealthPoint, maxHealthPoint);
    }
}