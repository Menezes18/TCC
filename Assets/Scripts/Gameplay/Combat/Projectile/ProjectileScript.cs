using Mirror;
using UnityEngine;

public class ProjectileScript : NetworkBehaviour
{
    [SerializeField] Database db;

    private Vector3 _velocity;
    private bool _launched;
    private bool _hasHit; //Phelipe
    private Transform _owner;

    public GameObject _vfx; //Phelipe

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

        // Se já acertou, não processa mais colisões
        if (_hasHit) return; //Phelipe

        // Colisão
        var hits = Physics.OverlapSphere(transform.position, db.projectileRadius, db.projectileMask);
        if (hits.Length > 0)
        {
            foreach (Collider c in hits)
            {
                if (c.transform.root == _owner) continue;

                var dmg = c.transform.root.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    Debug.LogError("Player on Damage");
                    dmg.ReceiveDamage(DamageType.Poop, transform.forward);

                    _hasHit = true; //Phelipe
                    VFXActivator(); //Phelipe
                }
            }

            //_launched = false;
        }
    }

    private void VFXActivator() //Phelipe
    {
        if (_vfx == null) return;

        GameObject vfxInstance = Instantiate(_vfx, transform.position, Quaternion.identity);
        vfxInstance.SetActive(true);

        Destroy(vfxInstance, 2f);
    }
}
