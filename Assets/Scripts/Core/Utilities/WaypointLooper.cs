using UnityEngine;

public class WaypointLooper : MonoBehaviour
{
    [Header("Waypoints na ordem")]
    public Transform[] waypoints;

    [Header("Velocidade (unidades/s)")]
    public float speed = 5f;
    
    [SerializeField] private float threshold = 0.1f;
    
    private int currentIndex = 0;

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentIndex];
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < threshold)
        {
            if (currentIndex == waypoints.Length - 1)
            {
                transform.position = waypoints[0].position;
                currentIndex = 1;
            }
            else
            {
                currentIndex++;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);

            // desenha linhas
            if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
        // conecta último ao primeiro só para referência visual
        if (waypoints.Length > 1
            && waypoints[0] != null
            && waypoints[waypoints.Length - 1] != null)
            Gizmos.DrawLine(
                waypoints[waypoints.Length - 1].position,
                waypoints[0].position
            );
    }
}