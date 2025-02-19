using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class Lane : NetworkBehaviour
{
    [Header("Pontos de Controle")]
    public Vector3 spawnPoint = Vector3.zero;
    public Vector3 endPoint = new Vector3(10, 0, 0);
    public float laneWidth = 2f;

    [Header("Debug Visual")]
    public Color laneColor = Color.yellow;
    public Color spawnColor = Color.green;
    public Color endColor = Color.red;
    public Color arrowColor = Color.blue;
    public float pointSize = 0.5f;

    private LaneManager.LaneConfig config;
    private float nextSpawnTime;
    private List<Vehicle> activeVehicles = new List<Vehicle>();
    [SyncVar]
    private bool isActive = true;

    public void Initialize(LaneManager.LaneConfig config)
    {
        if (!isServer) return;
        this.config = config;
        nextSpawnTime = Time.time + Random.Range(config.minSpawnInterval, config.maxSpawnInterval);
    }

    private void Update()
    {
        if(!isServer) return;
        if (config == null || !isActive) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnVehicle();
            ScheduleNextSpawn();
        }

        CleanupVehicles();
    }

    [Server]
    private void SpawnVehicle()
    {
        if (!HasEnoughSpaceForNewVehicle())
            return;

        int randomIndex = Random.Range(0, config.vehiclePrefabs.Length);

        // Criar o veículo na posição correta
        GameObject vehicleObj = Instantiate(
            config.vehiclePrefabs[randomIndex],
            transform.position + spawnPoint,
            Quaternion.identity
        );

        Vehicle vehicle = vehicleObj.GetComponent<Vehicle>();

        float randomSpeed = Random.Range(config.minVehicleSpeed, config.maxVehicleSpeed);
        Vector3 direction = (endPoint - spawnPoint).normalized;

        vehicle.Initialize(randomSpeed, direction, config.minVehicleSpacing);

        // Spawn correto na rede
        NetworkServer.Spawn(vehicleObj);

        vehicleObj.transform.SetParent(transform, true);

        activeVehicles.Add(vehicle);
    }


    private bool HasEnoughSpaceForNewVehicle()
    {
        Vector3 spawnPos = transform.position + spawnPoint;

        foreach (var vehicle in activeVehicles)
        {
            if (vehicle == null) continue;

            float distance = Vector3.Distance(spawnPos, vehicle.transform.position);
            if (distance < config.minVehicleSpacing)
            {
                return false;
            }
        }
        return true;
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(config.minSpawnInterval, config.maxSpawnInterval);
    }

    private void CleanupVehicles()
    {
        Vector3 direction = (endPoint - spawnPoint).normalized;
        float totalDistance = Vector3.Distance(spawnPoint, endPoint);

        activeVehicles.RemoveAll(vehicle =>
        {
            if (vehicle == null) return true;

            Vector3 toVehicle = vehicle.transform.position - (transform.position + spawnPoint);
            float progress = Vector3.Dot(toVehicle, direction);

            if (progress > totalDistance)
            {
                Destroy(vehicle.gameObject);
                return true;
            }
            return false;
        });
    }

    public bool CheckCollision(Bounds playerBounds)
    {
        foreach (var vehicle in activeVehicles)
        {
            if (vehicle != null && vehicle.GetBounds().Intersects(playerBounds))
                return true;
        }
        return false;
    }

    public void ClearVehicles()
    {
        foreach (var vehicle in activeVehicles)
        {
            if (vehicle != null)
                Destroy(vehicle.gameObject);
        }
        activeVehicles.Clear();
    }

   
    public Vector3 GetLaneDirection()
    {
        return (endPoint - spawnPoint).normalized;
    }

   
    public float GetLaneLength()
    {
        return Vector3.Distance(spawnPoint, endPoint);
    }




    #region OnDrawGizmo

    private void OnDrawGizmos()
    {
        // Desenha a faixa (caminho)
        Gizmos.color = laneColor;
        Vector3 direction = (endPoint - spawnPoint).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        Vector3 leftEdge = transform.position + spawnPoint - right * (laneWidth * 0.5f);
        Vector3 rightEdge = transform.position + spawnPoint + right * (laneWidth * 0.5f);

        // Desenha as linhas da faixa
        Gizmos.DrawLine(leftEdge, transform.position + endPoint - right * (laneWidth * 0.5f));
        Gizmos.DrawLine(rightEdge, transform.position + endPoint + right * (laneWidth * 0.5f));

        // Desenha linhas tracejadas no meio
        Vector3 start = transform.position + spawnPoint;
        Vector3 end = transform.position + endPoint;
        float totalDistance = Vector3.Distance(start, end);
        int segments = Mathf.CeilToInt(totalDistance / 2f);

        for (int i = 0; i < segments; i++)
        {
            float t = i * 2f;
            if (t + 1f > totalDistance) break;
            Vector3 segStart = start + direction * t;
            Vector3 segEnd = start + direction * Mathf.Min(t + 1f, totalDistance);
            Gizmos.DrawLine(segStart, segEnd);
        }

        // Desenha os pontos de spawn e fim
        Gizmos.color = spawnColor;
        Gizmos.DrawSphere(transform.position + spawnPoint, pointSize);
        Gizmos.color = endColor;
        Gizmos.DrawSphere(transform.position + endPoint, pointSize);

        // Desenha setas de direção
        DrawDirectionArrows();

        // Desenha áreas de spawn e fim
        Gizmos.color = new Color(spawnColor.r, spawnColor.g, spawnColor.b, 0.3f);
        DrawSpawnArea();
        Gizmos.color = new Color(endColor.r, endColor.g, endColor.b, 0.3f);
        DrawEndArea();
    }

    private void DrawDirectionArrows()
    {
        Vector3 direction = (endPoint - spawnPoint).normalized;
        Vector3 middle = transform.position + Vector3.Lerp(spawnPoint, endPoint, 0.5f);

        // Seta principal
        Gizmos.color = arrowColor;
        Gizmos.DrawRay(middle, direction * 2f);

        // Pontas da seta
        float arrowSize = 0.5f;
        Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * arrowSize;
        Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * arrowSize;
        Gizmos.DrawRay(middle + direction * 2f, right);
        Gizmos.DrawRay(middle + direction * 2f, left);
    }

    private void DrawSpawnArea()
    {
        Vector3 center = transform.position + spawnPoint;
        Vector3 size = new Vector3(laneWidth, 0.1f, laneWidth);
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            center,
            Quaternion.LookRotation((endPoint - spawnPoint).normalized),
            Vector3.one
        );
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void DrawEndArea()
    {
        Vector3 center = transform.position + endPoint;
        Vector3 size = new Vector3(laneWidth, 0.1f, laneWidth);
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            center,
            Quaternion.LookRotation((endPoint - spawnPoint).normalized),
            Vector3.one
        );
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
    }
    #endregion
}