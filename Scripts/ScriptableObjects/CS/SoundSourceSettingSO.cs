using UnityEngine;
using UnityEngine.Audio;
using VInspector;

[CreateAssetMenu(fileName = "SoundSourceSettingSO", menuName = "Scriptable Objects/SoundSourceSettingSO")]
public class SoundSourceSettingSO : ScriptableObject
{
    [Header("Mix")]
    public AudioMixerGroup mixerGroup;

    [Header("2D / 3D")]
    [Range(0f, 1f)] public float spatialBlend;

    [DisableIf("spatialBlend", 0f)]
    [Header("3D")]
    public float minDistance;
    public float maxDistance;
    public AudioRolloffMode rolloffMode;
    [EndIf]

    [EnableIf("rolloffMode", AudioRolloffMode.Custom)]
    public AnimationCurve customRolloff;
    [EndIf]

    [Header("Loop")]
    public bool isLoop;

    [Header("Volume / Pitch")]
    public float volume;
    public float pitch;
}
