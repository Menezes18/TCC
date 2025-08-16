using UnityEngine;

public class ActiveAnimation : MonoBehaviour
{
    public void BoolAnimacao(bool estado)
    {
        GetComponent<Animator>().SetBool("Open",estado);
    }
}
