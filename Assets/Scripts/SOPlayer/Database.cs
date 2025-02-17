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
}
