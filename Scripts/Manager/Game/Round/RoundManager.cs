using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

public class RoundManager : MonoBehaviourPunCallbacks
{
    public enum RoundPhase
    {
        Loading = 0,
        CountDown = 1,
        Playing = 2,
        RoundEnd = 3,
        GameEnd = 4
    }

    public static RoundManager Instance;

    private const int COUNTDOWN_DURATION_SECONDS_FIXED = 3;
    private const double VICTORY_CHECK_INTERVAL = 0.5;

    [SerializeField] private int matchPointToWin = 3;
    [SerializeField] private float roundEndShowSeconds = 3.0f;
    [SerializeField] private float gameEndShowSeconds = 5.0f;

    public RoundPhase CurrentPhase { get; private set; }
    public int CurrentSequence { get; private set; }
    public int CurrentRoundIndex { get; private set; }

    public bool IsMatchActive { get; private set; }
    public bool IsWorldReady { get; private set; }

    public event Action<RoundPhase, int> PhaseChanged;
    public event Action<int> RoundIndexChanged;
    public event Action<int> CountdownSecondChanged;

    private int lastCountdownSecond = -1;
    private double nextVictoryCheckTime;

    public int MatchPointToWin => Mathf.Max(matchPointToWin, 1);
    public float GameEndShowSeconds => gameEndShowSeconds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom)
        {
            if (IsMatchActive) ResetLocalState();
            return;
        }

        if (!NetKeys.TryGetRoomBool(NetKeys.RoomKey.PLAY, out bool isPlay) || !isPlay)
        {
            if (IsMatchActive) ResetLocalState();
            return;
        }

        if (!IsMatchActive) IsMatchActive = true;

        switch (CurrentPhase)
        {
            case RoundPhase.Loading:
                TickLoadingPhase();
                break;

            case RoundPhase.CountDown:
                TickCountdownPhase();
                break;

            case RoundPhase.Playing:
                TickPlayingPhase();
                break;

            case RoundPhase.RoundEnd:
                TickRoundEndPhase();
                break;

            case RoundPhase.GameEnd:
                TickGameEndPhase();
                break;
        }
    }

    public void NotifyWorldReady()
    {
        if (IsWorldReady) return;
        IsWorldReady = true;
    }

    #region Snapshot / Reset

    private void ResetLocalState()
    {
        IsMatchActive = false;
        IsWorldReady = false;

        CurrentPhase = RoundPhase.Loading;
        CurrentSequence = 0;
        CurrentRoundIndex = 0;

        lastCountdownSecond = -1;
        nextVictoryCheckTime = 0.0d;
    }

    private void ApplyRoomProperty()
    {
        if (!NetKeys.TryGetRoomInt(NetKeys.RoomKey.ROUND_SEQUENCE, out int sequence)) return;
        if (!NetKeys.TryGetRoomInt(NetKeys.RoomKey.ROUND_INDEX, out int roundIndex)) return;
        if (!NetKeys.TryGetRoomInt(NetKeys.RoomKey.ROUND_PHASE, out int phaseInt)) return;

        CurrentSequence = sequence;

        if (CurrentRoundIndex != roundIndex)
        {
            CurrentRoundIndex = roundIndex;
            RoundIndexChanged?.Invoke(CurrentRoundIndex);
        }

        RoundPhase newPhase = (RoundPhase)Mathf.Clamp(phaseInt, (int)RoundPhase.Loading, (int)RoundPhase.GameEnd);
        if (newPhase == CurrentPhase) return;

        CurrentPhase = newPhase;
        lastCountdownSecond = -1;

        if (CurrentPhase == RoundPhase.Playing)
        {
            nextVictoryCheckTime = PhotonNetwork.Time;
        }

        PhaseChanged?.Invoke(CurrentPhase, CurrentSequence);
    }

    #endregion

    #region Phase Ticks

    private void TickLoadingPhase()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        MasterRoundInitialize();

        if (!IsWorldReady) return;
        if (!MasterAllPlayersInitialized()) return;

        MasterStartCountdown();
    }

    private void TickCountdownPhase()
    {
        if (!NetKeys.TryGetRoomDouble(NetKeys.RoomKey.COUNTDOWN_START_TS, out double startTs)) return;
        if (!NetKeys.TryGetRoomInt(NetKeys.RoomKey.COUNTDOWN_DURATION_SECONDS, out int durationSeconds)) return;

        int remainSeconds = CalculateCountdownSeconds(startTs, durationSeconds);
        if (remainSeconds != lastCountdownSecond)
        {
            lastCountdownSecond = remainSeconds;
            CountdownSecondChanged?.Invoke(remainSeconds);
        }

        if (!PhotonNetwork.IsMasterClient) return;
        if (!IsCountdownEndSeconds(startTs, durationSeconds)) return;

        MasterEnterPlaying();
    }

    private void TickPlayingPhase()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        double nowTime = PhotonNetwork.Time;
        if (nowTime < nextVictoryCheckTime) return;

        nextVictoryCheckTime = nowTime + VICTORY_CHECK_INTERVAL;
        MasterEvaluateEndRound();
    }

    private void TickRoundEndPhase()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        MasterRoundEndSet();
    }

    private void TickGameEndPhase()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        MasterGameEndSet();
    }

    #endregion

    #region Countdown Math

    private int CalculateCountdownSeconds(double startTs, int durationSeconds)
    {
        double elapsedSeconds = PhotonNetwork.Time - startTs;
        double remainSeconds = (double)durationSeconds - elapsedSeconds;

        if (remainSeconds <= 0.0d) return 0;

        int sec = Mathf.CeilToInt((float)remainSeconds);
        return Mathf.Clamp(sec, 0, durationSeconds);
    }

    private bool IsCountdownEndSeconds(double startTs, int durationSeconds)
    {
        double elapsedSeconds = PhotonNetwork.Time - startTs;
        return elapsedSeconds >= (double)durationSeconds;
    }

    #endregion

    #region CAS

    private void MasterRoundInitialize()
    {
        if (NetKeys.TryGetRoomInt(NetKeys.RoomKey.ROUND_SEQUENCE, out int sequence) && sequence > 0) return;

        Hashtable set = new Hashtable();
        set[NetKeys.RoomKey.ROUND_INDEX] = 1;
        set[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.Loading;
        set[NetKeys.RoomKey.ROUND_SEQUENCE] = 1;

        set[NetKeys.RoomKey.WIN_ACTOR] = -1;
        set[NetKeys.RoomKey.WINNER_FINAL_SCORE] = 0;
        set[NetKeys.RoomKey.ROUND_END_TS] = 0.0d;

        set[NetKeys.RoomKey.ROUND_RESULT_APPLIED_ROUND] = 0;

        set[NetKeys.RoomKey.GAME_END_TS] = 0.0d;
        set[NetKeys.RoomKey.MATCH_WIN_ACTOR] = -1;

        NetKeys.SetRoomProps(set);
    }

    private void MasterStartCountdown()
    {
        if (CurrentPhase == RoundPhase.CountDown) return;

        Hashtable set = new Hashtable();
        set[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.CountDown;
        set[NetKeys.RoomKey.COUNTDOWN_START_TS] = PhotonNetwork.Time;
        set[NetKeys.RoomKey.COUNTDOWN_DURATION_SECONDS] = COUNTDOWN_DURATION_SECONDS_FIXED;
        set[NetKeys.RoomKey.ROUND_SEQUENCE] = CurrentSequence + 1;

        Hashtable expected = new Hashtable();
        expected[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.Loading;

        NetKeys.SetRoomProps(set, expected);
    }

    private void MasterEnterPlaying()
    {
        if (CurrentPhase == RoundPhase.Playing) return;

        Hashtable set = new Hashtable();
        set[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.Playing;
        set[NetKeys.RoomKey.ROUND_SEQUENCE] = CurrentSequence + 1;

        Hashtable expected = new Hashtable();
        expected[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.CountDown;

        NetKeys.SetRoomProps(set, expected);
    }

    private void MasterRoundEndSet()
    {
        if (!NetKeys.TryGetRoomDouble(NetKeys.RoomKey.ROUND_END_TS, out double roundEndTs)) return;
        if (roundEndTs <= 0.0d) return;

        if (!NetKeys.TryGetRoomInt(NetKeys.RoomKey.ROUND_RESULT_APPLIED_ROUND, out int lastApplied)) return;
        if (lastApplied == CurrentRoundIndex) return;

        double nowTime = PhotonNetwork.Time;
        if (nowTime < roundEndTs + roundEndShowSeconds) return;

        int target = MatchPointToWin;

        if (!NetKeys.TryGetRoomInt(NetKeys.RoomKey.WIN_ACTOR, out int winActor)) return;

        Player winner = GetPlayerByActor(winActor);
        if (winner == null) return;

        if (!NetKeys.TryGetPlayerInt(winner, NetKeys.PlayerKey.SCORE, out int currentScore)) return;

        bool matchEnded = (currentScore >= target);
        int nextRoundIndex = CurrentRoundIndex + 1;

        Hashtable expected = new Hashtable();
        expected[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.RoundEnd;
        expected[NetKeys.RoomKey.ROUND_INDEX] = CurrentRoundIndex;
        expected[NetKeys.RoomKey.ROUND_RESULT_APPLIED_ROUND] = lastApplied;

        Hashtable set = new Hashtable();
        set[NetKeys.RoomKey.ROUND_SEQUENCE] = CurrentSequence + 1;
        set[NetKeys.RoomKey.MATCH_TARGET_SCORE] = target;
        set[NetKeys.RoomKey.ROUND_RESULT_APPLIED_ROUND] = CurrentRoundIndex;

        if (matchEnded)
        {
            set[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.GameEnd;
            set[NetKeys.RoomKey.GAME_END_TS] = nowTime;
            set[NetKeys.RoomKey.MATCH_WIN_ACTOR] = winActor;
        }

        else
        {
            set[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.CountDown;
            set[NetKeys.RoomKey.ROUND_INDEX] = nextRoundIndex;

            set[NetKeys.RoomKey.COUNTDOWN_START_TS] = nowTime;
            set[NetKeys.RoomKey.COUNTDOWN_DURATION_SECONDS] = COUNTDOWN_DURATION_SECONDS_FIXED;

            set[NetKeys.RoomKey.WIN_ACTOR] = -1;
            set[NetKeys.RoomKey.WINNER_FINAL_SCORE] = 0;
            set[NetKeys.RoomKey.ROUND_END_TS] = 0.0d;
        }

        if (!NetKeys.SetRoomProps(set, expected)) return;

        if (!matchEnded) MasterResetPlayerPerRound();
    }

    private Player GetPlayerByActor(int actorNumber)
    {
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            Player p = players[i];
            if (p != null && p.ActorNumber == actorNumber) return p;
        }

        return null;
    }

    private void MasterResetPlayerPerRound()
    {
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            Player p = players[i];
            if (p == null) continue;

            Hashtable pset = new Hashtable();
            pset[NetKeys.PlayerKey.DEAD] = false;
            pset[NetKeys.PlayerKey.DEATH_SEQUENCE] = 0;
            pset[NetKeys.PlayerKey.KILLED_BY] = -1;

            NetKeys.SetPlayerProps(p, pset);
        }
    }

    private void MasterGameEndSet()
    {
        if (!NetKeys.TryGetRoomDouble(NetKeys.RoomKey.GAME_END_TS, out double gameEndTs)) return;
        if (gameEndTs <= 0.0d) return;

        double nowTime = PhotonNetwork.Time;
        if (nowTime < gameEndTs + gameEndShowSeconds) return;

        RoomNetworkManager room = RoomNetworkManager.Instance;
        if (room == null) return;

        room.TryReturnToLobbyAfterMatch();
    }

    #endregion

    #region Callbacks

    public override void OnJoinedRoom()
    {
        ApplyRoomProperty();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        ApplyRoomProperty();
    }

    public override void OnLeftRoom()
    {
        if (IsMatchActive)
        {
            ResetLocalState();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (CurrentPhase != RoundPhase.Playing) return;
        if (changedProps == null || !changedProps.ContainsKey(NetKeys.PlayerKey.DEAD)) return;

        MasterEvaluateEndRound();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (CurrentPhase != RoundPhase.Playing) return;

        MasterEvaluateEndRound();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (CurrentPhase != RoundPhase.Playing) return;

        MasterEvaluateEndRound();
    }

    private bool MasterAllPlayersInitialized()
    {
        Player[] players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0) return false;

        for (int i = 0; i < players.Length; i++)
        {
            Player p = players[i];
            if (p == null) return false;

            if (!NetKeys.TryGetPlayerBool(p, NetKeys.PlayerKey.INIT, out bool init)) return false;
            if (!init) return false;
        }

        return true;
    }

    #endregion

    #region Victory Evaluation

    private void MasterEvaluateEndRound()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;
        if (CurrentPhase != RoundPhase.Playing) return;

        Player[] players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0) return;

        if (players.Length == 1)
        {
            Player player = players[0];
            if (player != null)
            {
                MasterSetWinner(player.ActorNumber);
            }

            return;
        }

        TryEndRoundByFFA(players);
    }

    private void TryEndRoundByFFA(Player[] players)
    {
        int aliveCount = 0;
        int lastAliveActor = -1;

        for (int i = 0; i < players.Length; i++)
        {
            Player p = players[i];
            if (p == null) continue;

            if (!NetKeys.TryGetPlayerBool(p, NetKeys.PlayerKey.DEAD, out bool isDead)) return;
            if (isDead) continue;

            aliveCount += 1;
            lastAliveActor = p.ActorNumber;

            if (aliveCount >= 2) return;
        }

        if (aliveCount == 1) MasterSetWinner(lastAliveActor);
    }

    private void MasterSetWinner(int winActorNumber)
    {
        Room room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        if (!NetKeys.TryGetRoomInt(NetKeys.RoomKey.ROUND_PHASE, out int currentPhase)) return;
        if ((RoundPhase)currentPhase != RoundPhase.Playing) return;

        Player winner = GetPlayerByActor(winActorNumber);
        if (winner == null) return;

        int prevScore = 0;
        bool hasScore = NetKeys.TryGetPlayerInt(winner, NetKeys.PlayerKey.SCORE, out int scoreRaw);
        if (hasScore)
        {
            prevScore = Mathf.Max(0, scoreRaw);
        }

        int nextScore = prevScore + 1;

        Hashtable pExpected = new Hashtable();
        pExpected[NetKeys.PlayerKey.SCORE] = prevScore;

        Hashtable pSet = new Hashtable();
        pSet[NetKeys.PlayerKey.SCORE] = nextScore;

        if (!NetKeys.SetPlayerProps(winner, pSet, pExpected))
        {
            return;
        }

        Hashtable expected = new Hashtable();
        expected[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.Playing;
        expected[NetKeys.RoomKey.WIN_ACTOR] = -1;

        Hashtable set = new Hashtable();
        set[NetKeys.RoomKey.WIN_ACTOR] = winActorNumber;
        set[NetKeys.RoomKey.WINNER_FINAL_SCORE] = nextScore;
        set[NetKeys.RoomKey.ROUND_END_TS] = PhotonNetwork.Time;
        set[NetKeys.RoomKey.ROUND_PHASE] = (int)RoundPhase.RoundEnd;
        set[NetKeys.RoomKey.ROUND_SEQUENCE] = CurrentSequence + 1;

        if (!NetKeys.SetRoomProps(set, expected))
        {
            Debug.LogWarning("[RoundManager] MasterSetWinner RoomKey Set Error");
        }
    }

    #endregion
}
