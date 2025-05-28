using UnityEngine;

public class PermaHitKillDetection : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        IHitKillable ik = other.transform.GetComponent<IHitKillable>();
        
        if(ik == null) return;
        
        ik.OnHitSpectate();
    }
}
