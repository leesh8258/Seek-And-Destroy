using Photon.Pun;
using TMPro;
using UnityEngine;

public class LoadingOverlayManager : MonoBehaviour
{
    private static LoadingOverlayManager Instance;
    private const float INTERVAL = 0.1f;

    [Header("UI")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private TMP_Text detailText;

    private float timer;

    private bool lastVisible;
    private int lastPercent = -1;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Apply(false, -1);
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer < INTERVAL) return;
        timer = 0f;

        float progress = PhotonNetwork.LevelLoadingProgress;
        int percent = 0;

        bool visible = progress > 0.0f && progress < 1.0f;
        if (visible)
        {
            percent = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100);
        }

        else
        {
            percent = -1;
        }

        if (visible == lastVisible && percent == lastPercent) return;

        Apply(visible, percent);
    }

    private void Apply(bool visible, int percent)
    {
        lastVisible = visible;
        lastPercent = percent;

        if (overlayGroup != null)
        {
            overlayGroup.alpha = visible ? 1f : 0f;
            overlayGroup.interactable = visible;
            overlayGroup.blocksRaycasts = visible;
        }

        if (detailText != null)
        {
            detailText.text = percent.ToString() + "%";
        }
    }
}
