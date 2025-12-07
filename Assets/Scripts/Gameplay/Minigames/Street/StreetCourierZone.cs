using Mirror;
using UnityEngine;

public enum StreetCourierZoneType
{
    Pickup,
    Dropoff
}

public class StreetCourierZone : NetworkBehaviour
{
    [SerializeField] private StreetMinigameController _minigameController;
    [SerializeField] private StreetCourierZoneType _zoneType = StreetCourierZoneType.Pickup;
    [SyncVar] private ulong _ownerSteamId;
    [SyncVar(hook = nameof(OnTintChanged))] private Color32 _tint;
    [Header("Spawn do Jogador & Destaque")]
    [Tooltip("Ponto de spawn opcional do jogador dono desta entrega.")]
    [SerializeField] private Transform _playerSpawnPoint;
    [Tooltip("VFX opcional para destacar esta entrega apenas para o dono.")]
    [SerializeField] private GameObject _highlightVfx;
    [SerializeField] private Renderer[] renderers;

    [Header("Indicador de Entrega (Seta)")]
    [Tooltip("GameObject da seta/indicador que aparece quando o jogador está com a banana.")]
    [SerializeField] private GameObject _arrowIndicator;
    [Tooltip("Renderer da placa/seta que terá a cor alterada (deve ser filho do Arrow Indicator).")]
    [SerializeField] private Renderer _arrowRenderer;
    
    [Header("Particle System")]
    [Tooltip("Particle System que terá a cor alterada quando o jogador estiver com a banana.")]
    [SerializeField] private ParticleSystem _particleSystem;
    [Tooltip("Se marcado, usa a cor da zona na particle. Se desmarcado, usa a cor configurável abaixo.")]
    [SerializeField] private bool _useZoneColorForParticle = true;
    [Tooltip("Cor da particle quando o jogador está com a banana (só usado se Use Zone Color For Particle estiver desmarcado).")]
    [SerializeField] private Color _carryingParticleColor = Color.yellow;
    
    private Color _originalParticleColor;
    private bool _hasStoredOriginalColor = false;

    private void Start()
    {
        // Armazena a cor original da particle system
        if (_particleSystem != null && !_hasStoredOriginalColor)
        {
            var main = _particleSystem.main;
            _originalParticleColor = main.startColor.color;
            _hasStoredOriginalColor = true;
        }

        // Garante que a seta começa desativada
        if (_arrowIndicator != null)
        {
            _arrowIndicator.SetActive(false);
        }
    }

    private void Reset()
    {
        if (_minigameController == null)
            _minigameController = FindAnyObjectByType<StreetMinigameController>();

        if (_playerSpawnPoint == null)
        {
            var t = transform.Find("SpawnPoint");
            if (t != null) _playerSpawnPoint = t;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        Transform root = other.transform.root;
        if (!root.CompareTag("Player")) return;

        var pd = root.GetComponent<PlayerData>();
        if (pd == null || _minigameController == null) return;

        switch (_zoneType)
        {
            case StreetCourierZoneType.Pickup:
                _minigameController.ServerPickup(pd);
                break;
            case StreetCourierZoneType.Dropoff:
                if (_ownerSteamId == 0UL)
                {
                    Debug.LogWarning($"[StreetCourierZone] Dropoff zone '{name}' has no owner set; ignoring dropoff.");
                    return;
                }
                if (pd.playerInfo.steamId != _ownerSteamId) return;
                _minigameController.ServerDropoff(pd);
                break;
        }
    }

    [Server]
    public void ServerSetOwner(ulong steamId)
    {
        _ownerSteamId = steamId;
    }

    [Server]
    public void ServerSetTint(Color32 color)
    {
        _tint = color;
        RpcApplyTint(color);
    }

    void OnTintChanged(Color32 oldColor, Color32 newColor)
    {
        ApplyTint(newColor);
        // Atualiza a cor da seta se ela estiver ativa
        if (_arrowIndicator != null && _arrowIndicator.activeSelf)
        {
            ApplyArrowColor(newColor);
        }
    }

    [ClientRpc]
    void RpcApplyTint(Color32 color)
    {
        ApplyTint(color);
    }

    void ApplyTint(Color32 color)
    {
        // renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null && r.material != null)
                r.material.color = color;
        }
    }

    // Retorna o ponto de spawn associado a esta zona (ou a própria posição como fallback)
    public Transform GetSpawnPoint()
    {
        return _playerSpawnPoint != null ? _playerSpawnPoint : transform;
    }

    [TargetRpc]
    // Ativa o VFX de destaque apenas para o cliente dono desta zona
    public void TargetShowHighlight(Mirror.NetworkConnectionToClient conn)
    {
        if (_highlightVfx != null)
            _highlightVfx.SetActive(true);
        
        // Ativa a seta quando o jogador está com a banana
        ShowArrowIndicator();
        
        // Muda a cor da particle system para indicar que está carregando
        Color particleColor = _useZoneColorForParticle ? (Color)_tint : _carryingParticleColor;
        ChangeParticleColor(particleColor);
    }

    [TargetRpc]
    // Desativa o VFX de destaque apenas para o cliente dono desta zona
    public void TargetHideHighlight(Mirror.NetworkConnectionToClient conn)
    {
        if (_highlightVfx != null)
            _highlightVfx.SetActive(false);
        
        // Desativa a seta quando entrega ou morre
        HideArrowIndicator();
        
        // Restaura a cor original da particle system
        RestoreParticleColor();
    }

    /// <summary>
    /// Ativa o indicador de seta (quando o jogador está com a banana)
    /// </summary>
    private void ShowArrowIndicator()
    {
        if (_arrowIndicator != null)
        {
            _arrowIndicator.SetActive(true);
            // Aplica a cor da zona na seta
            ApplyArrowColor(_tint);
        }
    }

    /// <summary>
    /// Desativa o indicador de seta (quando entrega ou morre)
    /// </summary>
    private void HideArrowIndicator()
    {
        if (_arrowIndicator != null)
        {
            _arrowIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Aplica a cor da zona na seta/placa
    /// </summary>
    private void ApplyArrowColor(Color32 color)
    {
        if (_arrowRenderer != null && _arrowRenderer.material != null)
        {
            _arrowRenderer.material.color = color;
        }
        else if (_arrowIndicator != null)
        {
            // Tenta encontrar o renderer automaticamente se não foi configurado
            var renderer = _arrowIndicator.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// Muda a cor da Particle System para indicar que o jogador está carregando
    /// </summary>
    private void ChangeParticleColor(Color newColor)
    {
        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            main.startColor = newColor;
        }
    }

    /// <summary>
    /// Restaura a cor original da Particle System
    /// </summary>
    private void RestoreParticleColor()
    {
        if (_particleSystem != null && _hasStoredOriginalColor)
        {
            var main = _particleSystem.main;
            main.startColor = _originalParticleColor;
        }
    }

    public bool HasOwner => _ownerSteamId != 0UL;
    public bool IsDropoffFor(ulong steamId) => _zoneType == StreetCourierZoneType.Dropoff && _ownerSteamId == steamId;
    public StreetCourierZoneType ZoneType => _zoneType;
}
