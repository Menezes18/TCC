using UnityEngine;

public enum Acao{
    Push,
    Pulo,
    Arremeco
};
public class Active_vfx : MonoBehaviour
{
    public GameObject vfx;
    public Animator _animator;
    public Acao Acao;
    void Start()
    {
        if(_animator != null)
        {
            TemAnimacao();
        }
    }
    private void TemAnimacao()
    {
        if (Acao == Acao.Push)
            {
                _animator.SetBool("Push", true);
                _animator.SetBool("Pulo", false);
            }
            if (Acao == Acao.Pulo)
            {
                _animator.SetBool("Pulo", true);
                _animator.SetBool("Push", false);
            }
    }
    public void SpVFX()
    {
        vfx.SetActive(!vfx.activeInHierarchy);
    }

}
