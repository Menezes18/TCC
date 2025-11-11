using UnityEngine;

public class PunchingGloveState : StateMachineBehaviour
{
    private PunchingGlove _gloveScript;

    // Chamado no primeiro frame em que a animação (este estado) começa
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Encontra o script principal no objeto pai do Animator
        if (_gloveScript == null)
        {
        _gloveScript = animator.GetComponent<PunchingGlove>();
        }

        if (_gloveScript != null)
        {
            // Limpa a lista de jogadores já atingidos
            _gloveScript.ResetHitPlayers();
            
            // Avisa o script que a animação de soco começou
            _gloveScript.SetPunchingState(true);
        }
    }

    // Chamado no último frame antes da animação (este estado) terminar
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
    Debug.Log("==== PUNCH STATE: EXIT ===="); // Verifica se a animação está saindo
    if (_gloveScript != null)
    {
        _gloveScript.SetPunchingState(false);
    }
    }
}