using UnityEngine;
using UnityEngine.Events;
using Mirror;

public class ActionFrameCamera : MonoBehaviour
{
    public UnityEvent OnAction;

    public void FrameCamera()
    {
        OnAction.Invoke();
        if (MatchManager.singleton == null)
            return;

        if (NetworkClient.active)
        {
            MatchManager.singleton.CmdStartMatchAfterCamera(MatchManager.singleton.CameraPhaseGeneration);
        }
    }
}
