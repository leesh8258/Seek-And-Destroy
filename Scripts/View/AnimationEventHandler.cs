using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    private PlayerMoveController playerMoveController;

    private void Awake()
    {
        playerMoveController = GetComponentInParent<PlayerMoveController>();
    }

    // 발소리 사운드 이벤트
    public void OnFootStep()
    {
        if (playerMoveController == null) return;
        playerMoveController.OnFootStep();
    }
}
