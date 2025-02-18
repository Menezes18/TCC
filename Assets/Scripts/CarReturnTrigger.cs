using UnityEngine;
using Mirror;

public class CarReturnTrigger : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Car"))
        {
            CarController carController = other.GetComponent<CarController>();
            if (carController != null)
            {
                carController.ReturnToPool();
            }
        }
    }
}