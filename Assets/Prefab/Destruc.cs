using UnityEngine;

public class Destruc : MonoBehaviour
{
    public float Time;
    void Start()
    {
      Destroy(gameObject, Time);
    }
}
