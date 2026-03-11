using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;

public class InfoService
{
    [Serializable]
    public struct RoomMemberInfo
    {
        public int ActorNumber;
        public bool IsMasterClient;
        public bool IsLocal;

        public int CharacterId;
        public int WeaponId;

        public bool IsReady;
    }

    [Serializable]
    public class RoomLobbyInfo
    {
        public string RoomCode;
        public int PlayerCount;
        public int MaxPlayers;

        public bool InGame;
        public bool IsAllReady;

        public int MapId;

        public RoomMemberInfo[] Members;
    }

    private bool dirty;

    private RoomLobbyInfo lastRoomLobbyInfo;
    public RoomLobbyInfo LastRoomLobbyInfo => lastRoomLobbyInfo;
    public void MarkDirty() => dirty = true;

    public void Tick()
    {
        if (!dirty) return;
        dirty = false;
        Refresh();
    }

    public void ForceRefresh()
    {
        dirty = false;
        Refresh();
    }

    public void Clear()
    {
        dirty = false;
        lastRoomLobbyInfo = null;
    }

    private void Refresh()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            lastRoomLobbyInfo = null;
            return;
        }

        Room room = PhotonNetwork.CurrentRoom;

        RoomLobbyInfo info = new RoomLobbyInfo();
        info.RoomCode = room.Name;
        info.PlayerCount = room.PlayerCount;
        info.MaxPlayers = room.MaxPlayers;

        info.InGame = NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool inGame) && inGame;

        if (NetKeys.TryGetRoomInt(NetKeys.RoomKey.MAP_ID, out int mapId))
        {
            info.MapId = mapId;
        }

        info.IsAllReady = AllPlayersReady();

        Player[] players = PhotonNetwork.PlayerList;
        List<RoomMemberInfo> members = new List<RoomMemberInfo>(players != null ? players.Length : 0);

        int localActor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;

        if (players != null)
        {
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (p == null) continue;

                RoomMemberInfo member = new RoomMemberInfo();
                member.ActorNumber = p.ActorNumber;
                member.IsMasterClient = member.ActorNumber == room.MasterClientId;
                member.IsLocal = member.ActorNumber == localActor;
                
                member.CharacterId = NetKeys.TryGetPlayerInt(p, NetKeys.PlayerKey.CHARACTER_ID, out int cid) ? cid : 0;
                member.WeaponId = NetKeys.TryGetPlayerInt(p, NetKeys.PlayerKey.WEAPON_ID, out int wid) ? wid : 0;
                member.IsReady = NetKeys.TryGetPlayerBool(p, NetKeys.PlayerKey.READY, out bool ready) && ready;

                members.Add(member);
            }
        }

        info.Members = members.ToArray();
        lastRoomLobbyInfo = info;
    }

    public bool AllPlayersReady()
    {
        Player[] players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0) return false;

        for (int i = 0; i < players.Length; i++)
        {
            Player p = players[i];
            if (p == null) return false;

            if (!NetKeys.TryGetPlayerBool(p, NetKeys.PlayerKey.READY, out bool ready) || !ready)
                return false;
        }

        return true;
    }
}
