using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VehicleLane : MonoBehaviour
{
    [SerializeField] public List<Transform> vehicles = new List<Transform>();
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    float laneWidth = 5f;
    float gizmoVerticalOffset = 0.1f;
    
    [Header("Speed Control")]
    public float speed = 1f;
    public float offset = 0f;
    
    private float currentSpeed;
    private float _timer;
    
    // UnityEvent para quando a velocidade mudar
    [Header("Events")]
    public UnityEvent<float> OnSpeedChanged;
    
    float Speed => currentSpeed * 0.01f;
    float Offset => offset * 0.01f;
    float _frequency;
    
    // Flag para controlar se é um trem parado
    private bool isTrainStopped = false;
    
    private void Start()
    {
        _frequency = 1.0f / vehicles.Count;
        currentSpeed = speed;
    }
    
    private void Update()
    {
        // Só atualiza o timer se não for um trem parado
        if (!isTrainStopped)
        {
            _timer += Speed * Time.deltaTime;
            if (_timer >= 1) _timer = 0;
        }
        
        UpdateVehiclePositions();
    }
    
    private void UpdateVehiclePositions()
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            float startTime = _frequency * i;
            float curated = _timer + startTime + Offset;
            if (curated > 1f) curated -= 1f;
            
            Vector3 flatPos = Vector3.Lerp(startPoint.position, endPoint.position, curated);
            float originalY = vehicles[i].position.y;
            vehicles[i].position = new Vector3(flatPos.x, originalY, flatPos.z);
        }
    }

    // Método público para alterar a velocidade
    public void SetSpeed(float newSpeed)
    {
        currentSpeed = newSpeed;
        OnSpeedChanged?.Invoke(newSpeed);
    }

    // Métodos específicos para o sinal do trem
    public void SetTrainSpeedGreen()
    {
        isTrainStopped = false;
        SetSpeed(speed); // Usa a velocidade original configurada
        
        Debug.Log($"Trem liberado - Velocidade: {currentSpeed}");
    }
    
    public void SetTrainSpeedRed()
    {
        isTrainStopped = true;
        SetSpeed(0f);
        _timer = 0f;
        ResetVehiclePositionsToStart();


        Debug.Log("Trem parado - Sinal vermelho");
        
        // NÃO reseta o timer - os trens ficam parados onde estão
        // NÃO restaura velocidade automaticamente
    }
    
    // Método para restaurar velocidade original (se necessário)
    public void RestoreOriginalSpeed()
    {
        if (!isTrainStopped) // Só restaura se não estiver parado por sinal
        {
            SetSpeed(speed);
        }
    }
    private void ResetVehiclePositionsToStart()
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            float startTime = _frequency * i + Offset;
            if (startTime > 1f) startTime -= 1f;

            Vector3 flatPos = Vector3.Lerp(startPoint.position, endPoint.position, startTime);
            float originalY = vehicles[i].position.y;
            vehicles[i].position = new Vector3(flatPos.x, originalY, flatPos.z);
        }
    }

    
    private void OnDrawGizmos()
    {
        if (startPoint == null || endPoint == null) return;

        Vector3 p0 = startPoint.position;
        Vector3 p1 = endPoint.position;
        Vector3 dir = (p1 - p0).normalized;
        float length = Vector3.Distance(p0, p1);

        Vector3 center = (p0 + p1) * 0.5f;
        center = new Vector3(
            Mathf.Round(center.x),
            center.y + gizmoVerticalOffset,
            Mathf.Round(center.z)
        );

        Matrix4x4 oldMat = Gizmos.matrix;
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);
        Gizmos.matrix = Matrix4x4.TRS(
            center,
            rot,
            new Vector3(length, 0.01f, laneWidth)
        );

        // Muda a cor baseado na velocidade atual
        Gizmos.color = currentSpeed <= 0 ? Color.red : (currentSpeed >= 15f ? Color.green : Color.yellow);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = oldMat;
    }
}