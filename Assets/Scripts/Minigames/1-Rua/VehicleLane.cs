using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

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
    
    // Nova variável para velocidade atual (pode ser diferente da speed base)
    private float currentSpeed;

    
    // UnityEvent para quando a velocidade mudar
    [Header("Events")]
    public UnityEvent<float> OnSpeedChanged;
    
    float Speed => currentSpeed * 0.01f;
    float Offset => offset * 0.01f;
    float _timer;
    float _frequency;
    
    // Coroutine de referência para poder cancelar se necessário
    private Coroutine resetCoroutine;
    
    private void Start()
    {
        _frequency = 1.0f / vehicles.Count;
        currentSpeed = speed; // Inicializa com a velocidade padrão
    }
    
    private void Update()
    {
        _timer += Speed * Time.deltaTime;
        if (_timer >= 1) _timer = 0;
        
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
        OnSpeedChanged?.Invoke(currentSpeed);
    }

    // Métodos específicos para o sinal do trem
    public void SetTrainSpeedGreen()
    {
        // Cancela o reset automático se estiver rodando
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        SetSpeed(100f);
        Debug.Log($"Trem em {gameObject.name}: Sinal VERDE - Velocidade 100");
    }
    

    public void SetTrainSpeedRed()
    {
        SetSpeed(0f);
        Debug.Log($"Trem em {gameObject.name}: Sinal VERMELHO - Parado");

        // Inicia o reset automático para voltar ao startPoint
        AutoResetToStart();
    }
    
    // Coroutine para voltar ao startPoint
    private void AutoResetToStart()
    {        
        // Reset do timer para posição inicial
        _timer = 0f;
        
        // Restaura velocidade original
        RestoreOriginalSpeed();
        
        resetCoroutine = null;
    }
    
    // Método para restaurar velocidade original
    public void RestoreOriginalSpeed()
    {
        SetSpeed(speed);
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
        
        // Muda a cor baseado na velocidade atual (vermelho = parado, verde = movimento)
        Gizmos.color = currentSpeed <= 0 ? Color.red : (currentSpeed >= 15f ? Color.green : Color.yellow);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = oldMat;
    }
}