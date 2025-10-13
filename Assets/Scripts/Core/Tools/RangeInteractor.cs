using UnityEngine;

public enum InteractPanelType
{
    MinigameSelection,
    ColorChange,
}

public class RangeInteractor : MonoBehaviour
{
    private PlayerScript _player;
    public PlayerScript playerScript;

    private bool _inZone;
    [SerializeField] private HUDSO HUDSO;
    [SerializeField] private bool togglePanel = true;
    [SerializeField] private bool closeOnExit = true;
    private InteractPanelType _currentMode = InteractPanelType.MinigameSelection;
    private Transform cameraAnchor; 
    private bool _aligning;
    [SerializeField] private float alignSpeed = 500f;
    private bool _usePanelCamera;

    private void Awake()
    {
        _player = GetComponent<PlayerScript>();
        playerScript = _player;
    }

    private void Update()
    {
        if (playerScript == null) return;
        if (!playerScript.panel) return;
        if (!_aligning) return;
        AlignOnceToPanelForward();
    }

    private void AlignOnceToPanelForward()
    {
        Transform me = playerScript.transform;
        Transform anchorRef = cameraAnchor != null ? cameraAnchor : transform;
        Vector3 dir = anchorRef.forward; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) { _aligning = false; return; }
        Quaternion targetRot = Quaternion.LookRotation(dir);
        float remaining = Quaternion.Angle(me.rotation, targetRot);
        me.rotation = Quaternion.RotateTowards(me.rotation, targetRot, alignSpeed * Time.deltaTime);
        if (remaining <= 2f) _aligning = false;
    }

    public void GetPlayer(Collider other)
    {
        playerScript = other.GetComponent<PlayerScript>();
        Painel();
        if (playerScript != null && playerScript.panel)
            _aligning = true;
    }

    public void Painel()
    {
        if (playerScript == null) return;
        playerScript.panel = !playerScript.panel;
        if (playerScript.panel)
        {
            Transform anchor = transform;
            playerScript.SetPanelCameraAnchor(anchor);
        }
        else
        {
            playerScript.ClearPanelCameraAnchor();
            _aligning = false;
        }
    }

    public bool TryInteract()
    {
        if (_player == null) return false;
        if (_inZone)
        {
            if (togglePanel && HUDSO != null)
            {
                switch (_currentMode)
                {
                    case InteractPanelType.MinigameSelection:
                    {
                        bool opening = !HUDSO.MinigameSelectionOpen;
                        if (opening)
                        {
                            HUDSO.HideColorChangePanel();
                            HUDSO.ShowMinigameSelectionPanel();
                            Debug.Log("[RangeInteractor] Abrindo painel de minigames");
                            if (_player != null) _player.UILocked = true;
                            if (!playerScript.panel) Painel();
                        }
                        else
                        {
                            HUDSO.HideMinigameSelectionPanel();
                            Debug.Log("[RangeInteractor] Fechando painel de minigames");
                            if (_player != null) _player.UILocked = HUDSO.ColorChangeOpen;
                            if (!HUDSO.ColorChangeOpen && playerScript.panel) Painel();
                        }
                        break;
                    }
                    case InteractPanelType.ColorChange:
                    {
                        bool opening = !HUDSO.ColorChangeOpen;
                        if (opening)
                        {
                            HUDSO.HideMinigameSelectionPanel();
                            HUDSO.ShowColorChangePanel();
                            Debug.Log("[RangeInteractor] Abrindo painel de troca de cor");
                            if (_player != null) _player.UILocked = true;
                            if (!playerScript.panel) Painel();
                        }
                        else
                        {
                            HUDSO.HideColorChangePanel();
                            Debug.Log("[RangeInteractor] Fechando painel de troca de cor");
                            if (_player != null) _player.UILocked = HUDSO.MinigameSelectionOpen;
                            if (!HUDSO.MinigameSelectionOpen && playerScript.panel) Painel();
                        }
                        break;
                    }
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

    public void SetInZone(bool inside, InteractPanelType mode)
    {
        _inZone = inside;
        _currentMode = mode;
        if (inside)
        {
            if (_player != null && HUDSO != null)
            {
                bool targetOpen = (_currentMode == InteractPanelType.ColorChange && HUDSO.ColorChangeOpen) ||
                                  (_currentMode == InteractPanelType.MinigameSelection && HUDSO.MinigameSelectionOpen);
                if (targetOpen)
                {
                    _player.UILocked = true;
                    if (!playerScript.panel) Painel(); else _aligning = true;
                }
            }
        }
        if (!inside && closeOnExit)
        {
            if (HUDSO != null)
            {
                if (_currentMode == InteractPanelType.MinigameSelection && HUDSO.MinigameSelectionOpen)
                    HUDSO.HideMinigameSelectionPanel();
                if (_currentMode == InteractPanelType.ColorChange && HUDSO.ColorChangeOpen)
                    HUDSO.HideColorChangePanel();
            }
            if (_player != null)
            {
                bool anyOpen = (HUDSO != null) && (HUDSO.MinigameSelectionOpen || HUDSO.ColorChangeOpen);
                _player.UILocked = anyOpen;
                if (!anyOpen && playerScript.panel) Painel();
            }

        }
    }

    public void SetHUD(HUDSO hud)
    {
        if (HUDSO != null)
        {
            HUDSO.EventOnHideMinigameSelectionPanel -= OnAnyPanelHidden;
            HUDSO.EventOnHideColorChangePanel -= OnAnyPanelHidden;
            HUDSO.EventOnShowMinigameSelectionPanel -= OnAnyPanelShown;
            HUDSO.EventOnShowColorChangePanel -= OnAnyPanelShown;
        }
        HUDSO = hud;
        if (HUDSO != null)
        {
            HUDSO.EventOnHideMinigameSelectionPanel += OnAnyPanelHidden;
            HUDSO.EventOnHideColorChangePanel += OnAnyPanelHidden;
            HUDSO.EventOnShowMinigameSelectionPanel += OnAnyPanelShown;
            HUDSO.EventOnShowColorChangePanel += OnAnyPanelShown;
        }
    }

    private void OnDestroy()
    {
        if (HUDSO != null)
        {
            HUDSO.EventOnHideMinigameSelectionPanel -= OnAnyPanelHidden;
            HUDSO.EventOnHideColorChangePanel -= OnAnyPanelHidden;
            HUDSO.EventOnShowMinigameSelectionPanel -= OnAnyPanelShown;
            HUDSO.EventOnShowColorChangePanel -= OnAnyPanelShown;
        }
    }

    private void OnAnyPanelHidden()
    {
        if (_player == null) return;
        bool anyOpen = (HUDSO != null) && (HUDSO.MinigameSelectionOpen || HUDSO.ColorChangeOpen);
        _player.UILocked = anyOpen;
        if (!anyOpen && playerScript.panel) Painel();
    }

    private void OnAnyPanelShown()
    {
        if (_player == null) return;
        if (!_inZone) return; 

        _player.UILocked = true;
        if (!playerScript.panel) Painel(); else _aligning = true;
    }

    public void ConfigurePanelCamera(bool use, Transform anchor, float alignSpeed)
    {
        _usePanelCamera = use;
        cameraAnchor = anchor;
        this.alignSpeed = alignSpeed;
    }
}
