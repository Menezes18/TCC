using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class RangeInteractZone : MonoBehaviour
{
    [Header("HUD Ref")]
    [SerializeField] private HUDSO HUDSO;

    [Header("Interaction")]
    [SerializeField] private InteractPanelType panelMode = InteractPanelType.MinigameSelection;

    [Header("Panel Camera (optional)")]
    [SerializeField] private bool usePanelCamera = false;
    [SerializeField] private Transform cameraAnchor; // se vazio, usa o transform deste objeto
    [SerializeField] private float alignSpeed = 500f;

    [Header("SphereCast Zone Settings")]
    [SerializeField] private Transform center;
    [SerializeField] private float radius = 3f;
    [SerializeField] private float castDistance = 0.1f;
    [SerializeField] private Vector3 castDirection = Vector3.up;
    [SerializeField] private LayerMask layerMask = Physics.DefaultRaycastLayers;
    [SerializeField] private bool debugDraw;

    private PlayerScript _localPlayer;
    private RangeInteractor _localInteractor;
    private bool _wasInside;
    private bool _configuredForLocal;
    public UnityEvent onPlayerEnter;
    public UnityEvent onPlayerExit;

    private void Update()
    {

        if (_localPlayer == null)
            _localPlayer = TryGetLocalPlayer();
        if (_localPlayer == null) return;

        Transform c = center != null ? center : transform;
        Vector3 origin = c.position;
        Vector3 dir = castDirection.sqrMagnitude < 0.0001f ? Vector3.up : castDirection.normalized;
        float dist = Mathf.Max(0.01f, castDistance);

        // Heurística: distância primeiro (estável), cast opcional
        float playerDist = Vector3.Distance(_localPlayer.transform.position, origin);
        bool insideByDistance = playerDist <= radius;

        bool insideByCast = false;
        if (castDistance > 0f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, dir, dist, layerMask);
            if (hits != null)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    var col = hits[i].collider;
                    if (col == null) continue;
                    var ps = col.GetComponentInParent<PlayerScript>();
                    if (ps != null && ps.isLocalPlayer) { insideByCast = true; break; }
                }
            }
        }

        bool inside = insideByDistance || insideByCast;

        if (debugDraw)
        {
            Debug.DrawRay(origin, dir * dist, inside ? Color.green : Color.red, 0.05f);
        }

        // Inicializa interactor e HUD uma vez por jogador local
        if (_localInteractor == null)
            _localInteractor = _localPlayer.GetComponent<RangeInteractor>();
        if (_localInteractor != null && !_configuredForLocal)
        {
            var playerHud = _localPlayer != null ? _localPlayer.GetHUD() : null;
            if (playerHud != null)
                _localInteractor.SetHUD(playerHud);
            else if (HUDSO != null)
                _localInteractor.SetHUD(HUDSO);
            _localInteractor.ConfigurePanelCamera(usePanelCamera, cameraAnchor != null ? cameraAnchor : transform, alignSpeed);
            _configuredForLocal = true;
        }

        // Notifica apenas em transições (evita fechar por zonas distantes)
        if (_localInteractor != null && inside != _wasInside)
        {
            Debug.Log(inside ? "[RangeInteractZone] entrou no range" : "[RangeInteractZone] saiu do range");
            _localInteractor.SetInZone(inside, panelMode);

            if (inside) 
                onPlayerEnter?.Invoke();
            else
                onPlayerExit?.Invoke();

            _wasInside = inside;
        }
    }

    private PlayerScript TryGetLocalPlayer()
    {
        if (NetworkClient.active && NetworkClient.localPlayer != null)
        {
            var ps = NetworkClient.localPlayer.GetComponent<PlayerScript>();
            if (ps != null) return ps;
        }
        var players = FindObjectsOfType<PlayerScript>();
        foreach (var p in players)
        {
            if (p != null && p.isLocalPlayer) return p;
            Debug.LogWarning($"[RangeInteractZone] PlayerScript sem isLocalPlayer em {p.name}");
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Transform c = center != null ? center : transform;
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.2f);
        Gizmos.DrawSphere(c.position, radius);
        Gizmos.color = new Color(0f, 0.6f, 1f, 1f);
        Gizmos.DrawWireSphere(c.position, radius);
    }
}
