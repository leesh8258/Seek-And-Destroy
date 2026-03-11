using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour, IMenuInterface
{
    [Header("Player Slots")]
    [SerializeField] private PlayerSlotUI[] slots;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Sub Roots")]
    [SerializeField] private GameObject characterRoot;
    [SerializeField] private GameObject weaponRoot;
    [SerializeField] private GameObject mapRoot;

    [Header("Option / Quit")]
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;

    [Header("Local Buttons")]
    [SerializeField] private Button weaponButton;
    [SerializeField] private Button characterButton;
    [SerializeField] private Button readyButton;

    [Header("Host Buttons")]
    [SerializeField] private Button mapButton;
    [SerializeField] private Image mapButtonImage;
    [SerializeField] private Button startButton;

    [Header("Room Code")]
    [SerializeField] private Button copyRoomCodeButton;
    [SerializeField] private TextMeshProUGUI roomCodeText;

    private bool isHost;
    private bool isGame;
    private bool isReady;

    private int lastMapId = -1;

    public event Action RequestOpenOptionUI;
    public event Action RequestLeaveRoom;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    private void Update()
    {
        if (root == null || !root.activeSelf) return;
        if (RoomNetworkManager.Instance == null) return;

        InfoService.RoomLobbyInfo info = RoomNetworkManager.Instance.Info.LastRoomLobbyInfo;
        if (info == null) return;

        bool isHost = RoomNetworkManager.Instance.IsMasterClient;
        bool isGame = info.InGame;
        bool isReady = GetLocalReady(info);

        // 여기서 이미지, 텍스트 연동 변경하기
        if (this.isHost != isHost || this.isGame != isGame || this.isReady != isReady)
        {
            LockButtonUI(isHost, isGame, isReady);
        }

        // 캐릭터 슬롯 업데이트
        TickSlots(info);

        // 맵 이미지 업데이트
        TickMapButtonImage(info);
    }

    public void Init()
    {
        if (optionButton != null)
            optionButton.onClick.AddListener(() => RequestOpenOptionUI?.Invoke());

        if (quitButton != null)
            quitButton.onClick.AddListener(() => RequestLeaveRoom?.Invoke());

        if (weaponButton != null)
            weaponButton.onClick.AddListener(OnClickWeaponButton);

        if (characterButton != null)
            characterButton.onClick.AddListener(OnClickCharacterButton);

        if (readyButton != null)
            readyButton.onClick.AddListener(OnClickReady);

        if (mapButton != null)
            mapButton.onClick.AddListener(OnClickMapButton);

        if (startButton != null)
            startButton.onClick.AddListener(OnClickStart);

        if (copyRoomCodeButton != null)
            copyRoomCodeButton.onClick.AddListener(OnClickCopyRoomCode);
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
        lastMapId = -1;
        Refresh();
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public void Refresh()
    {
        if (RoomNetworkManager.Instance == null) return;

        InfoService.RoomLobbyInfo info = RoomNetworkManager.Instance.Info.LastRoomLobbyInfo;
        if (info != null)
        {
            bool isHost = RoomNetworkManager.Instance.IsMasterClient;
            bool isGame = info.InGame;
            bool isReady = GetLocalReady(info);
            LockButtonUI(isHost, isGame, isReady);
            TickMapButtonImage(info);

            if (roomCodeText != null) roomCodeText.text = info.RoomCode;
        }
    }

    private void OnClickReady()
    {
        if (RoomNetworkManager.Instance == null) return;

        bool ok = RoomNetworkManager.Instance.ToggleLocalReady();
        if (!ok) return;
    }

    private void OnClickStart()
    {
        if (RoomNetworkManager.Instance == null) return;
        if (!RoomNetworkManager.Instance.IsMasterClient) return;

        bool ok = RoomNetworkManager.Instance.TryStartGame();
        if (!ok) return;
    }

    private void OnClickWeaponButton()
    {
        if (isGame || isReady) return;

        weaponRoot.SetActive(true);
    }

    private void OnClickCharacterButton()
    {
        if (isGame || isReady) return;

        characterRoot.SetActive(true);
    }

    private void OnClickMapButton()
    {
        if (!isHost || isGame || isReady) return;

        mapRoot.SetActive(true);
    }
    
    private void OnClickCopyRoomCode()
    {
        if (RoomNetworkManager.Instance == null || RoomNetworkManager.Instance.Info.LastRoomLobbyInfo == null) return;

        string roomCode = RoomNetworkManager.Instance.Info.LastRoomLobbyInfo.RoomCode;
        if (string.IsNullOrEmpty(roomCode)) return;

        GUIUtility.systemCopyBuffer = roomCode;
    }

    private void LockButtonUI(bool isHost, bool isGame, bool isReady)
    {
        if (weaponButton != null) weaponButton.interactable = !isGame && !isReady;
        if (characterButton != null) characterButton.interactable = !isGame && !isReady;

        if (readyButton != null) readyButton.interactable = !isGame;

        if (mapButton != null) mapButton.interactable = isHost && !isGame && !isReady;
        if (startButton != null) startButton.interactable = isHost && !isGame;

        this.isHost = isHost;
        this.isGame = isGame;
        this.isReady = isReady;
    }

    private bool GetLocalReady(InfoService.RoomLobbyInfo info)
    {
        if (info.Members == null) return false;

        for (int i = 0; i < info.Members.Length; i++)
        {
            if (info.Members[i].IsLocal)
            {
                return info.Members[i].IsReady;
            }
        }

        return false;
    }

    private void TickSlots(InfoService.RoomLobbyInfo info)
    {
        if (slots == null) return;

        int memberCount = 0;
        if (info.Members != null)
        {
           memberCount = info.Members.Length;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < memberCount)
            {
                slots[i].TickOccupied(info.Members[i]);
            }

            else
            {
                slots[i].TickEmpty();
            }
        }
    }

    private void TickMapButtonImage(InfoService.RoomLobbyInfo info)
    {
        if (info == null) return;

        int mapId = info.MapId;

        if (lastMapId == mapId) return;
        lastMapId = mapId;

        Image target = mapButtonImage;
        if (target == null) return;

        if (DBManager.Instance != null && DBManager.Instance.TryGet<MapSO>(mapId, out MapSO mapSO) && mapSO != null)
        {
            target.sprite = mapSO.mapSprite;
        }
    }
}
