using UnityEngine;

public class FrameRateLimitManager : MonoBehaviour
{
    [SerializeField] private int targetFrameRate;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}
