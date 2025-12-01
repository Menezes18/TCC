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

        if (NetworkServer.active)
        {
            MatchManager.singleton.StartMatch();
        }
        else if (NetworkClient.active)
        {
            MatchManager.singleton.CmdStartMatchAfterCamera();
        }
    }
}
