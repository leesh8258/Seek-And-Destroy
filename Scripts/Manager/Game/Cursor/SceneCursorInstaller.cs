using UnityEngine;

public class SceneCursorInstaller : MonoBehaviour
{
    [Header("Cursor")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotspot = Vector2.zero;
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    [Header("State")]
    [SerializeField] private bool cursorVisible = true;
    [SerializeField] private CursorLockMode lockMode = CursorLockMode.None;

    [Header("Default Cursor")]
    [SerializeField] private bool useDefaultCursor;

    private void Start()
    {
        Apply();
    }

    public void Apply()
    {
        if (CursorManager.Instance == null) return;

        if (useDefaultCursor)
        {
            CursorManager.Instance.ApplyDefaultCursor();
            return;
        }

        CursorManager.Instance.ApplyCursor(cursorTexture, hotspot, cursorMode, cursorVisible, lockMode);
    }
}