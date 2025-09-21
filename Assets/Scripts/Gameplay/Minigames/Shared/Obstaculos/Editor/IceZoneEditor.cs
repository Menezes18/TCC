using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IceZone))]
public class IceZoneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Zona de Gelo (Ice)\n\n" +
            "Como usar:\n" +
            "1) Adicione a um objeto com Collider (IsTrigger) cobrindo a área de gelo.\n" +
            "2) Modo LocalOwner: efeito leve e suave, aplicado no cliente dono (economiza rede).\n" +
            "3) Modo ServerPulse: o servidor mantém velocidade de deslizamento por jogador, com atrito e direção, enviando pulsos periódicos.\n" +
            "   Ajuste 'slideFriction', 'maxSlideSpeed', 'captureThreshold', 'captureBlend' e 'controlMultiplierOnIce'.\n\n" +
            "Resultado: andar no gelo é difícil (menos controle) e a inércia mantém o jogador deslizando.",
            MessageType.Info);



        base.OnInspectorGUI();
    }
}
