using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//This script handles inspector.

public class InspectManager : MonoBehaviour
{
    [Header("Inspect Type")]
    public Types types;
     
    public enum Types { Text, TextAndImage };

    [Header("References")]
    public GameObject Inspector;
    
    
    [Header("Texts")]
    public string Title;
    [TextArea]
    public string Description;
    [Header("Images")]
    public Sprite image;

    [Header("References")]
    [HideInInspector] public TextMeshProUGUI _Title;
    [HideInInspector] public TextMeshProUGUI _Description;
    [HideInInspector] public Image _image;



    public void ActiveInspector()
    {
        
    }

    public void DeactiveInspector()
    {
        
    }


}

