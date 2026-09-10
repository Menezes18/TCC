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
    private void FixedUpdate()
    {
        if (!_launched) return;

        float step = Time.fixedDeltaTime;
        _velocity += Physics.gravity * db.projectileGravityScale * step;
        Vector3 start = transform.position;
        Vector3 displacement = _velocity * step;
        Vector3 end = start + displacement;

        // Se já acertou, não processa mais colisões
        if (_hasHit) return; //Phelipe

        // Colisão
        var hits = Physics.SphereCastAll(start, db.projectileRadius,
            displacement.sqrMagnitude > 0f ? displacement.normalized : Vector3.forward,
            displacement.magnitude, db.projectileMask, QueryTriggerInteraction.Collide);
        transform.position = end;
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                Collider c = hit.collider;
                if (c.transform.root == _owner) continue;

                var dmg = c.transform.root.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    Vector3 impactDirection = _velocity.sqrMagnitude > 0.0001f
                        ? _velocity.normalized
                        : transform.forward;
                    var ball = c.transform.root.GetComponent<BallPhysics>();
                    dmg.ReceiveDamage(ball != null ? DamageType.Push : DamageType.Poop, impactDirection);

                    if (ball != null && _owner != null)
                    {
                        var ownerData = _owner.GetComponent<PlayerData>();
                        if (ownerData != null)
                            ball.ServerRegisterTouch(ownerData.playerInfo.steamId);
                    }

                    _hasHit = true; //Phelipe
                    _launched = false;
                    RpcActivateVFX();
                    NetworkServer.Destroy(gameObject);
                    break;
                }
            }

            //_launched = false;
        }
    }

    [ClientRpc]
    private void RpcActivateVFX() //Phelipe
    {
        if (_vfx == null) return;

        GameObject vfxInstance = Instantiate(_vfx, transform.position, Quaternion.identity);
        vfxInstance.SetActive(true);

        Destroy(vfxInstance, 2f);
    }
}
