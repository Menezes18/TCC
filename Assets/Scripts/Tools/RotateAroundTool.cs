using System;
using UnityEngine;

public class RotateAroundTool : MonoBehaviour{
    public float angularSpeed;

    private void Update()
    {
        transform.Rotate(Vector3.up, angularSpeed *Time.deltaTime);
    }
}
