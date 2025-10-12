using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerEventPanel : MonoBehaviour
{
    public PlayerScript playerScript;
    public Transform cameraAnchor; // optional custom anchor; defaults to this panel transform
    private bool _aligning;
    [SerializeField] private float alignSpeed = 500f;

    public void GetPlayer(Collider other)
    {
        playerScript = other.GetComponent<PlayerScript>();
        Painel();
        if (playerScript != null && playerScript.panel)
            _aligning = true; // rotate once on entry
    }

    private void Update()
    {
        if (playerScript == null) return;
        if (!playerScript.panel) return;
        if (!_aligning) return;
        AlignOnceToPanelForward();
    }

    private void AlignOnceToPanelForward()
    {
        Transform me = playerScript.transform;
        Vector3 dir = transform.forward; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) { _aligning = false; return; }
        Quaternion targetRot = Quaternion.LookRotation(dir);
        float remaining = Quaternion.Angle(me.rotation, targetRot);
        me.rotation = Quaternion.RotateTowards(me.rotation, targetRot, alignSpeed * Time.deltaTime);
        if (remaining <= 2f) _aligning = false;
    }

    public void Painel()
    {
        if (playerScript == null) return;
        playerScript.panel = !playerScript.panel;
        if (playerScript.panel)
        {
            // set camera anchor while panel open
            Transform anchor = cameraAnchor != null ? cameraAnchor : transform;
            playerScript.SetPanelCameraAnchor(anchor);
        }
        else
        {
            playerScript.ClearPanelCameraAnchor();
            playerScript = null;
            _aligning = false;
        }
    }
}
