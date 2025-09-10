using UnityEngine;
using UnityEngine.Events;


public class BlindPanel : MonoBehaviour
{
    [SerializeField] HUDSO HUDSO;

    public UnityEvent<float> OnBlindAlphaChanged;

    void Start()
    {
        HUDSO.EventOnSetBlindAlpha += HUDSOOnEventOnSetBlindAlpha;
    }
    
    //
    private void HUDSOOnEventOnSetBlindAlpha(float value)
    {
        this.OnBlindAlphaChanged?.Invoke(value);
    }
}
