using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Database", menuName = "ScriptableObject/Database")]
public class Database : ScriptableObject{

    public float playerSpeed;

    public float airSpeed;
    public float maxAirSpeed;
    public float gravityGrounded;
    public float gravity;
    public float jumpHeight;

    [Header("Push Configuration")]
    public float pushForce = 5f;
    public float pushCooldown = 1f;
    public float pushRadius = 2f;
    public float pushAngle = 45f;
    public float pushDuration = 0.3f;
    
    
    
    [Header("Shoot Configuration")]
    public float shootCooldown = 1.0f;
    public float shootDamage = 10f;
    public float shootRange = 20f;
    public float pushForceShoot = 5f;
}
