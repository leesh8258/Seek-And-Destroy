using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    private const float HUD_REFRESH_INTERVAL = 0.1f;
    private const int MAX_SLOT_COUNT = 2;

    [Serializable]
    private struct RoundEndSlotUI
    {
        public TMP_Text scoreText;
        public TMP_Text meText;
    }

    private struct RoundEndSlotState
    {
        public int actorNumber;
        public int startScore;
        public int finalScore;
        public bool isLocal;
    }

    [Header("Phase Roots")]
    [SerializeField] private GameObject countDownRoot;
    [SerializeField] private GameObject gameHUDRoot;
    [SerializeField] private GameObject roundEndRoot;
    [SerializeField] private GameObject gameEndRoot;

    [Header("CountDown")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Game HUD")]
    [SerializeField] private TextMeshProUGUI localPlayerAmmoText;
    [SerializeField] private Image localPlayerWeaponImage;

    [Header("Round End")]
    [SerializeField] private RoundEndSlotUI[] roundEndSlots;

    [Header("Game End")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI returnCountdownText;

    [Header("Fade Overlay")]
    [SerializeField] private Image fadeOverlayImage;
    [SerializeField] private float fadeToBlackSeconds = 1.0f;
    [SerializeField] private float fadeToClearSeconds = 1.0f;

    private PlayerWeaponController localWeapon;
    private Sprite lastWeaponSprite;

    private float hudTimer;
    private Coroutine roundEndRoutine;
    private Coroutine fadeRoutine;
    private RoundManager roundManager;

    private void Start()
    {
        roundManager = RoundManager.Instance;
        if (roundManager == null)
        {
            Debug.LogError("RoundManager is null");
            return;
        }

        roundManager.PhaseChanged += OnPhaseChanged;
        roundManager.CountdownSecondChanged += OnCountdownSecondChanged;

        InitializeFadeOverlay();

        ApplyPhase(roundManager.CurrentPhase);
        ApplyCountdownSecond(0);


        if (roundManager.CurrentPhase == RoundManager.RoundPhase.RoundEnd)
        {
            StartRoundEndSequence();
        }

        else
        {
            ClearRoundEndSlots();
        }

        if (roundManager.CurrentPhase == RoundManager.RoundPhase.GameEnd)
        {
            RefreshGameEndTexts();
        }
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.PhaseChanged -= OnPhaseChanged;
            roundManager.CountdownSecondChanged -= OnCountdownSecondChanged;
        }

        StopRoundEndSequence();
        StopFadeRoutine();
    }

    private void Update()
    {
        if (roundManager == null) return;

        RoundManager.RoundPhase phase = roundManager.CurrentPhase;

        if (phase == RoundManager.RoundPhase.CountDown || phase == RoundManager.RoundPhase.Playing)
        {
            hudTimer += Time.unscaledDeltaTime;
            if (hudTimer >= HUD_REFRESH_INTERVAL)
            {
                hudTimer = 0.0f;
                RefreshLocalHud();
            }
        }

        if (phase == RoundManager.RoundPhase.GameEnd)
        {
            RefreshReturnCountdown();
        }
    }

    public void BindLocalPlayer(PlayerInitializeController player)
    {
        if (player == null) return;
        if (player.photonView == null) return;
        if (!player.photonView.IsMine) return;

        localWeapon = player.WeaponController;
    }

    private void OnPhaseChanged(RoundManager.RoundPhase phase, int sequence)
    {
        ApplyPhase(phase);

        // Loading
        if (phase == RoundManager.RoundPhase.Loading)
        {
            SetFadeBlackImmediate();
        }

        // CountDown
        if (phase == RoundManager.RoundPhase.CountDown)
        {
            StartFadeToClear();
        }

        else
        {
            ApplyCountdownSecond(0);
        }

        // Playing
        if (phase == RoundManager.RoundPhase.Playing)
        {
            SetFadeClearImmediate();
        }

        // RoundEnd
        if (phase == RoundManager.RoundPhase.RoundEnd)
        {
            StartRoundEndSequence();
            StartFadeToBlack();
        }

        else
        {
            StopRoundEndSequence();
        }

        // GameEnd
        if (phase == RoundManager.RoundPhase.GameEnd)
        {
            SetFadeBlackImmediate();
            RefreshGameEndTexts();
        }
    }

    private void OnCountdownSecondChanged(int remainingSec)
    {
        ApplyCountdownSecond(remainingSec);
    }

    private void ApplyPhase(RoundManager.RoundPhase phase)
    {
        SetActiveRoot(countDownRoot, phase == RoundManager.RoundPhase.CountDown);
        SetActiveRoot(gameHUDRoot, phase == RoundManager.RoundPhase.CountDown || phase == RoundManager.RoundPhase.Playing);
        SetActiveRoot(roundEndRoot, phase == RoundManager.RoundPhase.RoundEnd);
        SetActiveRoot(gameEndRoot, phase == RoundManager.RoundPhase.GameEnd);
    }

    private void ApplyCountdownSecond(int remainingSec)
    {
        if (countdownText == null) return;

        if (remainingSec <= 0)
        {
            countdownText.text = string.Empty;
            return;
        }

        countdownText.text = remainingSec.ToString();
    }

    #region Fade Overlay
    private void InitializeFadeOverlay()
    {
        if (fadeOverlayImage == null) return;

        if (!fadeOverlayImage.gameObject.activeSelf)
        {
            fadeOverlayImage.gameObject.SetActive(true);
        }

        SetFadeAlpha(1.0f);
    }

    private void StartFadeToBlack()
    {
        StartFade(1.0f, Mathf.Max(0.01f, fadeToBlackSeconds));
    }

    private void StartFadeToClear()
    {
        StartFade(0.0f, Mathf.Max(0.01f, fadeToClearSeconds));
    }

    private void StartFade(float targetAlpha, float duration)
    {
        if (fadeOverlayImage == null) return;

        float currentAlpha = fadeOverlayImage.color.a;
        if (Mathf.Approximately(currentAlpha, targetAlpha))
        {
            SetFadeAlpha(targetAlpha);
            return;
        }

        StopFadeRoutine();
        fadeRoutine = StartCoroutine(FadeRoutine(currentAlpha, targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration)
    {
        if (fadeOverlayImage == null)
        {
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0.0f;
        SetFadeAlpha(fromAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(toAlpha);
        fadeRoutine = null;
    }

    private void StopFadeRoutine()
    {
        if (fadeRoutine == null) return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    private void SetFadeBlackImmediate()
    {
        StopFadeRoutine();
        SetFadeAlpha(1.0f);
    }

    private void SetFadeClearImmediate()
    {
        StopFadeRoutine();
        SetFadeAlpha(0.0f);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlayImage == null) return;

        Color color = fadeOverlayImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeOverlayImage.color = color;
    }

    #endregion

    #region Game HUD

    private void RefreshLocalHud()
    {
        if (localWeapon == null) return;

        int curAmmo = Mathf.Max(0, localWeapon.CurrentAmmo);
        int maxAmmo = Mathf.Max(1, localWeapon.MaxAmmo);

        if (localPlayerAmmoText != null)
        {
            localPlayerAmmoText.text = curAmmo.ToString() + " / " + maxAmmo.ToString();
        }

        if (localPlayerWeaponImage != null)
        {
            WeaponSO so = localWeapon.CurrentWeaponSO;
            Sprite sprite = so != null ? so.weaponSprite : null;

            if (sprite != lastWeaponSprite)
            {
                lastWeaponSprite = sprite;
                localPlayerWeaponImage.sprite = sprite;
            }
        }

    }

    #endregion

    #region Round End

    private void StartRoundEndSequence()
    {
        StopRoundEndSequence();
        roundEndRoutine = StartCoroutine(RoundEndSequence());
    }

    private void StopRoundEndSequence()
    {
        if (roundEndRoutine == null) return;

        StopCoroutine(roundEndRoutine);
        roundEndRoutine = null;
    }

    private IEnumerator RoundEndSequence()
    {
        if (!TryGetRoomMembers(out InfoService.RoomMemberInfo[] members))
        {
            ClearRoundEndSlots();
            roundEndRoutine = null;
            yield break;
        }

        RoundEndSlotState[] states = new RoundEndSlotState[MAX_SLOT_COUNT];

        NetKeys.TryGetRoomInt(NetKeys.RoomKey.WIN_ACTOR, out int winActor);
        NetKeys.TryGetRoomInt(NetKeys.RoomKey.WINNER_FINAL_SCORE, out int winnerFinalScoreFromRoom);

        for (int i = 0; i < MAX_SLOT_COUNT; i++)
        {
            RoundEndSlotState state = new RoundEndSlotState();
            state.actorNumber = -1;
            state.startScore = 0;
            state.finalScore = 0;
            state.isLocal = false;

            if (i < members.Length)
            {
                InfoService.RoomMemberInfo member = members[i];

                state.actorNumber = member.ActorNumber;
                state.isLocal = member.IsLocal;

                Player player = GetPlayerByActor(member.ActorNumber);
                if (player != null)
                {
                    NetKeys.TryGetPlayerInt(player, NetKeys.PlayerKey.SCORE, out state.finalScore);
                }

                bool isWinnerSlot = state.actorNumber > 0 && state.actorNumber == winActor;
                if (isWinnerSlot)
                {
                    state.finalScore = Mathf.Max(0, winnerFinalScoreFromRoom);
                }

                state.startScore = isWinnerSlot ? Mathf.Max(0, state.finalScore - 1) : state.finalScore;
            }

            states[i] = state;
        }

        ApplyRoundEndSlotStates(states, true);

        yield return new WaitForSecondsRealtime(0.8f);

        ApplyRoundEndSlotStates(states, false);
    }

    private void ApplyRoundEndSlotStates(RoundEndSlotState[] states, bool useStartScore)
    {
        if (states == null) return;
        if (roundEndSlots == null) return;

        int count = Mathf.Min(states.Length, roundEndSlots.Length);
        for (int i = 0; i < count; i++)
        {
            RoundEndSlotUI ui = roundEndSlots[i];
            RoundEndSlotState state = states[i];

            if (ui.meText != null)
            {
                ui.meText.text = state.isLocal ? "[ME]" : string.Empty;
            }

            if (ui.scoreText != null)
            {
                int score = useStartScore ? state.startScore : state.finalScore;
                ui.scoreText.text = score.ToString();
            }
        }
    }

    private void ClearRoundEndSlots()
    {
        if (roundEndSlots == null) return;

        for (int i = 0; i < roundEndSlots.Length; i++)
        {
            if (roundEndSlots[i].scoreText != null)
            {
                roundEndSlots[i].scoreText.text = "0";
            }

            if (roundEndSlots[i].meText != null)
            {
                roundEndSlots[i].meText.text = string.Empty;
            }
        }
    }

    private bool TryGetRoomMembers(out InfoService.RoomMemberInfo[] members)
    {
        members = null;

        if (RoomNetworkManager.Instance == null) return false;
        if (RoomNetworkManager.Instance.Info == null) return false;

        InfoService.RoomLobbyInfo info = RoomNetworkManager.Instance.Info.LastRoomLobbyInfo;
        if (info == null) return false;
        if (info.Members == null) return false;
        if (info.Members.Length == 0) return false;

        members = info.Members;
        return true;
    }

    private Player GetPlayerByActor(int actorNumber)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return null;
        if (actorNumber <= 0) return null;

        return PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
    }

    #endregion

    #region Game End

    private void RefreshGameEndTexts()
    {
        int matchWinActor = -1;
        NetKeys.TryGetRoomInt(NetKeys.RoomKey.MATCH_WIN_ACTOR, out matchWinActor);

        string text = "WIN";

        if (TryGetRoomMembers(out InfoService.RoomMemberInfo[] members))
        {
            for (int i = 0; i < members.Length && i < MAX_SLOT_COUNT; i++)
            {
                if (members[i].ActorNumber == matchWinActor)
                {
                    text = (i + 1).ToString() + "P WIN";
                    break;
                }
            }
        }

        if (winnerText != null)
        {
            winnerText.text = text;
        }

        RefreshReturnCountdown();
    }

    private void RefreshReturnCountdown()
    {
        if (returnCountdownText == null) return;

        if (!NetKeys.TryGetRoomDouble(NetKeys.RoomKey.GAME_END_TS, out double gameEndTs) || gameEndTs <= 0.0d)
        {
            returnCountdownText.text = string.Empty;
            return;
        }

        double elapsed = PhotonNetwork.Time - gameEndTs;
        double remain = (double)roundManager.GameEndShowSeconds - elapsed;
        int remainSec = Mathf.Clamp(Mathf.CeilToInt((float)remain), 0, Mathf.CeilToInt(roundManager.GameEndShowSeconds));
        string remainSecText = remainSec.ToString();

        returnCountdownText.text = $"Return to the room in {remainSecText} seconds";
    }

    #endregion

    private void SetActiveRoot(GameObject target, bool active)
    {
        if (target == null) return;
        if (target.activeSelf == active) return;

        target.SetActive(active);
    }
}