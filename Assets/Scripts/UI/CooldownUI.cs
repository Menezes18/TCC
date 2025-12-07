using UnityEngine;
using UnityEngine.Events;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private HUDSO HUDSO;

    public UnityEvent<float> OnPushCooldownChanged;
    public UnityEvent<float> OnThrowCooldownChanged;
    public UnityEvent<bool> OnAbilityBlockChanged;

    void Start()
    {
        HUDSO.EventOnPushCooldownUpdated += HUDSOOnPushCooldownUpdated;
        HUDSO.EventOnThrowCooldownUpdated += HUDSOOnThrowCooldownUpdated;
        HUDSO.EventOnAbilityBlockUpdated += HUDSOOnAbilityBlockUpdated;
    }

    void OnDestroy()
    {
        HUDSO.EventOnPushCooldownUpdated -= HUDSOOnPushCooldownUpdated;
        HUDSO.EventOnThrowCooldownUpdated -= HUDSOOnThrowCooldownUpdated;
        HUDSO.EventOnAbilityBlockUpdated -= HUDSOOnAbilityBlockUpdated;
    }

    private void HUDSOOnPushCooldownUpdated(float value)
    {
        this.OnPushCooldownChanged?.Invoke(value);
    }

    private void HUDSOOnThrowCooldownUpdated(float value)
    {
        this.OnThrowCooldownChanged?.Invoke(value);
    }

    private void HUDSOOnAbilityBlockUpdated(bool blocked)
    {
        this.OnAbilityBlockChanged?.Invoke(blocked);
    }
}
