using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BouncePad))]
public class BouncePadEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Trampolim (Bounce Pad)\n\n" +
            "Como usar:\n" +
            "1) Adicione este componente a um objeto com Collider marcado como IsTrigger.\n" +
            "2) Ajuste as forças Horizontal/Vertical e se usa o Forward local ou direção global.\n" +
            "3) Coloque o volume sobre a área de contato do jogador.\n\n" +
            "Rede: o impulso é aplicado no servidor para manter consistência.",
            MessageType.Info);


        base.OnInspectorGUI();
    }
}
