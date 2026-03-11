using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    private Texture2D currentTexture;
    private Vector2 currentHotspot;
    private CursorMode currentMode = CursorMode.Auto;
    private bool currentVisible = true;
    private CursorLockMode currentLockMode = CursorLockMode.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ApplyDefaultCursor()
    {
        currentTexture = null;
        currentHotspot = Vector2.zero;
        currentMode = CursorMode.Auto;
        currentVisible = true;
        currentLockMode = CursorLockMode.None;

        ApplyCurrent();
    }

    public void ApplyCursor(Texture2D texture, Vector2 hotspot, CursorMode mode, bool visible, CursorLockMode lockMode)
    {
        currentTexture = texture;
        currentHotspot = hotspot;
        currentMode = mode;
        currentVisible = visible;
        currentLockMode = lockMode;

        ApplyCurrent();
    }

    private void ApplyCurrent()
    {
        Cursor.SetCursor(currentTexture, currentHotspot, currentMode);
        Cursor.visible = currentVisible;
        Cursor.lockState = currentLockMode;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        ApplyCurrent();
    }
}