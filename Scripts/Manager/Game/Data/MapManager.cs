using System;
using Photon.Pun;
using Photon.Realtime;
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
            spawnPoints = mapView.SpawnPoints;
        }

        IsReady = true;
    }

    public Transform GetSpawnPoint(Player targetPlayer)
    {
        if (targetPlayer == null)
        {
            Debug.LogError("[MapManager] targetPlayer is null.");
            return null;
        }

        int spawnIndex = GetResolvedSpawnIndex(targetPlayer);
        if (spawnIndex < 0)
        {
            return null;
        }

        return spawnPoints[spawnIndex];
    }

    private int GetResolvedSpawnIndex(Player targetPlayer)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[MapManager] SpawnPoints is null or empty.");
            return -1;
        }

        Player[] roomPlayers = PhotonNetwork.PlayerList;
        if (roomPlayers == null || roomPlayers.Length == 0)
        {
            Debug.LogError("[MapManager] Room player list is empty.");
            return -1;
        }

        Player[] orderedPlayers = new Player[roomPlayers.Length];
        Array.Copy(roomPlayers, orderedPlayers, roomPlayers.Length);
        Array.Sort(orderedPlayers, ComparePlayerActorNumber);

        bool[] used = new bool[spawnPoints.Length];

        for (int i = 0; i < orderedPlayers.Length; i++)
        {
            Player currentPlayer = orderedPlayers[i];
            if (currentPlayer == null)
            {
                continue;
            }

            int preferredIndex = GetPreferredSpawnIndex(currentPlayer.ActorNumber);
            int resolvedIndex = FindNextAvailableSpawnIndex(preferredIndex, used);

            if (resolvedIndex < 0)
            {
                Debug.LogError("[MapManager] No available spawn point.");
                return -1;
            }

            used[resolvedIndex] = true;

            if (currentPlayer.ActorNumber == targetPlayer.ActorNumber)
            {
                return resolvedIndex;
            }
        }

        Debug.LogError($"[MapManager] Failed to resolve spawn index for actorNumber={targetPlayer.ActorNumber}");
        return -1;
    }

    private int GetPreferredSpawnIndex(int actorNumber)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return -1;
        }

        if (actorNumber <= 0)
        {
            Debug.LogError($"[MapManager] Invalid actorNumber. actorNumber={actorNumber}");
            return -1;
        }

        return (actorNumber - 1) % spawnPoints.Length;
    }

    private int FindNextAvailableSpawnIndex(int startIndex, bool[] used)
    {
        if (used == null || used.Length == 0)
        {
            return -1;
        }

        if (startIndex < 0)
        {
            return -1;
        }

        for (int offset = 0; offset < used.Length; offset++)
        {
            int index = (startIndex + offset) % used.Length;
            if (!used[index])
            {
                return index;
            }
        }

        return -1;
    }

    private int ComparePlayerActorNumber(Player a, Player b)
    {
        int left = a != null ? a.ActorNumber : int.MaxValue;
        int right = b != null ? b.ActorNumber : int.MaxValue;
        return left.CompareTo(right);
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