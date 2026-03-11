using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public static class NetKeys
{
    public static class RoomKey
    {
        public const string PLAY = "pl";
        public const string MAP_ID = "mp";
        public const string RULE_ID = "rd";

        public const string ROUND_PHASE = "rp";
        public const string ROUND_SEQUENCE = "rs";
        public const string ROUND_INDEX = "ri";

        public const string COUNTDOWN_START_TS = "cs";
        public const string COUNTDOWN_DURATION_SECONDS = "cd";

        public const string ROUND_END_TS = "re";

        public const string ROUND_RESULT_APPLIED_ROUND = "rr";

        public const string MATCH_TARGET_SCORE = "mt";
        public const string WIN_ACTOR = "wa";
        public const string WINNER_FINAL_SCORE = "wfs";
        public const string MATCH_WIN_ACTOR = "mwa";

        public const string GAME_END_TS = "ge";

    }

    public static class PlayerKey
    {
        public const string READY = "r";
        public const string CHARACTER_ID = "c";
        public const string WEAPON_ID = "w";

        public const string INIT = "i";

        public const string DEAD = "d";
        public const string DEATH_SEQUENCE = "ds";
        public const string KILLED_BY = "kb";

        public const string SCORE = "sc";
    }

    #region Room CustomProperties
    public static bool TryGetRoomBool(string key, out bool value)
    {
        value = false;

        if (!PhotonNetwork.InRoom) return false;
        Room room = PhotonNetwork.CurrentRoom;
        if (room == null) return false;

        Hashtable props = room.CustomProperties;
        if (props == null) return false;

        if (!props.TryGetValue(key, out object raw)) return false;
        if (raw is bool b)
        {
            value = b;
            return true;
        }

        return false;
    }

    public static bool TryGetRoomInt(string key, out int value)
    {
        value = 0;

        if (!PhotonNetwork.InRoom) return false;
        Room room = PhotonNetwork.CurrentRoom;
        if (room == null) return false;

        Hashtable props = room.CustomProperties;
        if (props == null) return false;

        if (!props.TryGetValue(key, out object raw)) return false;
        if (raw is int i)
        {
            value = i;
            return true;
        }

        return false;
    }

    public static bool TryGetRoomDouble(string key, out double value)
    {
        value = 0.0d;

        if (!PhotonNetwork.InRoom) return false;
        Room room = PhotonNetwork.CurrentRoom;
        if (room == null) return false;

        Hashtable props = room.CustomProperties;
        if (props == null) return false;

        if (!props.TryGetValue(key, out object raw)) return false;
        if (raw is double d)
        {
            value = d;
            return true;
        }

        return false;
    }

    public static bool SetRoomProps(Hashtable set, Hashtable expected = null)
    {
        if (!PhotonNetwork.InRoom) return false;
        Room room = PhotonNetwork.CurrentRoom;
        if (room == null) return false;
        if (set == null || set.Count == 0) return false;

        return room.SetCustomProperties(set, expected);
    }

    #endregion

    #region Player CustomProperties
    public static bool HasPlayerKey(Player player, string key)
    {
        if (player == null) return false;

        Hashtable props = player.CustomProperties;
        return props != null && props.ContainsKey(key);
    }

    public static bool TryGetPlayerBool(Player player, string key, out bool value)
    {
        value = false;

        if (player == null) return false;
        Hashtable props = player.CustomProperties;
        if (props == null) return false;

        if (!props.TryGetValue(key, out object raw)) return false;
        if (raw is bool b)
        {
            value = b;
            return true;
        }

        return false;
    }

    public static bool TryGetPlayerInt(Player player, string key, out int value)
    {
        value = 0;

        if (player == null) return false;
        Hashtable props = player.CustomProperties;
        if (props == null) return false;

        if (!props.TryGetValue(key, out object raw)) return false;
        if (raw is int i)
        {
            value = i;
            return true;
        }

        return false;
    }

    public static bool SetPlayerProps(Player player, Hashtable set, Hashtable expected = null)
    {
        if (player == null) return false;
        if (set == null || set.Count == 0) return false;

        return player.SetCustomProperties(set, expected);
    }

    public static bool TryConfirmDeathOnMaster(Player targetPlayer, int killerActorNumber, out int confirmedDeathSequence)
    {
        confirmedDeathSequence = 0;

        if (!PhotonNetwork.IsMasterClient) return false;
        if (targetPlayer == null) return false;
        if (TryGetPlayerBool(targetPlayer, PlayerKey.DEAD, out bool isDead) && isDead) return false;

        int prevSeq = 0;
        bool hasSeq = TryGetPlayerInt(targetPlayer, PlayerKey.DEATH_SEQUENCE, out int seqRaw);
        if (hasSeq) prevSeq = Mathf.Max(0, seqRaw);

        // 키가 없을 수 있으니 baseline을 먼저 넣어줌 (초기화가 아직 안 된 방어 코드)
        bool hasDeadKey = HasPlayerKey(targetPlayer, PlayerKey.DEAD);
        bool hasSeqKey = HasPlayerKey(targetPlayer, PlayerKey.DEATH_SEQUENCE);
        bool hasKbKey = HasPlayerKey(targetPlayer, PlayerKey.KILLED_BY);

        if (!hasDeadKey || !hasSeqKey || !hasKbKey)
        {
            Hashtable baseline = new Hashtable();
            baseline[PlayerKey.DEAD] = false;
            baseline[PlayerKey.DEATH_SEQUENCE] = prevSeq;
            baseline[PlayerKey.KILLED_BY] = -1;
            SetPlayerProps(targetPlayer, baseline);
        }

        int nextSeq = prevSeq + 1;

        Hashtable set = new Hashtable();
        set[PlayerKey.DEAD] = true;
        set[PlayerKey.DEATH_SEQUENCE] = nextSeq;
        set[PlayerKey.KILLED_BY] = killerActorNumber;

        // CAS: DEAD=false & DEATH_SEQUENCE=prevSeq 일 때만 확정 성공
        Hashtable expected = new Hashtable();
        expected[PlayerKey.DEAD] = false;
        expected[PlayerKey.DEATH_SEQUENCE] = prevSeq;

        bool ok = SetPlayerProps(targetPlayer, set, expected);
        if (ok)
        {
            confirmedDeathSequence = nextSeq;
        }

        return ok;
    }

    #endregion

    #region LocalPlayer CustomProperties
    public static bool SetLocalPlayerProps(Hashtable set, Hashtable expected = null)
    {
        Player local = PhotonNetwork.LocalPlayer;
        if (local == null) return false;

        return SetPlayerProps(local, set, expected);
    }
    #endregion
}
