using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerControl", menuName = "Player/ControlSO")]
public class PlayerControlSO : ScriptableObject
{
    [Header("Movimentação")]
    public float speed = 5.0f;
    public float airSpeed = 8.0f;

    [Header("Física")]
    public float gravity = -9.81f;
    public float jumpForce = 10f;

    [Header("Câmera")]
    public float sensitivity = 5.0f;
}