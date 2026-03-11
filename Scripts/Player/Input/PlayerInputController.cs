using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    private Vector2 moveInput;
    private Vector2 aimScreenPosition;
    private bool isFireHold;
    private bool isInputLocked;
    private bool reloadPressed;

    public Vector2 MoveInput => moveInput;
    public Vector2 AimScreenPosition => aimScreenPosition;
    public bool IsFireHold => isFireHold;

    public void SetInputLocked(bool locked)
    {
        if (isInputLocked == locked)
        {
            return;
        }

        isInputLocked = locked;
        ResetActionInputState();
    }

    private void ResetActionInputState()
    {
        moveInput = Vector2.zero;
        isFireHold = false;
        reloadPressed = false;
    }

    public bool ConsumeReloadPressed()
    {
        bool v = reloadPressed;
        reloadPressed = false;
        return v;
    }

    public void OnMove(InputValue value)
    {
        if (isInputLocked) return;

        moveInput = value.Get<Vector2>();
    }

    public void OnFire(InputValue value)
    {
        if (isInputLocked) return;

        isFireHold = value.isPressed;
    }

    public void OnReload(InputValue value)
    {
        if (isInputLocked) return;

        if (!value.isPressed) return;
        reloadPressed = true;
    }

    public void OnMouseAim(InputValue value)
    {
        if (isInputLocked) return;

        aimScreenPosition = value.Get<Vector2>();
    }
}
