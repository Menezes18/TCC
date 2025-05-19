using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerEventPanel : MonoBehaviour
{
    public PlayerScript playerScript; 
        
    public void GetPlayer(Collider other)
    {
        playerScript = other.GetComponent<PlayerScript>();
        Painel();

    }

    public void Painel()
    {
        playerScript.panel = !playerScript.panel;
    }
}