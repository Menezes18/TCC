using Mirror;
using UnityEngine;


public class RaceCheckpoint : NetworkBehaviour
{
    public int index = 0;
    [SerializeField] private Transform respawnPoint;
    private RaceMinigameController _controller;

    public void BindController(RaceMinigameController controller)
    {
        _controller = controller;
    }

    public Vector3 GetRespawnPosition() => respawnPoint != null ? respawnPoint.position : transform.position;
    public Quaternion GetRespawnRotation() => respawnPoint != null ? respawnPoint.rotation : transform.rotation;

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;

        var pd = other.transform.root.GetComponent<PlayerData>();
        if (pd == null) return;

        (_controller ??= RaceMinigameController.singleton)?.ServerRegisterCheckpoint(pd, this);
    }
}

