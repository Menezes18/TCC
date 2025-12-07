using UnityEngine;
using UnityEngine.Events;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private HUDSO HUDSO;

    public UnityEvent<float> OnPushCooldownChanged;
    public UnityEvent<float> OnThrowCooldownChanged;

    void Start()
    {
        HUDSO.EventOnPushCooldownUpdated += HUDSOOnPushCooldownUpdated;
        HUDSO.EventOnThrowCooldownUpdated += HUDSOOnThrowCooldownUpdated;
    }

    void OnDestroy()
    {
        HUDSO.EventOnPushCooldownUpdated -= HUDSOOnPushCooldownUpdated;
        HUDSO.EventOnThrowCooldownUpdated -= HUDSOOnThrowCooldownUpdated;
    }

    private void HUDSOOnPushCooldownUpdated(float value)
    {
        this.OnPushCooldownChanged?.Invoke(value);
    }

    private void HUDSOOnThrowCooldownUpdated(float value)
    {
        this.OnThrowCooldownChanged?.Invoke(value);
    }
}
