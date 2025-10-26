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
    }

    [TargetRpc]
    // Desativa o VFX de destaque apenas para o cliente dono desta zona
    public void TargetHideHighlight(Mirror.NetworkConnectionToClient conn)
    {
        if (_highlightVfx != null)
            _highlightVfx.SetActive(false);
    }

    public bool HasOwner => _ownerSteamId != 0UL;
    public bool IsDropoffFor(ulong steamId) => _zoneType == StreetCourierZoneType.Dropoff && _ownerSteamId == steamId;
    public StreetCourierZoneType ZoneType => _zoneType;
}
