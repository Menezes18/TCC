using UnityEngine;
using UnityEngine.Events;

public class DelayedEventCaller : MonoBehaviour
{
    public void CallAfterDelay(float delay, UnityEvent callback)
    {
        StartCoroutine(InvokeDelayed(delay, callback));
    }

    private System.Collections.IEnumerator InvokeDelayed(float delay, UnityEvent callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }
}