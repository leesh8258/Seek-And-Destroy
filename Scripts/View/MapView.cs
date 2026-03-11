using UnityEngine;

public class MapView : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    public Transform[] SpawnPoints => spawnPoints;
}
