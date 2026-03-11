using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    private PlayerMoveController playerMoveController;

    private void Awake()
    {
        playerMoveController = GetComponentInParent<PlayerMoveController>();
    }

    public void OnFootStep()
    {
        if (playerMoveController == null) return;
        playerMoveController.OnFootStep();
    }
}
