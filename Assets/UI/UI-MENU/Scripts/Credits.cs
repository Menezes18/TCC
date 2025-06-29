using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Credits : MonoBehaviour
{

    public float speed = 100f;
    public float creditsPosBegin = -825f;
    public float boundaryTextEnd = 825f;

    RectTransform creditsRect;

    [SerializeField]
    bool isLooping = false;

    void OnEnable()
    {
        creditsRect = GetComponent<RectTransform>();
        StartCoroutine(AutoScroll());
    }
    
    void OnDisable ()
    {
        creditsRect.localPosition = new Vector3(creditsRect.localPosition.x, creditsPosBegin, creditsRect.localPosition.z);
    }

    IEnumerator AutoScroll()
    {
        while(creditsRect.localPosition.y < boundaryTextEnd)
        {
            creditsRect.Translate(Vector3.up * speed * Time.deltaTime);
            if(creditsRect.localPosition.y > boundaryTextEnd)
            {
                if (isLooping)
                {
                    creditsRect.localPosition = Vector3.up * creditsPosBegin;
                }
                else
                {
                    break;
                }
            }
            
            yield return null;
        }
            
    }
}
