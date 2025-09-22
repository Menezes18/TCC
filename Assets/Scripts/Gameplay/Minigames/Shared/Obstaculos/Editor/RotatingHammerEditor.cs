using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RotatingHammer))]
public class RotatingHammerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Martelo Giratório (Rotating Hammer)\n\n" +
            "Como usar:\n" +
            "1) Monte o braço/mesh com Collider (IsTrigger) e adicione este componente.\n" +
            "2) Defina o 'rotateTarget' (braço) e o 'pivot' (centro do giro).\n" +
            "3) Ajuste a velocidade de rotação e as forças de empurrão/elevação.\n" +
            "4) O acerto é processado no servidor para consistência.",
            MessageType.Info);

        base.OnInspectorGUI();
    }
}
