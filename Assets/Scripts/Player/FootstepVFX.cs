using Mirror;
using UnityEngine;

public class FootstepNetworkVFX : NetworkBehaviour
{
    [SerializeField] private ParticleSystem footstepPrefab;

    public Transform spawnPoint;
    public void PlayFootstep()
    {
        if (!isLocalPlayer) return;
        CmdPlayFootstep(spawnPoint.position, spawnPoint.rotation);
    }

    [Command]
    void CmdPlayFootstep(Vector3 pos, Quaternion rot)
    {
        RpcPlayFootstep(pos, rot);
    }

    [ClientRpc]
    void RpcPlayFootstep(Vector3 pos, Quaternion rot)
    {
        if (footstepPrefab == null) return;
        var ps = Instantiate(footstepPrefab, pos, rot);
        ps.Play();

        float lifeTime = ps.main.duration + ps.main.startLifetime.constantMax;
        Destroy(ps.gameObject, lifeTime);
    }
}