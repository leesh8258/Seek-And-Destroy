using UnityEngine;

public class FovRenderTextureManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera fovCamera;
    [SerializeField] private Material fullScreenMaterial;

    [Header("RenderTexture")]
    [SerializeField] private string texturePropertyName = "_FovMaskTex";
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
    [SerializeField] private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    [SerializeField] private RenderTextureFormat textureFormat = RenderTextureFormat.ARGB32;
    [SerializeField] private int depthBufferBits = 24;

    private RenderTexture runtimeRenderTexture;
    private int lastWidth = -1;
    private int lastHeight = -1;
    private int texturePropertyId;

    private void Awake()
    {
        texturePropertyId = Shader.PropertyToID(texturePropertyName);
    }

    private void OnEnable()
    {
        CreateOrResizeRenderTexture();
    }

    private void Update()
    {
        int targetWidth = Mathf.Max(1, Screen.width);
        int targetHeight = Mathf.Max(1, Screen.height);

        if (targetWidth != lastWidth || targetHeight != lastHeight)
        {
            CreateOrResizeRenderTexture();
        }
    }

    private void OnDisable()
    {
        ReleaseRenderTexture();
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
    }

    private void CreateOrResizeRenderTexture()
    {
        int targetWidth = Mathf.Max(1, Screen.width);
        int targetHeight = Mathf.Max(1, Screen.height);

        if (runtimeRenderTexture != null && targetWidth == lastWidth && targetHeight == lastHeight)
        {
            return;
        }

        ReleaseRenderTextureInternal();

        runtimeRenderTexture = new RenderTexture(targetWidth, targetHeight, depthBufferBits, textureFormat);
        runtimeRenderTexture.name = "Runtime_FovMask_RT";
        runtimeRenderTexture.filterMode = filterMode;
        runtimeRenderTexture.wrapMode = wrapMode;
        runtimeRenderTexture.useMipMap = false;
        runtimeRenderTexture.autoGenerateMips = false;
        runtimeRenderTexture.Create();

        lastWidth = targetWidth;
        lastHeight = targetHeight;

        if (fovCamera != null)
        {
            fovCamera.targetTexture = runtimeRenderTexture;
        }

        if (fullScreenMaterial != null)
        {
            fullScreenMaterial.SetTexture(texturePropertyId, runtimeRenderTexture);
        }
    }

    private void ReleaseRenderTexture()
    {
        if (fovCamera != null)
        {
            fovCamera.targetTexture = null;
        }

        if (fullScreenMaterial != null)
        {
            fullScreenMaterial.SetTexture(texturePropertyId, Texture2D.blackTexture);
        }

        ReleaseRenderTextureInternal();

        lastWidth = -1;
        lastHeight = -1;
    }

    private void ReleaseRenderTextureInternal()
    {
        if (runtimeRenderTexture == null) return;

        if (runtimeRenderTexture.IsCreated())
        {
            runtimeRenderTexture.Release();
        }

        Destroy(runtimeRenderTexture);
        runtimeRenderTexture = null;
    }
}