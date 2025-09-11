using Mirror;
using UnityEngine;

public class ProjectileScript : NetworkBehaviour
{
    [SerializeField] Database db;

    private Vector3 _velocity;
    private bool _launched;
    private Transform _owner;
    public GameObject _vfx;//Phelipe
    public Transform Owner
    {
        get => _owner;
        set => _owner = value;
    }

    [Server]
    public void Initialize(Vector3 origin, Vector3 direction)
    {
        transform.position = origin;

        Vector3 biasedDir = (direction + Vector3.up * db.verticalBias).normalized;
        _velocity = biasedDir * db.projectileSpeed;

        _launched = true;
    }

    [ServerCallback]
    private void Update()
    {
        if (!_launched) return;

        _velocity += Physics.gravity * db.projectileGravityScale * Time.deltaTime;

        transform.position += _velocity * Time.deltaTime;

        // Colisão
        var hits = Physics.OverlapSphere(transform.position, db.projectileRadius, db.projectileMask);
        if (hits.Length > 0)
        {
            foreach (Collider c in hits)
            {
                if (c.transform.root == _owner) continue;
//                Debug.LogError("Player on");
                var dmg = c.transform.root.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    Debug.LogError("Player on Damage");
                    dmg.ReceiveDamage(DamageType.Poop, transform.forward);
                    VFXActivator(); //Phelipe
                }
            }
            // Recicla após impacto (simples). Pode-se adicionar multi-hit se necessário.
            _launched = false;
            // Procurar pool e reciclar
            var poolObj = FindFirstObjectByType<MonoBehaviour>(); // fallback linear search
            ProjectilePool foundPool = null;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == "ProjectilePool") { foundPool = (ProjectilePool)mb; break; }
            }
            if (foundPool != null)
            {
                foundPool.Recycle(gameObject);
            }
            else
            {
                // fallback: desativar para evitar spam Destroy/Instantiate
                gameObject.SetActive(false);
            }
        }
    }
    private void VFXActivator()
    {
        _vfx.SetActive(!_vfx.activeInHierarchy);//Phelipe
    }

    [Server]
    public void ResetForPool()
    {
        _launched = false;
        _velocity = Vector3.zero;
    }
}