using Mirror;
using UnityEngine;

// Detecta o player local dentro de um raio usando Physics.SphereCastAll
// e apenas registra Debug.Log ao entrar (sem interface/alvo).
public class RangeInteractZone : MonoBehaviour
{
    [Header("HUD Ref")]
    [SerializeField] private HUDSO HUDSO;

    [Header("SphereCast Zone Settings")]
    [SerializeField] private Transform center;     // se vazio, usa este transform
    [SerializeField] private float radius = 3f;    // raio do zone
    [SerializeField] private float castDistance = 0.1f; // distância mínima do cast (evita zero)
    [SerializeField] private Vector3 castDirection = Vector3.up; // direção do spherecast
    [SerializeField] private LayerMask layerMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private bool debugDraw;

    private PlayerScript _localPlayer;
       private RangeInteractor _localInteractor;
    private bool _wasInside;

    private void Awake()
    {
        // sem interface: nenhuma configuração adicional necessária
    }

    private void Update()
    {
        // resolve player/interactor locais
        if (_localPlayer == null)
            _localPlayer = TryGetLocalPlayer();
        if (_localPlayer == null) return;

        // origem e direção do cast
        Transform c = center != null ? center : transform;
        Vector3 origin = c.position;
        Vector3 dir = castDirection.sqrMagnitude < 0.0001f ? Vector3.up : castDirection.normalized;
        float dist = Mathf.Max(0.01f, castDistance);

        // 1) distância direta do player ao centro do zone
        float playerDist = Vector3.Distance(_localPlayer.transform.position, origin);
        bool insideByDistance = playerDist <= radius;

        // 2) fallback por SphereCastAll, caso queira validar por física
        bool insideByCast = false;
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, dir, dist, layerMask, triggerInteraction);
        if (hits != null)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;
                // valida dono local
                var nid = col.GetComponentInParent<NetworkIdentity>();
                if (nid != null && !nid.isOwned) continue;
                // existe PlayerScript neste collider/parent?
                var ps = col.GetComponentInParent<PlayerScript>();
                if (ps == null || !ps.isLocalPlayer) continue;
                insideByCast = true; break;
            }
        }

        bool inside = insideByDistance || insideByCast;

        if (debugDraw)
        {
            Debug.DrawRay(origin, dir * dist, inside ? Color.green : Color.red, 0.05f);
        }

        if (inside && !_wasInside)
        {
            _wasInside = true;
            if (_localInteractor == null)
                _localInteractor = _localPlayer.GetComponent<RangeInteractor>() ?? _localPlayer.gameObject.AddComponent<RangeInteractor>();
            if (HUDSO != null)
                _localInteractor.SetHUD(HUDSO);
            _localInteractor.SetInZone(true);
            Debug.Log("entrou no range");
        }
        else if (!inside && _wasInside)
        {
            _wasInside = false;
            if (_localInteractor != null)
                _localInteractor.SetInZone(false);
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
