using UnityEngine;

[CreateAssetMenu(fileName = "PanelCameraSO", menuName = "Player/Panel Camera Settings")]
public class PanelCameraSO : ScriptableObject
{
    public Vector3 panelOrbitalOffset = new Vector3(0.12f, 0.08f, -2.29f);
    [Range(0.5f, 20f)] public float panelCamLerp = 6f;
    [Range(-30f, 45f)] public float panelPitch = 8f;
    [Range(0.1f, 2f)] public float panelExitDuration = 0.5f;
    [Range(30f, 360f)] public float panelRotateSpeed = 300f;
}

