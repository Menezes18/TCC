using UnityEngine;

public class StartCursor : MonoBehaviour{
    public PlayerControlsSO player;
    void Start()
    {
        player.EnableCursor();
    }

    
}
