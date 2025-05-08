
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Database", menuName = "Player/Database")]
public class Database : ScriptableObject{

    public float inputAccel;
    public float inputGravity;
    
    public float gravity;
    public float gravityGrounded = 1;
    
    public Vector3 orbitalOffset;
    public float minMouseY, maxMouseX;
    public LayerMask cameraColliderMash;
    public float cameraSphereRadius = 0.2f;

    [Header("Player")]
    public float playerSpeed = 1;
    public float playerJumpHeight;
    public float playerAirSpeed;
    public float playerMaxAirSpeed;
    public LayerMask PlayerMask;

    [Header("Player Push")]
    public float playerPushRadius;
    public float playerPushStrength;
    public float playerPushCooldownTimer;
    
    [Header("Player Stagger")]
    public float playerStaggerHeight;
    public float playerStaggerAirSpeedModifier = 1;
    public float playerStaggerStunDuration;

    [Header("Player Blind")] 
    public float playerBlindDuration;
    public AnimationCurve playerBlindCurve;
    
    [Header("Player Roll")]
    public float playerRollSpeed;
    public AnimationCurve playerRollCurve;
    public float playerRollDuration;
    public float playerRollCooldownDuration;
    
    [Header("Player Projectile")]
    public Transform projectilePrefab;
    public LayerMask projectileMask;
    public float projectileRadius;
    public float projectileSpeed;
    [Range(0f, 1f)] public float verticalBias = 0.5f;
    public float projectileGravityScale = 1;
    public Vector3 projectileLocalOffset = new Vector3(0f, 1f, 0.5f);
    public float playerThrowCooldown;



    [Header("Server Side")] 
    public List<Color> playerColors;
    
    public float serverPrepareDuration;
    public float serverMatchDuration;

}
