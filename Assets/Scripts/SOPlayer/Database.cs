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
    public float pushForce = 25f;
    public float pushDistance = 5f;
    public float slideDuration = 1.0f;
    public float pushRadius = 3f;
    public float pushCooldown = 1.5f;
    public float bounceForce = 8f;
    public float pushAngle = 45f; 
    public ParticleSystem pushVFX;
    public AudioSource pushAudio;
    
    
    
}
