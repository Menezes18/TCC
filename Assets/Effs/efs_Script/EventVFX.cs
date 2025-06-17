using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class EventVFX : MonoBehaviour
{
    public UnityEvent VFXActive;
    public GameObject[] VFX;
    [SerializeField] private float cooldown = 2f; // Tempo de cooldown em segundos

    private bool canTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (VFX.Length <= 0) return;
        if (!canTrigger) return;
        if (other.CompareTag("Player"))
        {
            canTrigger = false;
            VFXActive?.Invoke();
            StartCoroutine(CooldownTimer());
        }
    }

    private IEnumerator CooldownTimer()
    {
        yield return new WaitForSeconds(cooldown);
        canTrigger = true;
    }

    public void VFXTemporario(float lifetime)
    { 
        foreach (GameObject vfx in VFX)
            StartCoroutine(TemporaryVFX(vfx, lifetime));
    }

    private IEnumerator TemporaryVFX(GameObject vfx, float lifetime)
    {
        vfx.SetActive(true);
        yield return new WaitForSeconds(lifetime);
        vfx.SetActive(false);
    }
}
