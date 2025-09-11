using Mirror;
using UnityEngine;

// Contém apenas o que as habilidades precisam (DIP) evitando acessar PlayerScript inteiro.
public class PlayerContext
{
    public PlayerScript Player { get; }
    public NetworkIdentity Identity { get; }
    public Animator Animator { get; }
    public NetworkAnimator NetAnimator { get; }
    public PlayerCooldowns Cooldowns { get; }
    public Database Db { get; }
    public Transform CameraTransform { get; }

    public PlayerContext(PlayerScript p, PlayerCooldowns cds, Database db, Animator animator, NetworkAnimator netAnimator, Transform cam)
    {
        Player = p;
        Cooldowns = cds;
        Db = db;
        Animator = animator;
        NetAnimator = netAnimator;
        CameraTransform = cam;
        Identity = p.netIdentity;
    }
}
