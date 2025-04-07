using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [System.Serializable]
    public class AnimationClipData
    {
        public PlayerStates state;
        public string animationName;
        public float transitionDuration = 0.1f;
        public int layer = 0;
        public bool useBlendTree = false;
        public string blendParameterX = "";
        public string blendParameterY = "";
    }

    [Header("Animation Settings")]
    [SerializeField] private AnimationClipData[] animationClips;

    [Header("Thresholds")]
    [SerializeField] private float walkThreshold = 0.1f;
    [SerializeField] private float runThreshold = 0.5f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerScriptBase playerScript;
    [SerializeField] private PlayerInputScript playerInput;

    private Dictionary<PlayerStates, AnimationClipData> animationMap;
    private AnimationStateMachine stateMachine;
    private PlayerStates currentState;
    private Vector2 lastInput;

    private void Awake()
    {
        if (playerScript == null) playerScript = GetComponent<PlayerScriptBase>();
        if (playerInput == null) playerInput = GetComponent<PlayerInputScript>();
        if (animator == null) animator = GetComponent<Animator>();

        InitializeAnimationMap();
        InitializeStateMachine();
    }

    private void InitializeAnimationMap()
    {
        animationMap = new Dictionary<PlayerStates, AnimationClipData>();
        foreach (var clip in animationClips)
        {
            if (!animationMap.ContainsKey(clip.state))
                animationMap.Add(clip.state, clip);
        }
    }

    private void InitializeStateMachine()
    {
        stateMachine = new AnimationStateMachine(animator, animationMap);
        playerScript.OnStateChangeEvent += HandleStateChange;
    }

    private void Update()
    {
        if (playerInput == null) return;

        Vector2 input = playerInput.GetInput();
        HandleAnimations(input);
    }
    

    private void HandleAnimations(Vector2 input)
    {
        if (!animationMap.TryGetValue(playerScript.State, out var clipData)) return;

        if (clipData.useBlendTree)
        {
            if (!string.IsNullOrEmpty(clipData.blendParameterX))
                animator.SetFloat(clipData.blendParameterX, input.x);
            if (!string.IsNullOrEmpty(clipData.blendParameterY))
                animator.SetFloat(clipData.blendParameterY, input.y);
        }
        else
        {
            float mag = input.magnitude;
            string animState = GetMovementStateName(mag);
            animator.CrossFade(animState, clipData.transitionDuration, clipData.layer);
        }
    }

    private string GetMovementStateName(float magnitude)
    {
        if (magnitude < walkThreshold) return "Idle";
        if (magnitude < runThreshold) return "Walk";
        return "Run";
    }

    private void HandleStateChange(PlayerStates oldState, PlayerStates newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            stateMachine.TransitionToState(newState);
        }
    }

    private void OnDestroy()
    {
        if (playerScript != null)
            playerScript.OnStateChangeEvent -= HandleStateChange;
    }

    public void PlayCurrentStateAnimation()
    {
        if (animationMap.ContainsKey(playerScript.State))
        {
            animator.Play(animationMap[playerScript.State].animationName);
        }
    }
}
