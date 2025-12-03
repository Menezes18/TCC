using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequentialAnimationController : MonoBehaviour
{
    [System.Serializable]
    public class AnimationTarget
    {
        public GameObject targetObject;
        public int stateValue;
    }
    
    [Header("Configuration")]
    public List<AnimationTarget> animationTargets = new List<AnimationTarget>();
    public float delayBetweenAnimations = 1f;
    public string animatorParameterName = "State";
    
    private Coroutine _sequence;
    
    public void StartSequence()
    {
        StartSequence(delayBetweenAnimations);
    }
    
    public void StartSequence(float customDelay)
    {
        if (_sequence != null)
            StopCoroutine(_sequence);
        
        _sequence = StartCoroutine(RunSequence(customDelay));
    }
    
    private IEnumerator RunSequence(float delay)
    {
        foreach (var target in animationTargets)
        {
            if (target.targetObject != null)
            {
                Animator animator = target.targetObject.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetInteger(animatorParameterName, target.stateValue);
                }
            }
            
            yield return new WaitForSeconds(delay);
        }
        
        _sequence = null;
    }
}

