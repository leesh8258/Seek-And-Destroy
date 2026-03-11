using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LobbyStateService
{
    public enum InitMode
    {
        Initialize,
        Reset
    }

    public bool SetRoomMap(int mapId) => SetRoomPropertyInt(NetKeys.RoomKey.MAP_ID, mapId);
    private bool SetRoomPropertyInt(string key, int value)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("방에 들어가 있지 않습니다");
            return false;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("호스트만 설정을 변경할 수 있습니다");
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
            Debug.LogWarning("게임이 시작된 후에는 설정을 변경할 수 없습니다");
            return false;
        }

        // 이미 값이 같다면 true만 반환
        if (NetKeys.TryGetRoomInt(key, out int current) && current == value)
        {
            return true;
        }

        Hashtable set = new Hashtable();
        set[key] = value;

        NetKeys.SetRoomProps(set);
        return true;
    }

    public bool SetLocalWeapon(int weaponId)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogWarning("방에 들어가 있지 않습니다");
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
            Debug.LogWarning("게임이 시작된 후에는 무기를 변경할 수 없습니다");
            return false;
        }

        Player localPlayer = PhotonNetwork.LocalPlayer;
        int current = NetKeys.TryGetPlayerInt(localPlayer, NetKeys.PlayerKey.WEAPON_ID, out int w) ? w : 0;
        if (current == weaponId) return true;

        Hashtable set = new Hashtable();
        set[NetKeys.PlayerKey.WEAPON_ID] = weaponId;
        
        NetKeys.SetLocalPlayerProps(set);
        return true;
    }

    public bool SetLocalCharacter(int characterId)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogWarning("방에 들어가 있지 않습니다");
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
            Debug.LogWarning("게임이 시작된 후에는 캐릭터를 변경할 수 없습니다");
            return false;
        }

        Player localPlayer = PhotonNetwork.LocalPlayer;
        int current = NetKeys.TryGetPlayerInt(localPlayer, NetKeys.PlayerKey.CHARACTER_ID, out int c) ? c : 0;
        if (current == characterId) return true;

        Hashtable set = new Hashtable();
        set[NetKeys.PlayerKey.CHARACTER_ID] = characterId;

        NetKeys.SetLocalPlayerProps(set);
        return true;
    }

    public bool ToggleLocalReady()
    {
        Player localPlayer = PhotonNetwork.LocalPlayer;
        bool current = (localPlayer != null) && NetKeys.TryGetPlayerBool(localPlayer, NetKeys.PlayerKey.READY, out bool r) && r;
        return SetLocalReady(!current);
    }

    private bool SetLocalReady(bool isReady)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogWarning("방에 들어가 있지 않습니다");
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
            Debug.LogWarning("게임이 시작된 후에는 Ready를 변경할 수 없습니다");
            return false;
        }

        Player localPlayer = PhotonNetwork.LocalPlayer;
        bool current = NetKeys.TryGetPlayerBool(localPlayer, NetKeys.PlayerKey.READY, out bool ready) && ready;
        if (current == isReady) return true;

        Hashtable set = new Hashtable();
        set[NetKeys.PlayerKey.READY] = isReady;
        
        NetKeys.SetLocalPlayerProps(set);
        return true;
    }

    public void InitializeLocalLobbyProperties(InitMode mode)
    {
        Player localPlayer = PhotonNetwork.LocalPlayer;
        if (localPlayer == null) return;

        Hashtable props = new Hashtable();
        bool needSet = false;

        // READY
        if (mode == InitMode.Reset || !NetKeys.HasPlayerKey(localPlayer, NetKeys.PlayerKey.READY))
        {
            props[NetKeys.PlayerKey.READY] = false;
            needSet = true;
        }

        // CHARACTER_ID
        if (mode == InitMode.Reset || !NetKeys.HasPlayerKey(localPlayer, NetKeys.PlayerKey.CHARACTER_ID))
        {
            props[NetKeys.PlayerKey.CHARACTER_ID] = 0;
            needSet = true;
        }

        // WEAPON_ID
        if (mode == InitMode.Reset || !NetKeys.HasPlayerKey(localPlayer, NetKeys.PlayerKey.WEAPON_ID))
        {
            props[NetKeys.PlayerKey.WEAPON_ID] = 0;
            needSet = true;
        }

        // INIT
        if (mode == InitMode.Reset || !NetKeys.HasPlayerKey(localPlayer, NetKeys.PlayerKey.INIT))
        {
            props[NetKeys.PlayerKey.INIT] = false;
            needSet = true;
        }

        if (needSet)
        {
            NetKeys.SetLocalPlayerProps(props);
        }
    }
}
