using Mirror;
using UnityEngine;

public class ToggleZone : NetworkBehaviour
{
    [SerializeField] private GameObject target;
    
    [SyncVar(hook = nameof(OnStateChanged))]
    private bool isOn;

    private void Awake()
    {
        if (target != null) target.SetActive(isOn);
    }

    private void OnStateChanged(bool oldValue, bool newValue)
    {
        if (target != null) target.SetActive(newValue);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Player"))
        {
            isOn = !isOn;
        }
    }
}