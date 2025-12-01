using UnityEngine;
using UnityEngine.InputSystem;

public enum InteractPanelType
{
    MinigameSelection,
    ColorChange,
    FriendCall,
}

public class RangeInteractor : MonoBehaviour
{
    private PlayerScript _player;
    public PlayerScript playerScript;

    private bool _inZone;
    private bool _inColorZone;
    private bool _inMinigameZone;
    private bool _inFriendCallZone;
    private bool _wasInZone;
    private int _colorZoneCount;
    private int _minigameZoneCount;
    private int _friendCallZoneCount;
    [SerializeField] private HUDSO HUDSO;
    private bool _colorChangeOpen;
    private bool _minigameSelectionOpen;
    private bool _friendCallOpen;
    [SerializeField] private bool togglePanel = true;
    [SerializeField] private bool closeOnExit = true;
    private InteractPanelType _currentMode = InteractPanelType.MinigameSelection;
    private Transform cameraAnchor; 
    private bool _aligning;
    [SerializeField] private float alignSpeed = 500f;
    private bool _usePanelCamera;
    private float _nextInteractTime;

    private string GetHintText()
    {
        return "Aperte E para interagir";
    }

    private void SetCursorForPanel(bool open)
    {
        try
        {
            if (open)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        catch { /* ignore in headless */ }
    }
    
    private void TryAutoAssignHUD()
    {
        if (HUDSO != null) return;
        if (_player == null) _player = GetComponent<PlayerScript>();
        if (_player == null) return;
        var phud = _player.GetHUD();
        if (phud != null)
            SetHUD(phud);
    }

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
        if (Time.time < _nextInteractTime) return true;
        _nextInteractTime = Time.time + 0.2f;
        TryAutoAssignHUD();
        if (_inZone)
        {
            if (togglePanel && HUDSO != null)
            {
                var effectiveMode = _currentMode;
                if (_inColorZone) effectiveMode = InteractPanelType.ColorChange;
                else if (_inMinigameZone) effectiveMode = InteractPanelType.MinigameSelection;
                else if (_inFriendCallZone) effectiveMode = InteractPanelType.FriendCall;

                switch (effectiveMode)
                {
                    case InteractPanelType.MinigameSelection:
                    {
                        bool opening = !_minigameSelectionOpen;
                        if (opening)
                        {
                            if (_colorChangeOpen)
                                HUDSO.HideColorChangePanel();
                            HUDSO.ShowMinigameSelectionPanel();
                            Debug.Log("[RangeInteractor] Abrindo painel de minigames");
                            if (_player != null) _player.UILocked = true;
                            if (!playerScript.panel) Painel();
                            HUDSO?.HideInteractHint();
                        }
                        else
                        {
                            HUDSO.HideMinigameSelectionPanel();
                            Debug.Log("[RangeInteractor] Fechando painel de minigames");
                            if (_player != null) _player.UILocked = _colorChangeOpen;
                            if (!_colorChangeOpen && playerScript.panel) Painel();
                            if (_inZone) HUDSO?.ShowInteractHint(GetHintText());
                        }
                        break;
                    }
                    case InteractPanelType.ColorChange:
                    {
                        bool opening = !_colorChangeOpen;
                        if (opening)
                        {
                            if (_minigameSelectionOpen)
                                HUDSO.HideMinigameSelectionPanel();
                            HUDSO.ShowColorChangePanel();
                            Debug.Log("[RangeInteractor] Abrindo painel de troca de cor");
                            if (_player != null) _player.UILocked = true;
                            if (!playerScript.panel) Painel();
                            HUDSO?.HideInteractHint();
                        }
                        else
                        {
                            HUDSO.HideColorChangePanel();
                            Debug.Log("[RangeInteractor] Fechando painel de troca de cor");
                            if (_player != null) _player.UILocked = _minigameSelectionOpen;
                            if (!_minigameSelectionOpen && playerScript.panel) Painel();
                            if (_inZone) HUDSO?.ShowInteractHint(GetHintText());
                        }
                        break;
                    }
                    case InteractPanelType.FriendCall:
                    {
                        bool opening = !_friendCallOpen;
                        if (opening)
                        {
                            if (_minigameSelectionOpen) HUDSO.HideMinigameSelectionPanel();
                            if (_colorChangeOpen) HUDSO.HideColorChangePanel();
                            
                            HUDSO.ShowFriendListPanel();
                            Debug.Log("[RangeInteractor] Abrindo painel de amigos");
                            if (_player != null) _player.UILocked = true;
                            if (!playerScript.panel) Painel();
                            HUDSO?.HideInteractHint();
                        }
                        else
                        {
                            HUDSO.HideFriendListPanel();
                            Debug.Log("[RangeInteractor] Fechando painel de amigos");
                            if (_player != null) _player.UILocked = _colorChangeOpen || _minigameSelectionOpen;
                            if (!_colorChangeOpen && !_minigameSelectionOpen && playerScript.panel) Painel();
                            if (_inZone) HUDSO?.ShowInteractHint(GetHintText());
                        }
                        break;
                    }
                }
            }
            else
            {
                if (togglePanel && HUDSO == null)
                    Debug.LogWarning("[RangeInteractor] HUDSO não atribuído; não é possível abrir UI. Verifique SetHUD/RangeInteractZone.");
                Debug.Log("entrou no painel");
                if (_player != null) _player.UILocked = !_player.UILocked;
                
            }
            return true; 
        }
        return false;
    }

    public void SetInZone(bool inside, InteractPanelType mode)
    {
        // Mantém contadores por tipo (chamado apenas em transições por zona)
        if (mode == InteractPanelType.ColorChange)
        {
            _colorZoneCount += inside ? 1 : -1;
            if (_colorZoneCount < 0) _colorZoneCount = 0;
        }
        else if (mode == InteractPanelType.MinigameSelection)
        {
            _minigameZoneCount += inside ? 1 : -1;
            if (_minigameZoneCount < 0) _minigameZoneCount = 0;
        }
        else if (mode == InteractPanelType.FriendCall)
        {
            _friendCallZoneCount += inside ? 1 : -1;
            if (_friendCallZoneCount < 0) _friendCallZoneCount = 0;
        }

        _inColorZone = _colorZoneCount > 0;
        _inMinigameZone = _minigameZoneCount > 0;
        _inFriendCallZone = _friendCallZoneCount > 0;
        _inZone = _inColorZone || _inMinigameZone || _inFriendCallZone;
        bool entering = _inZone && !_wasInZone;
        bool exitingAll = !_inZone && _wasInZone;

        if (_inColorZone) _currentMode = InteractPanelType.ColorChange;
        else if (_inMinigameZone) _currentMode = InteractPanelType.MinigameSelection;
        else if (_inFriendCallZone) _currentMode = InteractPanelType.FriendCall;

        if (inside)
        {
            if (_player != null && HUDSO != null)
            {
                bool targetOpen = (_inColorZone && _colorChangeOpen) || (_inMinigameZone && _minigameSelectionOpen) || (_inFriendCallZone && _friendCallOpen);
                if (targetOpen)
                {
                    _player.UILocked = true;
                    if (!playerScript.panel) Painel(); else _aligning = true;
                }
                else if (entering)
                {
                    HUDSO.ShowInteractHint(GetHintText());
                }
            }
        }
        if (!inside && closeOnExit)
        {
            if (HUDSO != null)
            {
                if (mode == InteractPanelType.MinigameSelection && _minigameZoneCount == 0 && _minigameSelectionOpen)
                    HUDSO.HideMinigameSelectionPanel();
                if (mode == InteractPanelType.ColorChange && _colorZoneCount == 0 && _colorChangeOpen)
                    HUDSO.HideColorChangePanel();
                if (mode == InteractPanelType.FriendCall && _friendCallZoneCount == 0 && _friendCallOpen)
                    HUDSO.HideFriendListPanel();
            }
            if (_player != null)
            {
                bool anyOpen = _minigameSelectionOpen || _colorChangeOpen || _friendCallOpen;
                _player.UILocked = anyOpen;
                if (!anyOpen && playerScript.panel) Painel();
            }
            if (exitingAll)
            {
                HUDSO?.HideInteractHint();
            }
        }

        _wasInZone = _inZone;
    }

    public void SetHUD(HUDSO hud)
    {
        if (HUDSO != null)
        {
            HUDSO.EventOnHideMinigameSelectionPanel -= OnHideMinigameSelectionPanel;
            HUDSO.EventOnHideColorChangePanel -= OnHideColorChangePanel;
            HUDSO.EventOnShowMinigameSelectionPanel -= OnShowMinigameSelectionPanel;
            HUDSO.EventOnShowColorChangePanel -= OnShowColorChangePanel;
            HUDSO.EventOnShowFriendListPanel -= OnShowFriendListPanel;
            HUDSO.EventOnHideFriendListPanel -= OnHideFriendListPanel;
        }
        HUDSO = hud;
        if (HUDSO != null)
        {
            HUDSO.EventOnHideMinigameSelectionPanel += OnHideMinigameSelectionPanel;
            HUDSO.EventOnHideColorChangePanel += OnHideColorChangePanel;
            HUDSO.EventOnShowMinigameSelectionPanel += OnShowMinigameSelectionPanel;
            HUDSO.EventOnShowColorChangePanel += OnShowColorChangePanel;
            HUDSO.EventOnShowFriendListPanel += OnShowFriendListPanel;
            HUDSO.EventOnHideFriendListPanel += OnHideFriendListPanel;
        }
    }

    private void OnDestroy()
    {
        if (HUDSO != null)
        {
            HUDSO.EventOnHideMinigameSelectionPanel -= OnHideMinigameSelectionPanel;
            HUDSO.EventOnHideColorChangePanel -= OnHideColorChangePanel;
            HUDSO.EventOnShowMinigameSelectionPanel -= OnShowMinigameSelectionPanel;
            HUDSO.EventOnShowColorChangePanel -= OnShowColorChangePanel;
            HUDSO.EventOnShowFriendListPanel -= OnShowFriendListPanel;
            HUDSO.EventOnHideFriendListPanel -= OnHideFriendListPanel;
        }
    }

    private void OnHideMinigameSelectionPanel()
    {
        _minigameSelectionOpen = false;
        if (_player == null) return;
        bool anyOpen = _minigameSelectionOpen || _colorChangeOpen || _friendCallOpen;
        _player.UILocked = anyOpen;
        if (!anyOpen) SetCursorForPanel(false);
        if (!anyOpen && playerScript.panel) Painel();
        if (_inZone) HUDSO?.ShowInteractHint(GetHintText());
    }

    private void OnHideColorChangePanel()
    {
        _colorChangeOpen = false;
        if (_player == null) return;
        bool anyOpen = _minigameSelectionOpen || _colorChangeOpen || _friendCallOpen;
        _player.UILocked = anyOpen;
        if (!anyOpen) SetCursorForPanel(false);
        if (!anyOpen && playerScript.panel) Painel();
        if (_inZone) HUDSO?.ShowInteractHint(GetHintText());
    }

    private void OnShowMinigameSelectionPanel()
    {
        _minigameSelectionOpen = true;
        if (_player == null) return;
        if (!_inZone) return;
        _player.UILocked = true;
        SetCursorForPanel(true);
        if (!playerScript.panel) Painel(); else _aligning = true;
        HUDSO?.HideInteractHint();
    }

    private void OnShowColorChangePanel()
    {
        _colorChangeOpen = true;
        if (_player == null) return;
        if (!_inZone) return;
        _player.UILocked = true;
        SetCursorForPanel(true);
        if (!playerScript.panel) Painel(); else _aligning = true;
        HUDSO?.HideInteractHint();
    }

    private void OnShowFriendListPanel()
    {
        _friendCallOpen = true;
        if (_player == null) return;
        if (!_inZone) return;
        _player.UILocked = true;
        SetCursorForPanel(true);
        if (!playerScript.panel) Painel(); else _aligning = true;
        HUDSO?.HideInteractHint();
    }

    private void OnHideFriendListPanel()
    {
        _friendCallOpen = false;
        if (_player == null) return;
        bool anyOpen = _minigameSelectionOpen || _colorChangeOpen || _friendCallOpen;
        _player.UILocked = anyOpen;
        if (!anyOpen) SetCursorForPanel(false);
        if (!anyOpen && playerScript.panel) Painel();
        if (_inZone) HUDSO?.ShowInteractHint(GetHintText());
    }

    public void ConfigurePanelCamera(bool use, Transform anchor, float alignSpeed)
    {
        _usePanelCamera = use;
        cameraAnchor = anchor;
        this.alignSpeed = alignSpeed;
    }
}
