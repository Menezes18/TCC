using System;
using UnityEngine;

public class CollisionManager : MonoBehaviour
{
    public Action<Collision> OnVehicleCollisionEnter;
    public Action<Collision> OnVehicleCollisionExit;
    public Action<Collision> OnVehicleCollisionStay;
    
    private void OnCollisionEnter(Collision collision) => OnVehicleCollisionEnter?.Invoke(collision);
    private void OnCollisionStay(Collision collision) => OnVehicleCollisionStay?.Invoke(collision);
    private void OnCollisionExit(Collision collision) => OnVehicleCollisionExit?.Invoke(collision);
}