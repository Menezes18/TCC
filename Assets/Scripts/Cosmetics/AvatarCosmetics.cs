using UnityEngine;

public class AvatarCosmetics : MonoBehaviour
{


    public void RotationPlayer(Collider other)
    {
        Transform player = other.transform;
        player.Rotate(Vector3.up, 90f);
    }
}
