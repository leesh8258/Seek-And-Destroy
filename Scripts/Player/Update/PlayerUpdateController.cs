using UnityEngine;

public class PlayerUpdateController : MonoBehaviour
{
    private PlayerComponentCache componentCache;


    private void Awake()
    {
        if (componentCache == null)
        {
            componentCache = GetComponent<PlayerComponentCache>();
        }
    }

    private void Update()
    {
        TickLocalFrame();
    }

    private void LateUpdate()
    {
        LateTickLocalFrame();
    }

    private void TickLocalFrame()
    {
        if (componentCache == null || !componentCache.HasAllCoreComponents()) return;

        Vector2 screenPos = componentCache.InputController.AimScreenPosition;
        componentCache.AimController.Tick(transform.position, screenPos);

        componentCache.FovController.Tick();

        Vector3 aimDirection = componentCache.AimController.AimDirection;
        componentCache.MoveController.Tick(componentCache.InputController.MoveInput, aimDirection);

        componentCache.AnimationController.ApplyLocalMoveParameters(componentCache.InputController.MoveInput, componentCache.MoveController.WorldMoveDir);

        bool reloadPressed = componentCache.InputController.ConsumeReloadPressed();
        componentCache.WeaponController.Tick(componentCache.InputController.IsFireHold, reloadPressed, aimDirection);
    }

    private void LateTickLocalFrame()
    {
        if (componentCache == null || !componentCache.HasAllCoreComponents()) return;

        componentCache.AimController.LateTick();
        componentCache.FovController.LateTick();
    }
}
