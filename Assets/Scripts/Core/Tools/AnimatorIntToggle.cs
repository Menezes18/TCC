using UnityEngine;


public class AnimatorIntToggle : MonoBehaviour
{
    public Animator animator;
    public string parameter = "State";
    int id;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        id = Animator.StringToHash(parameter);
    }

    public void SetOn()
    {
        if (!animator) return;
        animator.SetInteger(id, 1);
    }

    public void SetOff()
    {
        if (!animator) return;
        animator.SetInteger(id, 0);
    }


}

