using UnityEngine;

public class MapEnvironmentSoundHandler : MonoBehaviour
{
    [SerializeField] private SoundSO hitSound;
    [SerializeField] private SoundSO walkSound;
    [Range(0f, 1f), SerializeField] private float volumeMultiplier;

    public SoundSO HitSound => hitSound;
    public SoundSO WalkSound => walkSound;
    public float VolumeMultiplier => volumeMultiplier;
    
}
