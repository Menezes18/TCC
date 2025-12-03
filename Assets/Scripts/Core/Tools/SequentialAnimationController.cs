using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SequentialAnimationController : MonoBehaviour
{
    
    public float delayBetweenAnimations = 1f;
    public string animatorParameterName = "State";
       
    private Coroutine _sequence;
    public Animator[] animators;

    public void Start()
    {
        StartSequence();
    }
    
    public void StartSequence()
    {
        StartSequence(delayBetweenAnimations);
    }
    
    public void StartSequence(float customDelay)
    {
        _sequence = StartCoroutine(RunSequence(customDelay));
    }
    private IEnumerator RunSequence(float delay)
    {
          

        yield return new WaitForSeconds(delay);
        Debug.Log("Starting sequence");
        animators[0].SetInteger("State", 1);
        animators[1].SetInteger("State", 2);
        animators[2].SetInteger("State", 3);
        animators[3].SetInteger("State", 4);
        

    }
}

