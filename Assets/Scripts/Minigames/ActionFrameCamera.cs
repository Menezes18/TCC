using UnityEngine;
using UnityEngine.Events;

public class ActionFrameCamera : MonoBehaviour
{
    public UnityEvent OnAction;

    public void FrameCamera()
    {
        OnAction.Invoke();
    }
}
