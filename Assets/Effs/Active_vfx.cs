using UnityEngine;

public class Active_vfx : MonoBehaviour
{
    public GameObject vfx;
    public void SpVFX()
    {
        vfx.SetActive(!vfx.activeInHierarchy);
    }

}
