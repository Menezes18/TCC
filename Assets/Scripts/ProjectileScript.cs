using Mirror;
using UnityEngine;

public class ProjectileScript : NetworkBehaviour
{
    [SerializeField] Database db;

    private Vector3 _velocity;
    private bool _launched;
    private Transform _owner;

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
                Debug.LogError("Player on");
                var dmg = c.transform.root.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    Debug.LogError("Player on Damage");
                    dmg.ReceiveDamage(DamageType.Poop, transform.forward);
                }
            }
            
            //_launched = false;
        }
    }
}