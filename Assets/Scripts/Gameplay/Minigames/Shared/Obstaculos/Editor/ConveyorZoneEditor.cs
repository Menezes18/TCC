using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConveyorZone))]
public class ConveyorZoneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Esteira (Server)\n\n" +
            "Como usar:\n" +
            "1) Adicione a um objeto com Collider (IsTrigger).\n" +
            "2) Defina a direção (Forward local ou vetor global) OU escolha modos de oposição ao jogador.\n" +
            "3) Controle a velocidade com 'beltSpeed'. O servidor aplica força de solo periódica para sincronizar todos.\n\n" +
            "Modos: Fixo (direção da esteira), OporFrenteDoJogador (contra o Forward), OporMovimento (contra a velocidade).",
            MessageType.Info);

        base.OnInspectorGUI();
    }
}
