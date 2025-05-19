using UnityEngine;

public class HitKillDetection : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        IHitKillable ik = other.transform.GetComponent<IHitKillable>();
        
        if(ik == null) return;
        
        ik.OnHitKill();
    }
}
