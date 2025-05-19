using UnityEngine;
using UnityEngine.Events;

public class ClickRelayIntegerTool : MonoBehaviour
{
    
    [SerializeField] bool _sendChildPosAsParameter;
    [SerializeField] private int value;
    
    public UnityEvent<int> ClickRelay;
    
    public void SendClickRelay()
    {
        if (_sendChildPosAsParameter){

            int siblingIndex = transform.GetSiblingIndex();
            this.ClickRelay?.Invoke(siblingIndex);
            return;
        }
        
        this.ClickRelay?.Invoke(value);
    }
}
