using System;
using System.Collections.Generic;
using UnityEngine;


public class VehicleLane : MonoBehaviour
{

    [SerializeField] private List<Transform> vehicles = new List<Transform>();
    
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

   
    float laneWidth = 5f;
    float gizmoVerticalOffset = 0.1f;
    
    public float speed = 1f;
    public float offset = 0f;

    
    float Speed => speed * 0.01f;
    float Offset => offset * 0.01f;

    float _timer;
    float _frequency;

    private void Start()
    { 
        _frequency = 1.0f / vehicles.Count;
    }

    private void Update()
    {
        _timer += Speed * Time.deltaTime;

        if (_timer >= 1)
            _timer = 0;
        
        for (int i = 0; i < vehicles.Count; i++){
            
            float startTime = _frequency * i;
            float curated = _timer + startTime + Offset;
            
            if (curated > 1f) curated -= 1f;

            Vector3 flatPos = Vector3.Lerp(startPoint.position, endPoint.position, curated);

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
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        
        Gizmos.matrix = oldMat;
    }
}