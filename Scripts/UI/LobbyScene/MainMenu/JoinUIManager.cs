using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinUIManager : MonoBehaviour, IMenuInterface
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Input")]
    [SerializeField] private TMP_InputField roomCodeInput;

    [Header("Button")]
    [SerializeField] private Button joinApplyButton;
    [SerializeField] private Button joinQuitButton;

    [Header("Error")]
    [SerializeField] private TextMeshProUGUI joinErrorText;

    public event Action<string> RequestJoinRoom;
    public event Action RequestCloseJoinOverlay;

    public void Init()
    {
        if (root != null) root.SetActive(false);

        if (joinQuitButton != null)
            joinQuitButton.onClick.AddListener(() => RequestCloseJoinOverlay?.Invoke());

        if (joinApplyButton != null)
            joinApplyButton.onClick.AddListener(OnClickJoinApply);

        ClearUI();
    }

    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        if (roomCodeInput != null)
        {
            roomCodeInput.text = string.Empty;
            roomCodeInput.Select();
            roomCodeInput.ActivateInputField();
        }

        SetError(string.Empty);
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        ClearUI();
    }

    public void Refresh()
    {
        ClearUI();
    }

    private void OnClickJoinApply()
    {
        string code = roomCodeInput != null ? roomCodeInput.text : string.Empty;
        code = (code ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(code))
        {
            SetError("Please enter room code again.");
            return;
        }

        RequestJoinRoom?.Invoke(code);
    }

    private void ClearUI()
    {
        if (roomCodeInput != null) roomCodeInput.text = string.Empty;
        SetError(string.Empty);
    }

    public void SetError(string message)
    {
        if (joinErrorText == null) return;

        joinErrorText.text = message;
        joinErrorText.gameObject.SetActive(!string.IsNullOrWhiteSpace(joinErrorText.text));
    }
}
