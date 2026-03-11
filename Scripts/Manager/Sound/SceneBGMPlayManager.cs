using UnityEngine;

public class SceneBGMPlayManager : MonoBehaviour
{
    [SerializeField] private SoundSO bgmSound;

    private void Start()
    {
        if (SoundManager.Instance == null) return;
        if (bgmSound == null) SoundManager.Instance.StopBGM();

        SoundManager.Instance.PlayBGM(bgmSound);
    }
}