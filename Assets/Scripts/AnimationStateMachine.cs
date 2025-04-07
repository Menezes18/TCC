using UnityEngine;
using System.Collections.Generic;

public class AnimationStateMachine
{
    private Animator animator;
    private Dictionary<PlayerStates, PlayerAnimationController.AnimationClipData> animationMap;
    private PlayerStates currentAnimationState;
    private float lastTransitionTime;

    public AnimationStateMachine(Animator animator, Dictionary<PlayerStates, PlayerAnimationController.AnimationClipData> animationMap)
    {
        this.animator = animator;
        this.animationMap = animationMap;
        this.currentAnimationState = PlayerStates.Moving;
    }

    public void TransitionToState(PlayerStates newState)
    {
        // Don't interrupt if same state or animation is still transitioning
        if (currentAnimationState == newState || Time.time < lastTransitionTime + 0.1f)
            return;

        if (animationMap.TryGetValue(newState, out var clipData))
        {
            // Use crossfade for smooth transitions
            animator.CrossFade(clipData.animationName, clipData.transitionDuration, clipData.layer);
            currentAnimationState = newState;
            lastTransitionTime = Time.time;
        }
        else
        {
           // Debug.LogWarning($"No animation found for state: {newState}");
        }
    }
}