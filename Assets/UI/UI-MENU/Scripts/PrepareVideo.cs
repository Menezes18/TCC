using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class PrepareVideo : MonoBehaviour
{
    public VideoPlayer player;

    private void Start()
    {
        player = GetComponent<VideoPlayer>();
        player.Prepare();
        
    }
}
