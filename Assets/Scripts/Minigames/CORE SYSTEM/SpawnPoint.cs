using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private int _teamId = -1; // -1 = qualquer time
    
    public int TeamId => _teamId;
    
    private void OnDrawGizmos()
    {
        // Visualização do ponto de spawn no editor
        Gizmos.color = _teamId switch
        {
            0 => Color.red,
            1 => Color.blue,
            2 => Color.green,
            3 => Color.yellow,
            _ => Color.white
        };
        
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}
