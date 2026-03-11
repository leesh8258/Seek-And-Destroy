using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomNetworkManager : MonoBehaviourPunCallbacks
{
    private const byte FIXED_MAX_PLAYERS = 2;
    public static RoomNetworkManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Connect")]
    [SerializeField] private string gameVersion = "1";

    [Header("Room")]
    [SerializeField] private int createRoomRetryLimit = 5;

    [Header("Default Room Settings")]
    [SerializeField] private int defaultMapId = 0;

    private string currentRoomCode;
    private int createRoomRetryCount;
    private bool isConnecting;

    public event Action RoomJoined;
    public event Action RoomLeft;
    public event Action<string> CreateRoomFailed;
    public event Action<string> JoinRoomFailed;
    public event Action Disconnected;

    public LobbyStateService Lobby { get; private set; }
    public MatchNetworkService Match { get; private set; }
    public InfoService Info { get; private set; }

    public bool IsConnectedAndReady => PhotonNetwork.IsConnectedAndReady;
    public bool IsInRoom => PhotonNetwork.InRoom;
    public bool IsMasterClient => PhotonNetwork.IsMasterClient;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;

        SceneManager.sceneLoaded += OnSceneLoaded;

        Lobby = new LobbyStateService();
        Info = new InfoService();
        Match = new MatchNetworkService(lobbySceneName, gameSceneName, defaultMapId, Info);

        Connect();
    }

    private void LateUpdate()
    {
        Info.Tick();
    }


    public void Connect()
    {
        if (PhotonNetwork.IsConnected) return;
        if (isConnecting) return;

        isConnecting = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Disconnect()
    {
        isConnecting = false;

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnConnectedToMaster()
    {
        isConnecting = false;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isConnecting = false;
        currentRoomCode = string.Empty;

        Info.Clear();

        Debug.LogWarning("포톤 연결끊김: " + cause);

        Disconnected?.Invoke();
    }

    public void CreateRoomCode()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Photon 연결이 준비되지 않았습니다.");
            CreateRoomFailed?.Invoke("Photon 연결이 준비되지 않았습니다.");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("이미 방에 참가 중입니다.");
            CreateRoomFailed?.Invoke("이미 방에 참가 중입니다.");

            Info.ForceRefresh();
            RoomJoined?.Invoke();
            return;
        }

        Lobby.InitializeLocalLobbyProperties(LobbyStateService.InitMode.Reset);

        createRoomRetryCount = Mathf.Max(1, createRoomRetryLimit);
        CreateRoomInternal();
    }


    public void JoinRoom(string rawCode)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Photon 연결이 준비되지 않았습니다.");
            JoinRoomFailed?.Invoke("Photon 연결이 준비되지 않았습니다.");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("이미 방에 참가 중입니다.");
            JoinRoomFailed?.Invoke("이미 방에 참가 중입니다.");

            Info.ForceRefresh();
            RoomJoined?.Invoke();
            return;
        }

        if (!RoomCodeService.NormalizeRoomCode(rawCode, out string normalizeCode, out string error))
        {
            Debug.LogWarning(error);
            JoinRoomFailed?.Invoke(error);
            return;
        }

        Lobby.InitializeLocalLobbyProperties(LobbyStateService.InitMode.Reset);
        PhotonNetwork.JoinRoom(normalizeCode);
    }

    public void LeaveRoom()
    {
        if (!PhotonNetwork.InRoom) return;
        PhotonNetwork.LeaveRoom();
    }

    public override void OnCreatedRoom()
    {
        currentRoomCode = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : currentRoomCode;

        Lobby.InitializeLocalLobbyProperties(LobbyStateService.InitMode.Initialize);

        Info.ForceRefresh();
    }

    public override void OnJoinedRoom()
    {
        currentRoomCode = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : currentRoomCode;
        
        Lobby.InitializeLocalLobbyProperties(LobbyStateService.InitMode.Initialize);
        Info.ForceRefresh();

        RoomJoined?.Invoke();
    }

    public override void OnLeftRoom()
    {
        currentRoomCode = string.Empty;
        Info.Clear();

        RoomLeft?.Invoke();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (returnCode == ErrorCode.GameIdAlreadyExists)
        {
            createRoomRetryCount -= 1;
            if (createRoomRetryCount > 0)
            {
                CreateRoomInternal();
                return;
            }

            Debug.LogWarning("방 코드 생성에 실패했습니다. 다시 시도해주세요.");
            CreateRoomFailed?.Invoke("방 코드 생성에 실패했습니다. 다시 시도해주세요.");
            return;
        }

        Debug.LogWarning("방 생성 실패:" + message);
        CreateRoomFailed?.Invoke("방 생성 실패: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("방 입장 실패:" + message);
        JoinRoomFailed?.Invoke("방 입장 실패: " + message);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != lobbySceneName) return;

        if (Match != null)
        {
            Match.NotifyEnteredLobby();
        }
    }

    private void CreateRoomInternal()
    {
        string roomCode = RoomCodeService.GenerateRoomCode();
        currentRoomCode = roomCode;

        // 방 코드로만 진입이 가능하도록
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = FIXED_MAX_PLAYERS;
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = false;

        // 디폴트 값 세팅
        Hashtable roomProperties = new Hashtable();
        roomProperties[NetKeys.RoomKey.PLAY] = false;
        roomProperties[NetKeys.RoomKey.MAP_ID] = defaultMapId;

        roomOptions.CustomRoomProperties = roomProperties;
        PhotonNetwork.CreateRoom(roomCode, roomOptions);
    }

    public bool SetLocalWeapon(int weaponId)
    {
        bool ok = Lobby.SetLocalWeapon(weaponId);
        if (ok) Info.MarkDirty();
        return ok;
    }

    public bool SetLocalCharacter(int characterId)
    {
        bool ok = Lobby.SetLocalCharacter(characterId);
        if (ok) Info.MarkDirty();
        return ok;
    }

    public bool ToggleLocalReady()
    {
        bool ok = Lobby.ToggleLocalReady();
        if (ok) Info.MarkDirty();
        return ok;
    }

    public bool SetRoomMap(int mapId)
    {
        bool ok = Lobby.SetRoomMap(mapId);
        if (ok) Info.MarkDirty();
        return ok;
    }

    public bool TryStartGame()
    {
        bool ok = Match.TryStartGame();
        if (ok) Info.MarkDirty();
        return ok;
    }

    public bool TryReturnToLobbyAfterMatch()
    {
        bool ok = Match.TryReturnToLobbyAfterMatch();
        if (ok) Info.MarkDirty();
        return ok;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => Info.MarkDirty();
    public override void OnPlayerLeftRoom(Player otherPlayer) => Info.MarkDirty();
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) => Info.MarkDirty();
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) => Info.MarkDirty();
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Info.MarkDirty();

        // 인게임 도중 호스트 바뀌었는데 로비 씬이면 게임 씬으로 보정
        if (PhotonNetwork.IsMasterClient)
        {
            if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
            {
                if (SceneManager.GetActiveScene().name == lobbySceneName)
                {
                    PhotonNetwork.LoadLevel(gameSceneName);
                }
            }
        }
    }


}
