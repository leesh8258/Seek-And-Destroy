using UnityEngine;

[CreateAssetMenu(fileName = "SoundSO", menuName = "Scriptable Objects/SoundSO")]
public class SoundSO : ScriptableObject
{
    [Header("Type")]
    public SoundType soundType;

    [Header("Sound Clip")]
    public AudioClip soundClip;
}
