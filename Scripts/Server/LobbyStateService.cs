using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

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
            return false;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
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
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
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
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
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
            return false;
        }

        if (NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) && isPlay)
        {
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

        if (mode == InitMode.Reset || !NetKeys.HasPlayerKey(localPlayer, NetKeys.PlayerKey.READY))
        {
            props[NetKeys.PlayerKey.READY] = false;
            needSet = true;
        }

        if (mode == InitMode.Reset || !NetKeys.HasPlayerKey(localPlayer, NetKeys.PlayerKey.CHARACTER_ID))
        {
            props[NetKeys.PlayerKey.CHARACTER_ID] = 0;
            needSet = true;
        }

        if (mode == InitMode.Reset || !NetKeys.HasPlayerKey(localPlayer, NetKeys.PlayerKey.WEAPON_ID))
        {
            props[NetKeys.PlayerKey.WEAPON_ID] = 0;
            needSet = true;
        }

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
