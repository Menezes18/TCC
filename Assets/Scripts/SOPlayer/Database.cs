
using UnityEngine;
[CreateAssetMenu(fileName = "Database", menuName = "Player/Database")]
public class Database : ScriptableObject{

    public float inputAccel;
    public float inputGravity;
    
    public float gravity;
    public float gravityGrounded = 1;
    
    public Vector3 orbitalOffset;
    public float minMouseY, maxMouseX;
    

    public float playerSpeed = 1;
    public float playerJumpHeight;
    public float playerAirSpeed;
    public float playerMaxAirSpeed;

    public LayerMask PlayerMask;
    public float playerPushRadius;
    public float playerPushStrength;
    public float playerPushCooldownTimer;
    
    public float playerStaggerHeight;
    public float playerStaggerAirSpeedModifier = 1;
    public float playerStaggerStunDuration;


}
