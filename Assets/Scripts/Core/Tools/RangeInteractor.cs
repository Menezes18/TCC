using UnityEngine;

public class RangeInteractor : MonoBehaviour
{
    private PlayerScript _player;
    private bool _inZone;
    [SerializeField] private HUDSO HUDSO;
    [SerializeField] private bool togglePanel = true;
    [SerializeField] private bool closeOnExit = true;

    private void Awake()
    {
        _player = GetComponent<PlayerScript>();
    }

    public bool TryInteract()
    {
        if (_player == null) return false;
        if (_inZone)
        {
            if (togglePanel && HUDSO != null)
            {
                bool opening = !HUDSO.MinigameSelectionOpen;
                if (opening)
                {
                    HUDSO.ShowMinigameSelectionPanel();
                    Debug.Log("entrou no painel");
                    if (_player != null) _player.UILocked = true; 
                }
                else
                {
                    HUDSO.HideMinigameSelectionPanel();
                    Debug.Log("fechou o painel");
                    if (_player != null) _player.UILocked = false; 
                }
            }
            else
            {
                Debug.Log("entrou no painel");
                if (_player != null) _player.UILocked = !_player.UILocked;
                
            }
            return true; 
        }
        return false;
    }

    public void SetInZone(bool inside)
    {
        _inZone = inside;
        if (!inside && closeOnExit)
        {
            if (HUDSO != null && HUDSO.MinigameSelectionOpen)
                HUDSO.HideMinigameSelectionPanel();
            if (_player != null) _player.UILocked = false;

        }
    }

    public void SetHUD(HUDSO hud)
    {
        HUDSO = hud;
    }
}
