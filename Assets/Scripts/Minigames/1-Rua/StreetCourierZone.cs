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

    private void Reset()
    {
        if (_minigameController == null)
            _minigameController = FindAnyObjectByType<StreetMinigameController>();
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
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null && r.material != null)
                r.material.color = color;
        }
    }

    public bool HasOwner => _ownerSteamId != 0UL;
    public bool IsDropoffFor(ulong steamId) => _zoneType == StreetCourierZoneType.Dropoff && _ownerSteamId == steamId;
    public StreetCourierZoneType ZoneType => _zoneType;
}
