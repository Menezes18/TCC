using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
public class PlayerManagerUI : NetworkBehaviour
{
    [SerializeField] private Image crosshair;

    public void Start()
    {
        
    }

    public IEnumerator FillOverTime(float duration)
    {
        float elapsedTime = 0f; 
        float startFill = 0f; 
        float endFill = 1f; 

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime; 
            float newFill = Mathf.Lerp(startFill, endFill, elapsedTime / duration); 
            crosshair.fillAmount = newFill; 
            yield return null; 
        }

        
        crosshair.fillAmount = endFill;
    }
}
