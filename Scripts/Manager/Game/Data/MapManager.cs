using System;
using UnityEngine;
using VInspector;

public class MapManager : MonoBehaviour
{
    [Header("Current MapSO")]
    [SerializeField, ReadOnly] private MapSO currentMapSO;
    [SerializeField, ReadOnly] private GameObject currentMapInstance;

    private Transform[] spawnPoints;

    public bool IsReady { get; private set; }

    public void Initialize(int mapId)
    {
        ClearCurrentMap();

        if (DBManager.Instance == null)
        {
            Debug.LogError("[MapManager] DBManager instance is null. Cannot initialize MapManager.");
            return;
        }

        if (!DBManager.Instance.TryGet<MapSO>(mapId, out MapSO mapSO))
        {
            Debug.LogError($"[MapManager] MapSO not found for mapId={mapId}");
            return;
        }

        currentMapSO = mapSO;
        GameObject prefab = currentMapSO.mapPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[MapManager] mapPrefab is null for mapId={mapId}");
            return;
        }

        currentMapInstance = Instantiate(prefab);
        MapView mapView = currentMapInstance.GetComponentInChildren<MapView>();
        if (mapView == null)
        {
            Debug.LogError("[MapManager] MapSpawnPoints component not found in map prefab.");
            spawnPoints = Array.Empty<Transform>();
        }

        else
        {
            Transform[] points = mapView.SpawnPoints;
            spawnPoints = points;
        }

        IsReady = true;
    }

    public int GetSpawnIndex(int actorNumber)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[MapManager] SpawnPoints is null or empty.");
            return -1;
        }

        if (actorNumber <= 0)
        {
            Debug.LogError($"[MapManager] Invalid actorNumber. actorNumber={actorNumber}");
            return -1;
        }

        return (actorNumber - 1) % spawnPoints.Length;
    }


    public Transform GetSpawnPoint(int actorNumber)
    {
        int spawnIndex = GetSpawnIndex(actorNumber);
        if (spawnIndex < 0)
        {
            return null;
        }

        return spawnPoints[spawnIndex];
    }

    private void ClearCurrentMap()
    {
        IsReady = false;

        if (currentMapInstance != null)
        {
            Destroy(currentMapInstance);
            currentMapInstance = null;
        }

        currentMapSO = null;
        spawnPoints = Array.Empty<Transform>();
    }

}
