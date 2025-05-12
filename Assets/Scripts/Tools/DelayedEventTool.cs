using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class DelayedEventTool : MonoBehaviour
{
    
    public UnityEvent DelayedEvent;

    public void TriggerDelayedEvent(float delay)
    {
        LeanTween.delayedCall(delay,() => {
            this.DelayedEvent.Invoke();
        });
    }
}
