using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RecordeDistanciaEndpoint : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text distanciaText;
    public GameObject player;
    public float initialDistance = 0;
    public float maxTraveled = 0;
    private float recordDist = 0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        float initialDist = Vector3.Distance(player.transform.position, transform.position);
        initialDistance = initialDist;
        maxTraveled = 0f;
    }

    private void Update()
    {
        if(player == null){
            player = GameObject.FindGameObjectWithTag("Player");

            float initialDist = Vector3.Distance(player.transform.position, transform.position);
            initialDistance = initialDist;
            maxTraveled = 0f;
        }

        float currentDistance = Vector3.Distance(player.transform.position, transform.position);
        float traveled = initialDistance - currentDistance;
        
        if (traveled > maxTraveled)
        {
            maxTraveled = traveled;
            distanciaText.text = "Recorde: " + Mathf.RoundToInt(maxTraveled).ToString();
        }
    }
}
