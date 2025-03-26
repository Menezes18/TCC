using UnityEngine;
using Mirror;

public class Vehicle : NetworkBehaviour
{
    [Header("Network Sync")]
    [SyncVar]
    private Vector3 moveDirection;
    [SyncVar]
    private float speed;

    [SyncVar(hook = nameof(OnPositionChanged))]
    private Vector3 syncedPosition;


    private float minSpacing;
    private Bounds bounds;
    private bool isInitialized = false;

    [Server]
    public void Initialize(float speed, Vector3 direction, float minSpacing)
    {
        if (!isServer) return;
        Debug.Log($"[Server] Inicializando veículo com speed: {speed}");
        this.speed = speed;
        this.moveDirection = direction;
        this.minSpacing = minSpacing;
        this.syncedPosition = transform.position;

        // Configura os bounds usando o Collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            bounds = col.bounds;
        }
        else
        {
            bounds = new Bounds(transform.position, Vector3.one);
        }

        // Rotaciona o veículo na direção do movimento
        transform.forward = new Vector3(direction.x, 0, direction.z);

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (isServer)
        {
            Debug.Log($"[Server] Movendo veículo com velocidade: {speed} e direção: {moveDirection}");
            transform.position += moveDirection * speed * Time.deltaTime;
            syncedPosition = transform.position;
            RpcUpdatePosition(transform.position);
            // Atualiza os bounds
            if (bounds != null)
            {
                bounds.center = transform.position;
            }

            // Apenas o servidor verifica colisões
        
                PreventVehicleCollision();

        }
        else
        {
            transform.position = syncedPosition;
        }
        
    }
    [ClientRpc]
    private void RpcUpdatePosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }


    private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        if (!isServer)
        {
            transform.position = newPos;
        }
    }
    [Server]
    private void PreventVehicleCollision()
    {
        if (!isServer) return;

        Vehicle[] vehicles = transform.parent.GetComponentsInChildren<Vehicle>();
        foreach (var otherVehicle in vehicles)
        {
            if (otherVehicle == null || otherVehicle == this) continue;

            float distance = Vector3.Distance(transform.position, otherVehicle.transform.position);
            if (distance < minSpacing)
            {
                Vector3 directionToOther = (otherVehicle.transform.position - transform.position).normalized;
                if (Vector3.Dot(moveDirection, directionToOther) > 0)
                {
                    speed = otherVehicle.speed * 0.8f;
                }
            }
        }
    }

    public float GetSpeed()
    {
        return speed;
    }

    public Bounds GetBounds()
    {
        return bounds;
    }
}