using UnityEngine;
using UnityEngine.Events;

public class ClickRelayIntegerTool : MonoBehaviour
{
    
    [SerializeField] bool _sendChildPosAsParameter;
    [SerializeField] private int value;
    
    public UnityEvent<int> ClickRelay;
    
    public void SendClickRelay()
    {
        var parent = transform.parent;
        var grandParent = parent ? parent.parent : null;

        if (parent != null && grandParent != null)
        {
            int parentSiblingIndex = parent.GetSiblingIndex(); // posição do PAI dentro do AVO
            ClickRelay?.Invoke(parentSiblingIndex);
            return;
        }
        
        this.ClickRelay?.Invoke(value);
    }
}
