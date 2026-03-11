using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSO", menuName = "Scriptable Objects/CharacterSO")]
public class CharacterSO : ScriptableObject
{
    [Header("Prefab")]
    public GameObject characterPrefab;

    [Header("Sprite")]
    public Sprite characterSprite;

    [Header("HP")]
    public int healthPoint;

    [Header("Speed")]
    public int moveSpeed;

    [Header("Sound")]
    public SoundSO HitSound;
    public SoundSO DeathSound;
}
