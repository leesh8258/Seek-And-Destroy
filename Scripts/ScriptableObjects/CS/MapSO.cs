using UnityEngine;

[CreateAssetMenu(fileName = "MapSO", menuName = "Scriptable Objects/MapSO")]
public class MapSO : ScriptableObject
{
    [Header("Prefab")]
    public GameObject mapPrefab;

    [Header("Sprite")]
    public Sprite mapSprite;
}
