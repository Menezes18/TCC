using TMPro;
using UnityEngine;


public class RoomCodeDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private string prefix = "Código: ";

    private SteamLobby _steamLobby;

    private void Awake()
    {
        if (codeText == null)
            codeText = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        _steamLobby = SteamLobby.instance ?? FindObjectOfType<SteamLobby>();
        if (_steamLobby != null)
        {
            _steamLobby.RoomCodeUpdated += OnRoomCodeUpdated;
            OnRoomCodeUpdated(_steamLobby.CurrentRoomCode);
        }
        else
        {
            UpdateText(null);
        }
    }

    private void OnDisable()
    {
        if (_steamLobby != null)
            _steamLobby.RoomCodeUpdated -= OnRoomCodeUpdated;
    }

    private void OnRoomCodeUpdated(string code)
    {
        UpdateText(code);
    }

    private void UpdateText(string code)
    {
        if (codeText == null)
            return;

        codeText.text = string.IsNullOrWhiteSpace(code)
            ? $"{prefix}-"
            : $"{prefix}{code}";
    }
}
