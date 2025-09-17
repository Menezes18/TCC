using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FallGuysDoor))]
public class FallGuysDoorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Porta estilo Fall Guys\n\n" +
            "Como usar:\n" +
            "1) Adicione em cada porta. Mantenha um Collider sólido (não IsTrigger) e um Trigger frontal para detectar batida.\n" +
            "2) No pai, adicione 'Fall Guys Door Row' para sortear automaticamente as portas verdadeiras.\n" +
            "3) Porta verdadeira cai (Animate ou Physics); porta falsa empurra o jogador para trás.",
            MessageType.Info);

        if (GUILayout.Button("Abrir documentação completa"))
        {
            var ta = AssetDatabase.LoadAssetAtPath<Object>("Assets/Documentation/Hazards.md");
            if (ta != null) { Selection.activeObject = ta; EditorGUIUtility.PingObject(ta); }
        }

        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(FallGuysDoorRow))]
public class FallGuysDoorRowEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Fileira de Portas (Fall Guys)\n\n" +
            "Como usar:\n" +
            "1) Adicione no objeto-pai da fileira.\n" +
            "2) Portas filhas devem ter o componente 'Fall Guys Door'.\n" +
            "3) No servidor, uma (ou várias) será(ão) marcada(s) como verdadeira(s).",
            MessageType.Info);

        if (GUILayout.Button("Abrir documentação completa"))
        {
            var ta = AssetDatabase.LoadAssetAtPath<Object>("Assets/Documentation/Hazards.md");
            if (ta != null) { Selection.activeObject = ta; EditorGUIUtility.PingObject(ta); }
        }

        base.OnInspectorGUI();
    }
}

