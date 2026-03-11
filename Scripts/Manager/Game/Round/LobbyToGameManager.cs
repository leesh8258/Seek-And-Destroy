using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class LobbyToGameManager: MonoBehaviourPunCallbacks
{
    private const string PLAYER_PREFAB_NAME = "Player";

    [Header("Managers")]
    [SerializeField] private MapManager mapManager;
    [SerializeField] private GameUIManager gameUIManager;

    private bool isReady;
    private bool hasSpawnedLocalPlayer;

    private void Start()
    {
        TryReadyFromRoom();
    }

    public override void OnJoinedRoom()
    {
        TryReadyFromRoom();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        TryReadyFromRoom();
    }

    public override void OnLeftRoom()
    {
        isReady = false;
        hasSpawnedLocalPlayer = false;
    }

    private void TryReadyFromRoom()
    {
        if (isReady) return;
        if (!PhotonNetwork.InRoom) return;

        Room room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        if (!NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) || !isPlay) return;

        if (!NetKeys.TryGetRoomInt(NetKeys.RoomKey.MAP_ID, out int mapId))
        {
            Debug.LogError("[LobbyToGameManager] Missing room MAP_ID.");
            return;
        }

        if (mapManager == null)
        {
            Debug.LogError("[LobbyToGameManager] MapManager is null.");
            return;
        }

        mapManager.Initialize(mapId);

        if (!mapManager.IsReady)
        {
            Debug.LogError("[LobbyToGameManager] Map/Rule not ready.");
            return;
        }

        SpawnLocalPlayer();
        MarkLocalInitialized();

        if (RoundManager.Instance == null)
        {
            Debug.LogError("[LobbyToGameManager] RoundManager.Instance is null.");
            return;
        }

        RoundManager.Instance.NotifyWorldReady();

        isReady = true;
    }

    private void SpawnLocalPlayer()
    {
        if (hasSpawnedLocalPlayer) return;

        Transform spawn = GetInitialSpawnPoint();
        Vector3 pos = spawn != null ? spawn.position : Vector3.zero;
        Quaternion rot = spawn != null ? spawn.rotation : Quaternion.identity;

        GameObject localPlayer = PhotonNetwork.Instantiate(PLAYER_PREFAB_NAME, pos, rot);
        hasSpawnedLocalPlayer = true;

        if (gameUIManager != null)
        {
            PlayerInitializeController controller = localPlayer.GetComponent<PlayerInitializeController>();
            if (controller != null)
            {
                gameUIManager.BindLocalPlayer(controller);
            }
        }
    }

    private Transform GetInitialSpawnPoint()
    {
        if (mapManager == null)
        {
            return null;
        }

        int actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
        return mapManager.GetSpawnPoint(actorNumber);
    }

    private void MarkLocalInitialized()
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        Hashtable props = new Hashtable();
        props[NetKeys.PlayerKey.INIT] = true;
        NetKeys.SetLocalPlayerProps(props);
    }
}
