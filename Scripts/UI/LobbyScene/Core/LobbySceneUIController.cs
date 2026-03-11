using UnityEngine;

public class LobbySceneUIController : MonoBehaviour
{
    public enum BaseState
    {
        MainMenu = 0,
        Lobby = 1
    }

    [Header("UI Manager")]
    [SerializeField] private MainMenuUIManager mainMenuUIManager;
    [SerializeField] private LobbyUIManager lobbyUIManager;
    [SerializeField] private OptionUIManager optionUIManager;
    [SerializeField] private JoinUIManager joinUIManager;

    private BaseState currentState;
    private bool isOptionsOpen;
    private bool isJoinOpen;

    #region Initialize & Event
    private void Start()
    {
        Initialize();
        SyncStateFromNetwork();
    }

    private void OnDestroy()
    {
        DisconnectEvent();
    }

    private void Initialize()
    {
        mainMenuUIManager.Init();
        lobbyUIManager.Init();
        optionUIManager.Init();
        joinUIManager.Init();

        mainMenuUIManager.Hide();
        lobbyUIManager.Hide();
        optionUIManager.Hide();
        joinUIManager.Hide();

        isOptionsOpen = false;
        isJoinOpen = false;

        currentState = (BaseState)(-1);

        ConnectEvent();
    }

    private IMenuInterface GetUIManager(BaseState state)
    {
        switch(state)
        {
            case BaseState.MainMenu:
                return mainMenuUIManager;

            case BaseState.Lobby:
                return lobbyUIManager;

            default:
                return null;
        }
    }

    private void ConnectEvent()
    {
        if (RoomNetworkManager.Instance != null)
        {
            RoomNetworkManager.Instance.RoomJoined += OnRoomJoined;
            RoomNetworkManager.Instance.RoomLeft += OnRoomLeft;
            RoomNetworkManager.Instance.JoinRoomFailed += OnJoinRoomFailed;
            RoomNetworkManager.Instance.CreateRoomFailed += OnCreateRoomFailed;
            RoomNetworkManager.Instance.Disconnected += OnDisconnected;
        }

        if (mainMenuUIManager != null)
        {
            mainMenuUIManager.RequestCreateRoom += OnRequestCreateRoom;
            mainMenuUIManager.RequestOpenJoinOverlay += OpenJoinUI;
            mainMenuUIManager.RequestOpenOptionUI += OpenOptionUI;
            mainMenuUIManager.RequestQuitApp += QuitApp;
        }

        if (joinUIManager != null)
        {
            joinUIManager.RequestCloseJoinOverlay += CloseJoinUI;
            joinUIManager.RequestJoinRoom += OnRequestJoinRoom;
        }

        if (lobbyUIManager != null)
        {
            lobbyUIManager.RequestOpenOptionUI += OpenOptionUI;
            lobbyUIManager.RequestLeaveRoom += OnRequestLeaveRoom;
        }

        if (optionUIManager != null)
        {
            optionUIManager.RequestCloseOptionUI += CloseOptionUI;
        }
        

    }

    private void DisconnectEvent()
    {
        if (RoomNetworkManager.Instance != null)
        {
            RoomNetworkManager.Instance.RoomJoined -= OnRoomJoined;
            RoomNetworkManager.Instance.RoomLeft -= OnRoomLeft;
            RoomNetworkManager.Instance.JoinRoomFailed -= OnJoinRoomFailed;
            RoomNetworkManager.Instance.CreateRoomFailed -= OnCreateRoomFailed;
            RoomNetworkManager.Instance.Disconnected -= OnDisconnected;
        }

        if (mainMenuUIManager != null)
        {
            mainMenuUIManager.RequestCreateRoom -= OnRequestCreateRoom;
            mainMenuUIManager.RequestOpenJoinOverlay -= OpenJoinUI;
            mainMenuUIManager.RequestOpenOptionUI -= OpenOptionUI;
            mainMenuUIManager.RequestQuitApp -= QuitApp;

        }

        if (joinUIManager != null)
        {
            joinUIManager.RequestCloseJoinOverlay -= CloseJoinUI;
            joinUIManager.RequestJoinRoom -= OnRequestJoinRoom;
        }


        if (lobbyUIManager != null)
        {
            lobbyUIManager.RequestOpenOptionUI -= OpenOptionUI;
            lobbyUIManager.RequestLeaveRoom -= OnRequestLeaveRoom;
        }

        if (optionUIManager != null)
        {
            optionUIManager.RequestCloseOptionUI -= CloseOptionUI;
        }
    }
    #endregion

    #region Private API
    private void QuitApp()
    {
        Application.Quit();
    }

    private void OpenOptionUI()
    {
        if (isOptionsOpen) return;

        isOptionsOpen = true;
        optionUIManager.Show();
        optionUIManager.Refresh();
    }

    private void CloseOptionUI()
    {
        if (!isOptionsOpen) return;

        isOptionsOpen = false;
        optionUIManager.Hide();
    }

    private void OpenJoinUI()
    {
        if (isJoinOpen) return;

        isJoinOpen = true;
        joinUIManager.Show();
        joinUIManager.Refresh();
    }

    private void CloseJoinUI()
    {
        if (!isJoinOpen) return;

        isJoinOpen = false;
        joinUIManager.Hide();
    }

    private void OnRequestCreateRoom()
    {
        if (RoomNetworkManager.Instance == null) return;
        RoomNetworkManager.Instance.CreateRoomCode();
    }

    private void OnRequestJoinRoom(string code)
    {
        if (RoomNetworkManager.Instance == null) return;
        RoomNetworkManager.Instance.JoinRoom(code);
    }

    private void OnRequestLeaveRoom()
    {
        if (RoomNetworkManager.Instance == null) return;
        RoomNetworkManager.Instance.LeaveRoom();
    }

    private void OnRoomJoined()
    {
        if (isJoinOpen) CloseJoinUI();
        SetState(BaseState.Lobby);
    }

    private void OnRoomLeft()
    {
        SetState(BaseState.MainMenu);
    }

    private void OnJoinRoomFailed(string msg)
    {
        if (!isJoinOpen) OpenJoinUI();
        joinUIManager.SetError(msg);
    }

    private void OnCreateRoomFailed(string msg)
    {
        mainMenuUIManager.SetError(msg);
    }

    private void OnDisconnected()
    {
        SyncStateFromNetwork();
    }

    private void SyncStateFromNetwork()
    {
        if (RoomNetworkManager.Instance != null && RoomNetworkManager.Instance.IsInRoom)
        {
            SetState(BaseState.Lobby);
        }

        else
        {
            SetState(BaseState.MainMenu);
        }

        RefreshCurrentState();
    }
    #endregion

    #region Public API
    public void RefreshCurrentState()
    {
        IMenuInterface currentUI =  GetUIManager(currentState);
        currentUI?.Refresh();
    }

    public void SetState(BaseState next)
    {
        if (next == currentState) return;

        if (isOptionsOpen)
        {
            CloseOptionUI();
        }

        if (isJoinOpen)
        {
            CloseJoinUI();
        }

        IMenuInterface currentUI = GetUIManager(currentState);
        currentUI?.Hide();

        currentState = next;
        IMenuInterface nextUI = GetUIManager(currentState);
        nextUI?.Show();
        nextUI?.Refresh();
    }
    #endregion
}
