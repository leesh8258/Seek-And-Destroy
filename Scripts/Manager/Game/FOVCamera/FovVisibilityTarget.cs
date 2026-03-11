using UnityEngine;
using VInspector;

public class FovVisibilityTarget : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private Transform visibilityCheckPoint;
    [SerializeField] private Renderer[] managedRenderers;
    [SerializeField] private Canvas[] managedCanvases;

    [Header("Runtime")]
    [SerializeField, ReadOnly] private bool isVisible;

    public Transform VisibilityCheckPoint => visibilityCheckPoint != null ? visibilityCheckPoint : transform;
    public bool IsVisible => isVisible;

    public void SetManagedRenderers(Renderer[] renderers)
    {
        managedRenderers = renderers;
        ApplyVisibilityState(isVisible);
    }

    public void SetManagedCanvases(Canvas[] canvases)
    {
        managedCanvases = canvases;
        ApplyVisibilityState(isVisible);
    }

    public void RefreshManagedRenderersFromChildren()
    {
        managedRenderers = GetComponentsInChildren<Renderer>(true);
        ApplyVisibilityState(isVisible);
    }

    public void ResetVisibilityState()
    {
        SetVisible(false);
    }

    public Vector3 GetCheckPosition()
    {
        return VisibilityCheckPoint.position;
    }

    public void SetVisible(bool visible)
    {
        if (isVisible == visible)
        {
            return;
        }

        isVisible = visible;
        ApplyVisibilityState(visible);
    }

    private void ApplyVisibilityState(bool visible)
    {
        ApplyRendererState(visible);
        ApplyCanvasState(visible);
    }

    private void ApplyCanvasState(bool visible)
    {
        if (managedCanvases == null || managedCanvases.Length == 0)
        {
            return;
        }

        for (int i = 0; i < managedCanvases.Length; i++)
        {
            Canvas canvas = managedCanvases[i];
            if (canvas == null)
            {
                continue;
            }

            canvas.enabled = visible;
        }
    }

    private void ApplyRendererState(bool visible)
    {
        if (managedRenderers == null || managedRenderers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < managedRenderers.Length; i++)
        {
            Renderer renderer = managedRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = visible;
        }
    }
}