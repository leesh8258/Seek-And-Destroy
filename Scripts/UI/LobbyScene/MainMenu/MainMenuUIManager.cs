using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour, IMenuInterface
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Main Buttons")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;

    [Header("Error Text")]
    [SerializeField] private TextMeshProUGUI errorText;

    public event Action RequestCreateRoom;
    public event Action RequestOpenJoinOverlay;
    public event Action RequestOpenOptionUI;
    public event Action RequestQuitApp;

    public void Init()
    {
        if (root != null) root.SetActive(false);

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(() => RequestCreateRoom?.Invoke());

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(() => RequestOpenJoinOverlay?.Invoke());

        if (optionButton != null)
            optionButton.onClick.AddListener(() => RequestOpenOptionUI?.Invoke());

        if (quitButton != null)
            quitButton.onClick.AddListener(() => RequestQuitApp?.Invoke());
    }

    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public void Refresh()
    {
        SetError(string.Empty);
    }

    public void SetError(string message)
    {
        if (errorText == null) return;

        errorText.text = message;
        errorText.gameObject.SetActive(!string.IsNullOrWhiteSpace(errorText.text));
    }
}
