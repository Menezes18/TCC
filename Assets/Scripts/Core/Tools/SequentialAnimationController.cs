using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SequentialAnimationController : MonoBehaviour
{
    
    public float delayBetweenAnimations = 1f;
    public string animatorParameterName = "State";
       
    private Coroutine _sequence;
    public Animator animator1player;
    public Animator animator2player;

    public void Start()
    {
        StartCoroutine(DelayedAnimationStart());
    }

    private IEnumerator DelayedAnimationStart()
    {
        yield return new WaitForSeconds(1.5f);

        animator1player.SetBool("Start1", true);
        animator2player.SetBool("Start2", true);
    }

   
}

