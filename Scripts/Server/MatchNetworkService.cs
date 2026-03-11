using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchNetworkService
{
    private readonly string lobbySceneName;
    private readonly string gameSceneName;
    private readonly int defaultMapId;

    private readonly InfoService info;
    private bool isReturningToLobby;

    public MatchNetworkService(string lobbySceneName, string gameSceneName, int defaultMapId, InfoService info)
    {
        this.lobbySceneName = lobbySceneName;
        this.gameSceneName = gameSceneName;
        this.defaultMapId = defaultMapId;
        this.info = info;
    }

    public bool TryStartGame()
    {
        info.ForceRefresh();

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("방에 들어가 있지 않습니다");
            return false;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("호스트만 시작할 수 있습니다");
            return false;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            Debug.LogWarning("2명 이상이 필요합니다");
            return false;
        }

        if (info != null && !info.AllPlayersReady())
        {
            Debug.LogWarning("모든 플레이어가 READY 상태여야 합니다");
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
            return true;
        }

        // 바꿀 값
        Hashtable set = new Hashtable();
        set[NetKeys.RoomKey.PLAY] = true;

        // 현재 이 값이 아니면 업데이트X
        Hashtable expected = new Hashtable();
        expected[NetKeys.RoomKey.PLAY] = false;

        bool requested = PhotonNetwork.CurrentRoom.SetCustomProperties(set, expected);
        if (!requested)
        {
            Debug.LogWarning("게임 시작 요청 거절");
            return false;
        }

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            Hashtable pset = new Hashtable();
            pset[NetKeys.PlayerKey.INIT] = false;
            pset[NetKeys.PlayerKey.DEAD] = false;
            pset[NetKeys.PlayerKey.DEATH_SEQUENCE] = 0;
            pset[NetKeys.PlayerKey.KILLED_BY] = -1;
            pset[NetKeys.PlayerKey.SCORE] = 0;

            NetKeys.SetPlayerProps(players[i], pset);
        }

        if (SceneManager.GetActiveScene().name == lobbySceneName)
        {
            PhotonNetwork.LoadLevel(gameSceneName);
        }

        return true;
    }

    public void NotifyEnteredLobby()
    {
        isReturningToLobby = false;
    }

    public bool TryReturnToLobbyAfterMatch()
    {
        if (isReturningToLobby) return true;
        if (!PhotonNetwork.IsMasterClient) return false;
        if (!PhotonNetwork.InRoom) return false;

        NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool play);
        if (!play) return false;

        // RoomProps 초기화
        Hashtable expected = new Hashtable();
        expected[NetKeys.RoomKey.PLAY] = true;

        Hashtable set = new Hashtable();
        set[NetKeys.RoomKey.PLAY] = false;
        set[NetKeys.RoomKey.MAP_ID] = defaultMapId;

        set[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundManager.RoundPhase.Loading;
        set[NetKeys.RoomKey.ROUND_SEQUENCE] = 0;
        set[NetKeys.RoomKey.ROUND_INDEX] = 0;

        set[NetKeys.RoomKey.WIN_ACTOR] = -1;
        set[NetKeys.RoomKey.ROUND_END_TS] = 0.0d;

        set[NetKeys.RoomKey.MATCH_TARGET_SCORE] = 0;
        set[NetKeys.RoomKey.MATCH_WIN_ACTOR] = -1;
        set[NetKeys.RoomKey.GAME_END_TS] = 0.0d;

        set[NetKeys.RoomKey.ROUND_RESULT_APPLIED_ROUND] = 0;

        bool ok = NetKeys.SetRoomProps(set, expected);
        if (!ok) return false;

        // PlayerProps 초기화
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            Hashtable pset = new Hashtable();
            pset[NetKeys.PlayerKey.READY] = false;
            pset[NetKeys.PlayerKey.INIT] = false;

            pset[NetKeys.PlayerKey.DEAD] = false;
            pset[NetKeys.PlayerKey.DEATH_SEQUENCE] = 0;
            pset[NetKeys.PlayerKey.KILLED_BY] = -1;

            pset[NetKeys.PlayerKey.SCORE] = 0;

            NetKeys.SetPlayerProps(players[i], pset);
        }

        PhotonNetwork.CurrentRoom.IsOpen = true;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        isReturningToLobby = true;

        PhotonNetwork.DestroyAll();
        PhotonNetwork.LoadLevel(lobbySceneName);
        return true;
    }
}
